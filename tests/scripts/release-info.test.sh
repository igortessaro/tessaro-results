#!/usr/bin/env bash
# Tests for .github/scripts/release-info.sh (tag validation, main-only releases, breaking-change baseline).
# Each case builds a throwaway git repository with tags and a fake origin/main.
# Cases start with `ac "<id>" ...`; AC IDs are found by the AC traceability check.

set -euo pipefail

script="$(cd "$(dirname "$0")/../.." && pwd)/.github/scripts/release-info.sh"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

failures=0
cases=0
current_ac=""
current_desc=""
repo_count=0

ac() { current_ac="$1"; current_desc="$2"; cases=$((cases + 1)); }

fail_case() {
  failures=$((failures + 1))
  printf 'FAIL [%s] %s\n  %s\n' "$current_ac" "$current_desc" "$1"
}

# Creates a new repository with one commit on main and enters it.
new_repo() {
  repo_count=$((repo_count + 1))
  local dir="$work/repo-$repo_count"
  mkdir -p "$dir" && cd "$dir"
  git init -q -b main
  git config user.email test@example.com
  git config user.name test
  git config tag.gpgSign false
  git config commit.gpgSign false
  commit
}

commit() { git commit -q --allow-empty -m "commit $RANDOM"; }
publish_main() { git update-ref refs/remotes/origin/main HEAD; }
head_sha() { git rev-parse HEAD; }

# Runs the script; sets $out, $err and $code.
run() {
  code=0
  out="$(bash "$script" "$1" "$2" 2>"$work/stderr")" || code=$?
  err="$(cat "$work/stderr")"
}

expect_output() { # <expected stdout>
  if [ "$code" -ne 0 ] || [ "$out" != "$1" ]; then
    fail_case "expected exit 0 and:
$1
  got exit $code and:
$out
  stderr: $err"
  fi
}

expect_error() { # <substring of stderr>
  if [ "$code" -ne 1 ] || [[ "$err" != *"$1"* ]]; then
    fail_case "expected exit 1 with '$1' on stderr; got exit $code, stderr: $err, stdout: $out"
  fi
}

# --- Tag format (fail fast) ---------------------------------------------------------------------

for tag in v1.2 release-1 1.2.3 v01.2.3 v1.2.3- "v1.2.3 "; do
  ac "guard" "malformed tag '$tag' is rejected"
  new_repo; publish_main
  run "$tag" "$(head_sha)"
  expect_error "tag must look like v1.2.3 or v1.2.3-rc.1"
done

ac "guard" "missing arguments are rejected"
new_repo; publish_main
code=0; err="$(bash "$script" 2>&1 >/dev/null)" || code=$?
expect_error "usage:"

# --- The tagged commit must be on main ----------------------------------------------------------

ac "release-guard" "a commit on a branch that is not main is rejected"
new_repo; publish_main
git switch -q -c topic; commit
run v0.0.1 "$(head_sha)"
expect_error "the tagged commit is not on main"

ac "release-guard" "an unknown commit is rejected"
new_repo; publish_main
run v0.0.1 0123456789abcdef0123456789abcdef01234567
expect_error "the tagged commit is not on main"

ac "release-guard" "an older commit of main is accepted"
new_repo; old="$(head_sha)"; commit; publish_main
git tag v0.0.1 "$old"
run v0.0.1 "$old"
expect_output "version=0.0.1
prerelease=false
baseline="

# --- Prerelease flag ----------------------------------------------------------------------------

ac "release-guard" "a prerelease tag sets prerelease=true"
new_repo; publish_main; git tag v0.0.1-rc.1
run v0.0.1-rc.1 "$(head_sha)"
expect_output "version=0.0.1-rc.1
prerelease=true
baseline="

ac "release-guard" "build metadata alone is not a prerelease"
new_repo; publish_main; git tag v1.0.0+build.5
run v1.0.0+build.5 "$(head_sha)"
expect_output "version=1.0.0+build.5
prerelease=false
baseline="

# --- Baseline for the breaking-change check -----------------------------------------------------

ac "release-guard" "no earlier tag: no baseline"
new_repo; publish_main; git tag v0.0.1
run v0.0.1 "$(head_sha)"
expect_output "version=0.0.1
prerelease=false
baseline="

ac "release-guard" "patch bump in 0.y: baseline is the previous version"
new_repo; git tag v0.1.0; commit; publish_main; git tag v0.1.1
run v0.1.1 "$(head_sha)"
expect_output "version=0.1.1
prerelease=false
baseline=0.1.0"

ac "release-guard" "minor bump in 0.y allows breaking changes: no baseline"
new_repo; git tag v0.1.3; commit; publish_main; git tag v0.2.0
run v0.2.0 "$(head_sha)"
expect_output "version=0.2.0
prerelease=false
baseline="

ac "release-guard" "0.y to 1.0.0 allows breaking changes: no baseline"
new_repo; git tag v0.9.0; commit; publish_main; git tag v1.0.0
run v1.0.0 "$(head_sha)"
expect_output "version=1.0.0
prerelease=false
baseline="

ac "release-guard" "minor bump from 1.x: baseline is the previous version"
new_repo; git tag v1.4.2; commit; publish_main; git tag v1.5.0
run v1.5.0 "$(head_sha)"
expect_output "version=1.5.0
prerelease=false
baseline=1.4.2"

ac "release-guard" "major bump from 1.x allows breaking changes: no baseline"
new_repo; git tag v1.4.2; commit; publish_main; git tag v2.0.0
run v2.0.0 "$(head_sha)"
expect_output "version=2.0.0
prerelease=false
baseline="

ac "release-guard" "final release on the same commit as its rc: baseline is the rc"
new_repo; publish_main; git tag v0.0.1-rc.1; git tag v0.0.1
run v0.0.1 "$(head_sha)"
expect_output "version=0.0.1
prerelease=false
baseline=0.0.1-rc.1"

ac "release-guard" "the closest earlier tag in history wins"
new_repo; git tag v0.1.0; commit; git tag v0.1.1; commit; publish_main; git tag v0.1.2
run v0.1.2 "$(head_sha)"
expect_output "version=0.1.2
prerelease=false
baseline=0.1.1"

ac "release-guard" "a tag on another branch is not a baseline"
new_repo; base="$(head_sha)"
git switch -q -c topic; commit; git tag v0.5.0
git switch -q main; commit; publish_main; git tag v0.0.1
run v0.0.1 "$(head_sha)"
expect_output "version=0.0.1
prerelease=false
baseline="
: "$base"

ac "release-guard" "tags that are not versions are ignored"
new_repo; git tag v0.1.0; commit; git tag docs-snapshot; git tag vnext; commit; publish_main; git tag v0.1.1
run v0.1.1 "$(head_sha)"
expect_output "version=0.1.1
prerelease=false
baseline=0.1.0"

echo "release-info: $((cases - failures))/$cases passed"
[ "$failures" -eq 0 ]
