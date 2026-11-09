#!/usr/bin/env bash
# Release guard: validates a release tag and computes what the release needs.
#
# Usage: release-info.sh <tag> <sha>
#   Run inside the repository with full history and tags, and with origin/main fetched.
#
# Prints, in the $GITHUB_OUTPUT format:
#   version=<tag without the leading v>
#   prerelease=<true|false>
#   baseline=<previous version to check API compatibility against, or empty when breaking changes are allowed>
#
# Exit codes: 0 = release may proceed, 1 = rejected (reason on stderr).

set -euo pipefail

# SemVer 2.0 with a leading "v": MAJOR.MINOR.PATCH, optional -prerelease and +build parts.
readonly semver='^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$'

fail() { echo "release-info: $1" >&2; exit 1; }

[[ $# -eq 2 ]] || fail "usage: release-info.sh <tag> <sha>"
tag="$1"
sha="$2"

[[ "$tag" =~ $semver ]] || fail "tag must look like v1.2.3 or v1.2.3-rc.1 (SemVer with a leading v), got '$tag'"
major="${BASH_REMATCH[1]}"
minor="${BASH_REMATCH[2]}"
prerelease_part="${BASH_REMATCH[4]}"

# Only commits that are on main can be released.
git merge-base --is-ancestor "$sha" origin/main 2>/dev/null || fail "the tagged commit is not on main ($sha)"

# Closest earlier version tag in the history of $sha, ignoring the tag being released and any
# tag that is not a version (git describe has no regular expressions, so exclude and retry).
previous=""
excludes=(--exclude "$tag")
while candidate="$(git describe --tags --abbrev=0 --match 'v*' "${excludes[@]}" "$sha" 2>/dev/null)"; do
  if [[ "$candidate" =~ $semver ]]; then
    previous="$candidate"
    break
  fi
  excludes+=(--exclude "$candidate")
done

# A breaking change needs a major bump from 1.0.0 on, or a minor bump in 0.y (SemVer §4).
baseline=""
if [[ -n "$previous" ]]; then
  [[ "$previous" =~ $semver ]]
  previous_major="${BASH_REMATCH[1]}"
  previous_minor="${BASH_REMATCH[2]}"
  if [[ "$major" -gt "$previous_major" ]]; then
    baseline=""
  elif [[ "$major" -eq 0 && "$previous_major" -eq 0 && "$minor" -gt "$previous_minor" ]]; then
    baseline=""
  else
    baseline="${previous#v}"
  fi
fi

echo "version=${tag#v}"
if [[ -n "$prerelease_part" ]]; then echo "prerelease=true"; else echo "prerelease=false"; fi
echo "baseline=$baseline"
