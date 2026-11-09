# Tasks 002 — Async composition

**Status:** Approved (2026-11-09) · **Created:** 2026-11-09
**Spec:** [002](spec.md) (Implemented 2026-11-09) · **Plan:** [002](plan.md) (Approved 2026-11-09)
**Retrospective:** [retrospective.md](retrospective.md) (2026-11-09)

> Ordered, small, independently verifiable steps that implement plan 002. Each task is **one
> branch and one pull request**. Tasks run in order: each one builds on the merged result of the
> previous one. Execute each with the `sdd-task` skill.

**Progress is tracked on GitHub**, not in this file: one issue per task in milestone
[002 — Async composition](https://github.com/igortessaro/tessaro-results/milestone/2), labelled `task`, on the
[Tessaro.Results board](https://github.com/users/igortessaro/projects/4). This file holds the *what*
of each task; the issue holds its *state* (one source of truth each).

---

## How each task is done

As in [tasks 001](../001-result-core/tasks.md#how-each-task-is-done): branch `feature/002-…` from an
up-to-date `main`, move the card to **In Progress**, red (tests tagged `[Trait("AC", "AC-x.y")]`,
seen failing) → green → refactor (XML docs, `PublicAPI.Unshipped.txt`), consumer check for new
public API, local build / tests with the 80 % gate / `dotnet format`, PR with the task's title
(ADR 0005) and `Closes #<issue>`, merge only on the owner's approval after the head commit's
checks are green.

**Consumer checks in this spec** declare the expected type of each call and include a case that
must fail (plan 002 §8 R7).

## Definition of done (every task)

- All ACs listed in the task have at least one passing test with the matching `Trait`
- Build has zero warnings (warnings are errors); `dotnet format` is clean
- Every new public member has XML docs (with `<exception>` tags) and is in `PublicAPI.Unshipped.txt`
- No public member exists that the spec does not require
- CI is green on the PR's head commit; coverage ≥ 80 % line and branch
- The PR title follows ADR 0005, the PR closes the task's issue, and the card ends in **Done**

---

## Overview

| ID | Issue | Task | Covers | Branch | PR title |
|---|---|---|---|---|---|
| T00 | (this PR) | Spec, plan and tasks | — | `feature/002-docs-spec` | `Spec 002 \| Add async composition spec, plan and tasks` |
| T01 | [#16](https://github.com/igortessaro/tessaro-results/issues/16) | Async steps on completed results | US-2 and US-3 (completed receivers), AC-3.7 | `feature/002-completed-async` | `Spec 002 (T01) \| Add async steps on completed results` |
| T02 | [#17](https://github.com/igortessaro/tessaro-results/issues/17) | Members on pending results | US-1, US-2 and US-3 (pending receivers), NFR-2 | `feature/002-pending-results` | `Spec 002 (T02) \| Add MapAsync, BindAsync and MatchAsync on tasks` |
| T03 | [#18](https://github.com/igortessaro/tessaro-results/issues/18) | Consumer checks, docs and closing | NFR-3 | `feature/002-docs-close` | `Spec 002 (T03) \| Document async composition and close spec 002` |

T02 calls the members T01 adds (plan §3.4: pending members delegate to completed-result members),
so the tasks run in this order.

### Where each AC lands

US-2 and US-3 apply to completed **and** pending receivers, so their ACs are split by receiver;
each half is tested in exactly one task.

| AC | T01 (completed receiver) | T02 (pending receiver) |
|---|---|---|
| AC-1.1 – AC-1.5 | — | ✓ |
| AC-2.1 – AC-2.5 | ✓ completed half | ✓ pending half |
| AC-2.6 | — | ✓ (chains of `BindAsync` on pending results) |
| AC-3.1, AC-3.4, AC-3.5, AC-3.6 | ✓ completed half (steps) | ✓ pending half (receiver and steps) |
| AC-3.2, AC-3.3 | — | ✓ (they are about the receiver task) |
| AC-3.7 | ✓ | — |

---

## Tasks

### T00 — Spec, plan and tasks

- **Goal:** land spec 002, plan 002, this file and ADR 0006 through one pull request.
- **Before the PR:** create the milestone "002 — Async composition" and one `task` issue per task
  (T01–T03), add them to the board as **Todo**, and fill the issue column above — done 2026-11-09:
  milestone 2, issues #16–#18.
- **Done when:** the PR is merged with every check green.

### T01 — Async steps on completed results

- **Goal:** a completed `Result` / `Result<T>` takes async steps; the synchronous `Map` refuses an
  async mapper at compile time.
- **Covers:** completed half of AC-2.1 – AC-2.5, AC-3.1, AC-3.4 – AC-3.6; AC-3.7 · plan §3.1, §3.3, §3.4.
- **Notes:** `Result.Async.cs`, `ResultOfT.Async.cs`, `Result.AsyncGuard.cs`. Non-`async` wrappers
  with `async` local functions; a completed failure returns `Task.FromResult(…)` without calling the
  step. AC-3.7 is asserted by reflection (`ObsoleteAttribute { IsError: true }` and its message), and
  the consumer check shows CS0619 at a call site.
- **Done when:** the PR is merged.

### T02 — Members on pending results

- **Goal:** `Task<Result>` and `Task<Result<T>>` take `MapAsync`, `BindAsync` and `MatchAsync` with
  sync and async steps, and the library never resumes on the caller's context.
- **Covers:** AC-1.1 – AC-1.5; pending half of AC-2.1 – AC-2.5, AC-3.1, AC-3.4 – AC-3.6; AC-2.6,
  AC-3.2, AC-3.3; NFR-2 · plan §3.2, §3.4, §5.
- **Notes:** `ResultTaskExtensions.cs` (on `Task<Result>`) and `ResultTaskExtensions.OfT.cs`
  (on `Task<Result<T>>`); every member awaits the receiver (`ConfigureAwait(false)`, null check) and
  delegates to the spec 001 or T01 member. "Before awaiting" is asserted with a never-completing
  `TaskCompletionSource`. The NFR-2 guard (counting `SynchronizationContext`) is proved able to fail
  by removing `ConfigureAwait(false)` once.
- **Done when:** the PR is merged, and the consumer check compiles the 16 binding cases of plan §8
  R1 with their declared types, plus the must-fail control.

### T03 — Consumer checks, docs and closing

- **Goal:** async composition is documented and permanently checked from outside; spec 002 closes.
- **Covers:** NFR-3 · plan §5 (Compatibility).
- **Steps:**
  1. `tests/.../Compatibility/AsyncSamples.cs`: the spec §2 handler and controller from
     `Consumer.Compat` (with ASP.NET Core), and an async chain inside `Tessaro.Compat` next to
     `Tessaro.Optional`.
  2. README: an "Async" section (pending results, async steps, `MatchAsync`, the `Async` naming rule,
     the `Map` guard, "validated before awaiting"), every sample compiled in `ReadmeSamples.cs` /
     `AsyncSamples.cs`; remove "No async helpers yet" from the known limitations.
  3. AC traceability for **both** specs:
     `bash ~/.claude/skills/sdd/scripts/check-ac-traceability.sh --marker 'Trait\("AC"' specs/002-async-composition/spec.md tests`
     (and again for spec 001). Test traits of the two specs share the `AC-x.y` format, so each test
     file of spec 002 also carries `[Trait("Spec", "002")]` and the check runs per spec on its own
     test files (resolved question 1).
  4. Tick the review checklists; spec 002 `Implemented (YYYY-MM-DD)`; `retrospective.md`, and apply
     its lessons to the skills.
- **Done when:** the PR is merged and the milestone is closed.

---

## Resolved questions (2026-11-09, owner approved the recommendations)

1. **Spec 002 tests carry `[Trait("Spec", "002")]`** and live in their own files
   (`PendingResultTests`, `AsyncStepTests`, `AsyncFailFastTests`, `Compatibility/AsyncSamples.cs`);
   the traceability script runs per spec on that spec's test files, so repeated AC IDs across
   specs can't cover each other or show up as unknown.
2. **Three implementation tasks**, split by receiver (T02 calls the members T01 adds).

## Review checklist

- [x] Every AC of the spec lands in exactly one task (or is split by receiver, stated in both tasks)
- [x] Tasks are ordered by dependency; tasks sharing files run in sequence
- [x] Owner-only steps are named inside the task that needs them (none in this spec)
- [x] Each task has a branch, a PR title in the ADR 0005 format and a "done when"
- [x] The closing task covers docs, AC traceability, checklists, status and retrospective
