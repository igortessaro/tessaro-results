# Tasks 004 — Side-effect steps

**Status:** Approved (2026-11-11) · **Created:** 2026-11-11
**Spec:** [004](spec.md) (Implemented 2026-11-11) · **Plan:** [004](plan.md) (Approved 2026-11-11)
**Retrospective:** [retrospective.md](retrospective.md) (2026-11-11)

> Ordered, small, independently verifiable steps that implement plan 004. Each task is **one
> branch and one pull request**. Tasks run in order: each one builds on the merged result of the
> previous one. Execute each with the `sdd-task` skill.

**Progress is tracked on GitHub**, not in this file: one issue per task in milestone
[004 — Side-effect steps](https://github.com/igortessaro/tessaro-results/milestone/4), labelled `task`, on the
[Tessaro.Results board](https://github.com/users/igortessaro/projects/4). This file holds the *what*
of each task; the issue holds its *state* (one source of truth each).

---

## How each task is done

As in [tasks 002](../002-async-composition/tasks.md#how-each-task-is-done): branch `feature/004-…` from
an up-to-date `main`, move the card to **In Progress**, red (tests tagged `[Trait("Spec", "004")]`
and `[Trait("AC", "AC-x.y")]`, seen failing) → green → refactor (XML docs, `PublicAPI.Unshipped.txt`),
consumer check for new public API, local build / tests with the 80 % gate / `dotnet format`, PR
with the task's title (ADR 0005) and `Closes #<issue>`, merge only on the owner's approval after the
head commit's checks are green.

**Consumer checks in this spec** declare the expected type of each call, include a must-fail
control, and re-run the existing call shapes of specs 001–002 (plan 004 §8 R7, R4).

## Definition of done (every task)

- All ACs listed in the task have at least one passing test with the matching traits
- Build has zero warnings (warnings are errors); `dotnet format` is clean
- Every new public member has XML docs (with `<exception>` tags) and is in `PublicAPI.Unshipped.txt`
- No public member exists that the spec does not require
- The tests of specs 001–002 pass unchanged (NFR-4)
- CI is green on the PR's head commit; coverage ≥ 80 % line and branch
- The PR title follows ADR 0005, the PR closes the task's issue, and the card ends in **Done**

---

## Overview

| ID | Issue | Task | Covers | Branch | PR title |
|---|---|---|---|---|---|
| T00 | (this PR) | Spec, plan and tasks | — | `feature/004-docs-spec` | `Spec 004 \| Add side-effect steps spec, plan and tasks` |
| T01 | [#34](https://github.com/igortessaro/tessaro-results/issues/34) | Side-effect steps on completed results | US-1, US-2; US-3, US-4, AC-5.3 (completed receivers); AC-5.1 | `feature/004-completed-side-effects` | `Spec 004 (T01) \| Add OnSuccess and OnFailure on results` |
| T02 | [#35](https://github.com/igortessaro/tessaro-results/issues/35) | Side-effect steps on pending results | US-3, US-4, AC-5.3 (pending receivers); AC-5.2; NFR-2 | `feature/004-pending-side-effects` | `Spec 004 (T02) \| Add OnSuccessAsync and OnFailureAsync on tasks` |
| T03 | [#36](https://github.com/igortessaro/tessaro-results/issues/36) | Consumer checks, docs and closing | NFR-3 | `feature/004-docs-close` | `Spec 004 (T03) \| Document side-effect steps and close spec 004` |

T02's pending members delegate to the members T01 adds (plan §3.4), and both edit
`ResultSideEffectExtensions`, so the tasks run in this order.

### Where each AC lands

US-3, US-4 and AC-5.3 apply to completed **and** pending receivers, so their ACs are split by
receiver; each half is tested in exactly one task.

| AC | T01 (completed receiver) | T02 (pending receiver) |
|---|---|---|
| AC-1.1 – AC-1.5, AC-2.1 – AC-2.3 | ✓ | — |
| AC-3.1, AC-3.3 – AC-3.6 | ✓ completed half (async actions) | ✓ pending half (sync and async actions) |
| AC-3.2, AC-3.7 | — | ✓ (pending receiver only) |
| AC-4.1, AC-4.4 – AC-4.6 | ✓ completed half | ✓ pending half |
| AC-4.2, AC-4.3 | — | ✓ (they are about the receiver task) |
| AC-5.1 | ✓ (synchronous steps only) | — |
| AC-5.2 | — | ✓ |
| AC-5.3 | ✓ completed half (`OnSuccess`, `OnSuccessAsync` on a result) | ✓ pending half (`OnSuccessAsync` on a task) |

---

## Tasks

### T00 — Spec, plan and tasks

- **Goal:** land spec 004, plan 004, this file and the ADR 0006 note through one pull request.
- **Before merging:** create the milestone "004 — Side-effect steps" and one `task` issue per task
  (T01–T03), add them to the board as **Todo**, and fill the issue column above — done 2026-11-11:
  milestone 4, issues #34–#36.
- **Done when:** the PR is merged with every check green.

### T01 — Side-effect steps on completed results

- **Goal:** a completed `Result` / `Result<T>` takes `OnSuccess`, `OnFailure` and, with async
  actions, `OnSuccessAsync`, `OnFailureAsync`; the synchronous steps refuse `Task`-returning actions
  and the success steps refuse result-returning actions at compile time.
- **Covers:** AC-1.1 – AC-1.5, AC-2.1 – AC-2.3; completed half of AC-3.1, AC-3.3 – AC-3.6,
  AC-4.1, AC-4.4 – AC-4.6, AC-5.3; AC-5.1 · plan §3.1, §3.3 (Task guards and the 2 + 2 result
  guards on completed receivers), §3.4.
- **Notes:** `ResultSideEffectExtensions.cs` and `ResultSideEffectExtensions.Guards.cs`. Generic
  members constrained to `Result` return the receiver itself; a completed result on the other
  branch returns `Task.FromResult(result)`. Throwing actions in tests are method calls, not
  throw-only lambdas (plan §3.4). Guards are asserted by reflection (`ObsoleteAttribute { IsError: true }`
  and message; invoking one throws `NotSupportedException`).
- **Done when:** the PR is merged, and the consumer check shows CS0619 for the 9 Task-guard shapes
  of plan §8 R1 and the 4 completed result-guard shapes, plus the must-fail control.

### T02 — Side-effect steps on pending results

- **Goal:** `Task<Result>` and `Task<Result<T>>` take `OnSuccessAsync` and `OnFailureAsync` with
  sync and async actions, awaited, without resuming on the caller's context.
- **Covers:** AC-3.2, AC-3.7, AC-4.2, AC-4.3, AC-5.2; pending half of AC-3.1, AC-3.3 – AC-3.6,
  AC-4.1, AC-4.4 – AC-4.6, AC-5.3; NFR-2 · plan §3.2, §3.3 (4 pending result guards), §3.4, §5.
- **Notes:** `ResultSideEffectExtensions.Pending.cs`; every member validates, awaits the receiver
  through `ResultTaskExtensions.Completed` (made `internal`, plan §3.4) and delegates to T01's
  members. "Before awaiting" uses a never-completing `TaskCompletionSource`; "awaited" (AC-3.6,
  AC-5.2) a gate the test releases. The NFR-2 guard is proved able to fail by removing
  `ConfigureAwait(false)` once.
- **Done when:** the PR is merged, and the consumer check compiles the pending binding cases of
  plan §8 R2/R3 with their declared types (spec §2 chain included), shows CS0619 for the 4 pending
  result-guard shapes, plus the must-fail control.

### T03 — Consumer checks, docs and closing

- **Goal:** side-effect steps are documented and permanently checked from outside; spec 004 closes.
- **Covers:** NFR-3 · plan §5 (Compatibility).
- **Steps:**
  1. `tests/.../Compatibility/SideEffectSamples.cs`: the spec §2 handler and worker from
     `Consumer.Compat` (with ASP.NET Core) and a chain inside `Tessaro.Compat` next to
     `Tessaro.Optional`.
  2. README: a "Side effects" section (`OnSuccess`/`OnFailure`, `…Async`, same instance, never
     transforms, the guards), every sample compiled in the compatibility tests; replace "side-effect
     steps (`Tap`)" in the known limitations with the plan §3.4 limitations (overloaded method
     groups, throw-only lambdas, block lambdas discarding a task, `ValueTask` actions).
  3. AC traceability for spec 004 on its own test files (command in `CLAUDE.md`, with
     `Trait("Spec", "004")` and `specs/004-side-effect-steps/spec.md`), and again for 001 and 002.
     *Done 2026-11-11 with the script's new `--files-with`/`--files-without` filters (retrospective).*
  4. Tick the review checklists; spec 004 `Implemented (YYYY-MM-DD)`; `retrospective.md`, and apply
     its lessons to the skills.
- **Done when:** the PR is merged and the milestone is closed.

Releasing the new members (a `0.2.0` minor, per `CONTRIBUTING.md`) is not part of spec 004; it
follows the release routine once the owner decides.

---

## Resolved questions (2026-11-11, owner approved the recommendations)

1. **Three implementation tasks, split by receiver** as in spec 002 (T02 delegates to T01's members).
2. **The T00 PR is titled `Spec 004 | Add side-effect steps spec, plan and tasks`.**

## Review checklist

- [x] Every AC of the spec lands in exactly one task (or is split by receiver, stated in both tasks)
- [x] Tasks are ordered by dependency; tasks sharing files run in sequence
- [x] Owner-only steps are named inside the task that needs them (none in this spec)
- [x] Each task has a branch, a PR title in the ADR 0005 format and a "done when"
- [x] The closing task covers docs, AC traceability, checklists, status and retrospective
