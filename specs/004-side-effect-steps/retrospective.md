# Retrospective 004 — Side-effect steps

**Date:** 2026-11-11 · **Spec:** [004](spec.md) (Implemented 2026-11-11) · **Tasks:** T00–T03, one PR each
(#33, #37, #38 and this one)

## Outcome

- `OnSuccess` and `OnFailure`, and `OnSuccessAsync` / `OnFailureAsync`, work on completed and pending
  `Result` / `Result<T>`. Each step runs a side effect on one outcome and passes the **same instance**
  on, with its static type. Arguments are validated at the call, async actions are awaited without
  resuming on the caller's context, and exceptions and cancellation are never turned into failures.
- 23 public members in one static class (`ResultSideEffectExtensions`): 12 working members and
  11 compile-time guards. The guards turn two silent mistakes into CS0619: a `Task`-returning action in a
  sync step (never awaited, or `async void`), and a result-returning action in a success step (its
  errors lost).
- All 24 acceptance criteria have tests. Traceability runs per spec (001: 40/40, 002: 18/18,
  004: 24/24, no unknown IDs). NFR-1 to NFR-4 are tested. 220 tests, 100 % line and branch coverage.
  The spec §2 handler and worker, and every README sample, compile and run in the compatibility tests.

## What went well

- **The plan spike found a gap the spec had missed.** `OnSuccessAsync(o => repository.UpdateAsync(o))`
  compiled, awaited the update and silently dropped its failure. The fix (8 result guards) was put to
  the owner as plan open question 1, approved, and landed as AC-5.3 before any code.
- **Generic extensions constrained to `Result`** gave one implementation per step for both shapes and
  kept `Result<T>` in, `Result<T>` out with no covariant overrides. The spike checked the risks first:
  constraint-failing candidates are dropped, and a `Task<…>` receiver never matches the `TResult` member.
- **Pending members delegate to the completed ones**, and the existing `Completed` helper was made
  `internal` instead of copied, so the null-receiver rule has one source.
- **Every limitation in the README was checked against the compiler** before it was written: 9 claims,
  including the ones that *do* compile (block lambdas, `ValueTask`).

## What we would change

| Problem | When it showed up | Change for next time |
|---|---|---|
| The spec guarded the `Task` mistake but not its sibling: a result-returning action dropped its errors. The plan spike found it, not the spec review | plan phase | Applied to `sdd` (plan phase): when a guard stops one silent mistake, spike the other return types the same delegate would drop |
| An untyped `null` (`OnFailure(null!)`) bound to the `Task` guard and broke the AC-4.1 tests (CS0619); the spike had only re-run *existing* members' `null` shapes | T01 | Applied to `sdd` (consumer spike): pass an untyped `null` to each **new** member too |
| The first NFR-2 test drove only a pending chain, so a mutant removing `ConfigureAwait(false)` from the completed-result path survived | T02 | Applied to `sdd-task`: when a property is implemented in several places, mutate each one separately |
| Per-spec traceability needed a hand-written `bash -c` file list (spec 002's follow-up) | T03 | Applied: `check-ac-traceability.sh` gained `--files-with` / `--files-without`, tested positive, negative, empty and with spaces in paths; `CLAUDE.md` and `sdd` use them |
| `CA1848` flagged the spec's own `logger.LogWarning(…)` sample | T03 | Kept as a note: narrow `#pragma` around the two sample classes, with the reason; don't change the spec's sample to satisfy a performance rule |

These changes were applied to the `sdd` and `sdd-task` skills.

## Follow-ups

- **Release `0.2.0`**, a minor with the new members, following `CONTRIBUTING.md`. Not part of spec 004;
  it's the owner's call when to ship.
- Possible later specs, from §6: a side effect on either outcome (`Tap(result => …)`), side effects that
  can fail the chain (`Check`), `Ensure`, `MapErrors`. Revisit if a service needs them.
- A Roslyn analyzer for tasks discarded inside block lambdas (plan §10) — only if a service hits it.
