#!/usr/bin/env bash
# Tests for .github/scripts/check-pr-title.sh (rules in ADR 0005).
# Each case starts with `ac "AC-x.y" ...` so the AC traceability check can find it.

set -euo pipefail

script="$(cd "$(dirname "$0")/../.." && pwd)/.github/scripts/check-pr-title.sh"
failures=0
cases=0
current_ac=""
current_desc=""

ac() { current_ac="$1"; current_desc="$2"; }

run_case() { # <expected: pass|fail> <title>
  local expected="$1" title="$2" actual output
  cases=$((cases + 1))
  if output="$(bash "$script" "$title" 2>&1)"; then actual=pass; else actual=fail; fi
  if [ "$actual" != "$expected" ]; then
    failures=$((failures + 1))
    printf 'FAIL [%s] %s\n  title:    %s\n  expected: %s, got: %s\n  output:   %s\n' \
      "$current_ac" "$current_desc" "$title" "$expected" "$actual" "$output"
  fi
}

expect_pass() { run_case pass "$1"; }
expect_fail() { run_case fail "$1"; }

ac "pr-title" "a task title passes";                       expect_pass "Spec 012 (T03) | Document the public API"
ac "pr-title" "a spec-phase title passes";                 expect_pass "Spec 012 | Add the release spec"
ac "pr-title" "a hotfix title passes";                     expect_pass "Hotfix | Fix the null check in the factory"
ac "pr-title" "a chore title passes";                      expect_pass "Chore | Update the PR template"
ac "pr-title" "exactly 72 characters passes";              expect_pass "Chore | $(printf 'A%.0s' {1..64})"
ac "pr-title" "a Dependabot title only needs the prefix";  expect_pass "Deps | bump xunit.v3 from 4.0.2 to 4.1.0 in the test group, longer than seventy-two characters"

ac "pr-title" "a Conventional Commit title fails";         expect_fail "docs: update readme"
ac "pr-title" "a lower-case summary fails";                expect_fail "Spec 012 (T03) | document usage"
ac "pr-title" "a trailing period fails";                   expect_fail "Spec 012 (T03) | Document usage."
ac "pr-title" "trailing whitespace fails";                 expect_fail "Spec 012 (T03) | Document usage "
ac "pr-title" "73 characters fails";                       expect_fail "Chore | $(printf 'A%.0s' {1..65})"
ac "pr-title" "a spec number without three digits fails";  expect_fail "Spec 12 (T03) | Document usage"
ac "pr-title" "a task without a space before it fails";    expect_fail "Spec 012(T03) | Document usage"
ac "pr-title" "a task without two digits fails";           expect_fail "Spec 012 (T3) | Document usage"
ac "pr-title" "an unknown context fails";                  expect_fail "Feat | Add thing"
ac "pr-title" "a missing space after the bar fails";       expect_fail "Spec 012 (T03) |Document usage"
ac "pr-title" "a Dependabot title without a space fails";  expect_fail "Deps |bump x"
ac "pr-title" "an empty title fails";                      expect_fail ""

ac "pr-title" "a failure shows the format and an example"
cases=$((cases + 1))
output="$(bash "$script" "docs: update readme" 2>&1 || true)"
if [[ "$output" != *"<Context> | <Imperative summary>"* || "$output" != *"Spec 012 (T03) | Document the public API"* ]]; then
  failures=$((failures + 1))
  printf 'FAIL [%s] %s\n  output: %s\n' "$current_ac" "$current_desc" "$output"
fi

echo "check-pr-title: $((cases - failures))/$cases passed"
[ "$failures" -eq 0 ]
