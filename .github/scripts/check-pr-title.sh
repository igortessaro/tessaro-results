#!/usr/bin/env bash
# Checks a pull request title against the standard in ADR 0005.
#
# Usage: check-pr-title.sh "<title>"
# Exit codes: 0 = valid, 1 = invalid (the reason and the expected format go to stderr).

set -euo pipefail

readonly max_length=72
readonly deps_pattern='^Deps \| [^[:space:]].*$'
readonly title_pattern='^(Spec [0-9]{3}( \(T[0-9]{2}\))?|Hotfix|Chore) \| [A-Z].*[^.[:space:]]$'

title="${1-}"

fail() {
  cat >&2 <<EOF
Invalid pull request title: "$title"
Reason: $1

Expected: <Context> | <Imperative summary>   (ADR 0005, docs/adr/0005-pull-request-title-and-description.md)
  Context: Spec NNN (TNN), Spec NNN, Hotfix, Deps or Chore
  Summary: starts with a capital imperative verb, no trailing period; whole title at most $max_length characters
Example: Spec 012 (T03) | Document the public API
EOF
  exit 1
}

# Dependabot writes the summary itself, so only the prefix is checked (ADR 0005).
if [[ "$title" =~ $deps_pattern ]]; then
  exit 0
fi

[[ "$title" =~ $title_pattern ]] || fail "it does not match the format"
[[ "${#title}" -le "$max_length" ]] || fail "it has ${#title} characters (maximum $max_length)"
