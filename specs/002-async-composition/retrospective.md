# Retrospective 002 — Async composition

**Date:** 2026-11-09 · **Spec:** [002](spec.md) (Implemented 2026-11-09) · **Tasks:** T00–T03, one PR each

## Outcome

- `MapAsync`, `BindAsync` and `MatchAsync` compose completed and pending results with synchronous
  or asynchronous steps, with spec 001's semantics: every error passed on, no later step after a
  failure, arguments validated **at the call** (before awaiting), exceptions and cancellation
  unchanged, no resume on the caller's context.
- The synchronous `Map` refuses a mapper that returns a `Task` at compile time (CS0619, AC-3.7), so a
  result can't hold an unawaited task by accident.
- All 18 acceptance criteria have tests; traceability runs **per spec** on each spec's own test
  files (spec 002: 18/18, spec 001: 40/40, both without unknown IDs). NFR-1 to NFR-3 tested.
- 174 tests, 100 % line and branch coverage. Every guard was proved able to fail with mutants
  (8 mutants across T01 and T02, including dropping `ConfigureAwait(false)`: 0 → 6 context posts).
- The spec's §2 example and every README sample compile and run in the compatibility tests.

## What went well

- **The plan spike found the one real design flaw before any code**: ADR 0006's promise that a
  synchronous `Map` could never take an async delegate was false (a generic `TOut` accepts a
  `Task`). The fix (an `[Obsolete(error: true)]` overload) was spiked, put to the owner as an
  amendment (AC-3.7) and landed in T01.
- **Delegation kept 26 members to one implementation per operation**: pending members await and call
  the completed-result members, which reuse spec 001's carrying-over (`ToFailure`).
- **Starting T02 on top of T01's branch while T01's CI ran**, then `git rebase --onto main` after
  the merge, kept the work moving without mixing two tasks in one PR.

## What we would change

| Problem | When it showed up | Change for next time |
|---|---|---|
| The first consumer-spike run reported every case as "compiles": the spike library failed an analyzer first, and the filter only looked for errors in the case file | plan phase | Applied to `sdd` (plan phase): declare each call's type and include a **must-fail control case**; check the build succeeded, not only that the case file has no errors |
| The accepted ADR 0006 claimed a guarantee the type system doesn't give | plan phase | Applied to `sdd`: an ADR's consequences about compile-time behaviour are spiked before acceptance, not after |
| The `Map` guard made `Map<string>(_ => null!)` ambiguous (CS0121) — found by spec 001's tests in T01, not by the spike | T01 | Applied to `sdd`: when a plan adds overloads to an existing member, the consumer spike re-runs that member's existing call shapes, including an explicit type argument with an untyped `null` |
| AC IDs repeat across specs (`AC-1.1` in 001 and 002), so one traceability run over `tests/` reports unknown IDs and can let one spec's test "cover" the other's AC | tasks phase | Resolved in tasks 002: `[Trait("Spec", "NNN")]` and per-spec runs (command in `CLAUDE.md`). Applied to `sdd`: decide this in the **first** spec's tasks phase |
| zsh doesn't word-split `$var` and macOS bash 3.2 has no `mapfile`, so the first per-spec run passed one "file" made of all names | T03 | Applied to `sdd-task`: build file lists as bash arrays with `while read`, and run multi-file commands under `bash` |
| A test for AC-3.7 used `GetMethods()` on `Result<>` and also found the inherited guard of `Result` | T01 | Kept as a note: reflection tests on a derived type use `BindingFlags.DeclaredOnly` |

These changes were applied to the `sdd` and `sdd-task` skills.

## Follow-ups

- **Release to nuget.org** — candidate spec 003 (the copied `release.yml`; owner steps: release environment, release-tags ruleset, Trusted Publishing).
- **Side-effect steps** (`Tap`/`OnSuccess`/`OnFailure`, sync and async) — candidate spec 004.
- The traceability per spec is a one-line `bash -c` today; if a third spec arrives, give the `sdd` script a `--trait Spec=NNN` filter instead.
