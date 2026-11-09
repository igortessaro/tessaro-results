<!--
Title: <Context> | <Imperative summary>   (ADR 0005, max 72 characters)
  Spec 012 (T03) | Document the public API
  Spec 012 | Add the release spec
  Hotfix | … · Deps | … · Chore | …
Omit any section below that does not apply; never leave one empty.
-->

## Summary

<!-- Two to four sentences: what changed and why. For a task, name it, e.g. "T03 — Creation and state". -->

## Changes

-

## Testing

<!-- What was run and the real results. List the ACs covered, each with a [Trait("AC", "AC-x.y")] test. -->

| Check | Result |
|---|---|
| `dotnet build -c Release` | |
| `dotnet format --verify-no-changes` | |
| `dotnet test` + coverage gate | |

## Notes for reviewer

<!-- ADRs, deviations from the plan, risks, what is left for later. -->

## Checklist

- [ ] All listed ACs have at least one passing test with the matching `Trait`
- [ ] Zero build warnings; `dotnet format` is clean
- [ ] New public members have XML docs and are in `PublicAPI.Unshipped.txt`
- [ ] No public member that the spec does not require
- [ ] Deviations from the plan are recorded in `plan.md` in this PR
- [ ] CI green; coverage ≥ 80 % line and branch

Closes #
