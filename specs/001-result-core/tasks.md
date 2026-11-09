# Tasks 001 — Result core

**Status:** Approved (2026-11-09) · **Created:** 2026-11-09
**Spec:** [001](spec.md) (Approved 2026-11-09) · **Plan:** [001](plan.md) (Approved 2026-11-09)

> Ordered, small, independently verifiable steps that implement plan 001. Each task is **one
> branch and one pull request**. Tasks run in order: each one builds on the merged result of the
> previous one. Execute each with the `sdd-task` skill.

**Progress is tracked on GitHub**, not in this file: one issue per task in milestone
[001 — Result core](https://github.com/igortessaro/tessaro-results/milestone/1), labelled `task`. This file holds the *what* of each task;
the issue holds its *state* (one source of truth each).

---

## How each task is done

1. Create the branch named in the task from an up-to-date `main` (trunk-based; Gitflow prefixes,
   always `feature/001-…` for this spec).
2. **Red** — write the tests for the task's acceptance criteria first, each tagged
   `[Trait("AC", "AC-x.y")]`, and see them fail (or fail to compile).
3. **Green** — write the simplest code that makes them pass, following plan §3 and the
   behaviour table in plan §3.4.
4. **Refactor** — clean up with the tests green; XML docs (with `<exception>` tags) and
   `PublicAPI.Unshipped.txt` updated.
5. **Consumer check** for tasks that add public API: the `Compatibility/` tests (from T05; before
   that, a throwaway consumer project in the scratchpad) compile the new members from outside
   the `Tessaro` namespace.
6. Run locally:
   ```bash
   dotnet build Tessaro.Results.slnx -c Release
   dotnet test --solution Tessaro.Results.slnx -c Release --coverlet --coverlet-output-format opencover --coverlet-threshold 80 --coverlet-threshold-type line --coverlet-threshold-type branch
   dotnet format Tessaro.Results.slnx --verify-no-changes
   ```
7. Push and open the PR with the task's title (ADR 0005); the body follows the PR template and
   ends with `Closes #<issue>`.
8. Before merging, read the checks **for the PR's head SHA**. The owner squash-merges (subject =
   PR title + ` (#PR)`, no `Co-Authored-By`), or tells Claude explicitly to merge one PR.

## Definition of done (every task)

- All ACs listed in the task have at least one passing test with the matching `Trait`
- Build has zero warnings (warnings are errors); `dotnet format` is clean
- Every new public member has XML docs, including `<exception>` tags, and is in `PublicAPI.Unshipped.txt`
- No public member exists that the spec does not require
- CI is green on the PR's head commit (from T01 on); coverage ≥ 80 % line and branch
- The PR title follows ADR 0005 and the PR closes the task's issue

---

## Overview

| ID | Issue | Task | Covers | Branch | PR title |
|---|---|---|---|---|---|
| T00 | (this PR) | Task list | — | `feature/001-tasks` | `Spec 001 \| Add the task list for spec 001` |
| T01 | [#1](https://github.com/igortessaro/tessaro-results/issues/1) | Shared setup, scaffolding and errors | US-3, NFR-1 | `feature/001-scaffold-errors` | `Spec 001 (T01) \| Scaffold the solution and add Error` |
| T02 | [#2](https://github.com/igortessaro/tessaro-results/issues/2) | Create and inspect results | US-1, US-2, US-4, NFR-2 | `feature/001-create-results` | `Spec 001 (T02) \| Create and inspect results` |
| T03 | [#3](https://github.com/igortessaro/tessaro-results/issues/3) | Result hierarchy | US-5 | `feature/001-hierarchy` | `Spec 001 (T03) \| Make Result<T> a Result` |
| T04 | [#4](https://github.com/igortessaro/tessaro-results/issues/4) | Transform and chain | US-6 | `feature/001-map-bind` | `Spec 001 (T04) \| Add Map and Bind` |
| T05 | [#5](https://github.com/igortessaro/tessaro-results/issues/5) | Branch on the outcome and consumer checks | US-7, NFR-3 | `feature/001-match` | `Spec 001 (T05) \| Add Match and consumer checks` |
| T06 | [#6](https://github.com/igortessaro/tessaro-results/issues/6) | Documentation and closing | — | `feature/001-docs` | `Spec 001 (T06) \| Document Result usage and close spec 001` |

T02–T05 all edit `Result.cs` / `ResultOfT.cs` and their partial files, so they run strictly in
sequence even where the stories are independent.

---

## Before T00 — repository creation

| # | Step | Who |
|---|---|---|
| 1 | `git init -b main`; commit the approved planning documents (`CLAUDE.md`, `docs/`, `specs/001-result-core/spec.md`, `plan.md`) as `docs: add constitution, ADRs, spec and plan for 001-result-core` (no `Co-Authored-By`) — done 2026-11-09, `68125f7` | Claude |
| 2 | Create the public repository and push `main`: `gh repo create igortessaro/tessaro-results --public --source . --push` (run from the repository root) — done 2026-11-09 | Owner |
| 3 | Create the milestone, the `task` label and one issue per task (T01–T06), then fill the issue column above in T00 — done 2026-11-09, issues #1–#6 | Claude |

This one-off commit lands on `main` before any protection exists, exactly as tessaro-optional's
first commit did; every later change goes through a pull request.

## Tasks

### T00 — Task list

- **Goal:** land this file through the first pull request.
- **Done when:** the PR is merged, with the issue numbers filled in the overview.

### T01 — Shared setup, scaffolding and errors

- **Goal:** a building, tested, CI-checked solution from the first PR, with the error model.
- **Covers:** AC-3.1 – AC-3.5 · NFR-1 · plan §1, §2, §4, §6, §8 R3, R4.
- **Steps:**
  1. Make sure the tessaro-optional checkout is on an up-to-date `main` (ask the owner before pulling).
  2. Run the `tessaro-bootstrap` skill with `--name Results` (dry run first); adapt every reported
     line. Keep the copied `release.yml` dormant (release spec).
  3. Create `Tessaro.Results.slnx`, `src/Tessaro.Results/Tessaro.Results.csproj` (`net10.0`, own
     description and tags, plan §4), `tests/Tessaro.Results.Tests/` (xUnit v3 on MTP, Shouldly,
     coverlet.MTP, `Microsoft.AspNetCore.App` framework reference and `Tessaro.Optional` package
     reference for T05), empty `PublicAPI.*.txt`, a short `README.md`.
  4. Red → green: `ErrorType`, `Error` (CA1716 suppressed on the type only), `ErrorTests`, and the
     NFR-1 test in `NonFunctionalTests` (library references only BCL assemblies).
  5. Toolchain check end to end: build, test with the coverage gate, format, `dotnet pack` (no
     dependency groups), and `bash tests/scripts/*.test.sh`.
- **Why errors land here:** a test project with no tests exits with code 8 (MTP "zero tests ran"),
  which would fail the first CI run. Shipping US-3 with the scaffolding makes the very first PR
  green and lets the required checks be registered right after it.
- **Owner-only (before the PR's CI can pass):** create the SonarCloud project
  `igortessaro_tessaro-results`, turn off automatic analysis, and
  `gh secret set SONAR_TOKEN --repo igortessaro/tessaro-results`.
- **Owner-only (after this PR's CI ran green once):** import the `main` ruleset:
  `gh api -X POST repos/igortessaro/tessaro-results/rulesets --input .github/rulesets/main.json`.
- **Done when:** the PR is merged with every check green, and the ruleset requires those checks.

### T02 — Create and inspect results

- **Goal:** successes and failures can be created, inspected and logged.
- **Covers:** AC-1.1 – AC-1.4 · AC-2.1 – AC-2.9 · AC-4.1 – AC-4.4 · NFR-2 · plan §3.2–§3.4.
- **Notes:** `Result`, `Result<T>` (state, `Value`, factories, implicit conversions, `ToString`);
  errors stored in a `ReadOnlyCollection` (plan §3.4, R6) with a test on the indexer setter;
  NFR-2 test in `NonFunctionalTests`.
- **Done when:** the PR is merged.

### T03 — Result hierarchy

- **Goal:** a `Result<T>` is usable as a `Result`, and failures carry over between shapes.
- **Covers:** AC-5.1 – AC-5.5 · plan §3.2 (`ToFailure<T>`), §3.4.
- **Notes:** AC-5.5 is shown by reflection (no public or protected constructor; `Result<T>` sealed).
- **Done when:** the PR is merged.

### T04 — Transform and chain

- **Goal:** `Map` and `Bind` on both shapes, propagating every error.
- **Covers:** AC-6.1 – AC-6.9 · plan §3.2, §3.3, §3.4.
- **Notes:** delegate validation tested in both states; "not called" asserted with a counting delegate.
- **Done when:** the PR is merged.

### T05 — Branch on the outcome and consumer checks

- **Goal:** `Match` on both shapes, and the permanent compile-only consumer checks.
- **Covers:** AC-7.1 – AC-7.4 · NFR-3 · plan §5 (Compatibility), §8 R2.
- **Done when:** the PR is merged, and the `Compatibility/` tests compile the "Success looks like"
  example of the spec from `Consumer.Compat` (with `Microsoft.AspNetCore.Http`) and `Tessaro.Compat`
  (with `Tessaro.Optional`).

### T06 — Documentation and closing

- **Goal:** consumers can adopt the library, and spec 001 is closed with evidence.
- **Steps:**
  1. `README.md`: install, `using Tessaro;`, the main flows, the limitations of plan §3.4
     (interface conversions, `Result<Error>`, `Result<T>.Ok()`), and the migration checklist from
     spec §5 with the recommended category → status mapping. Every code sample is compiled in the
     `Compatibility/` tests.
  2. AC traceability:
     `bash ~/.claude/skills/sdd/scripts/check-ac-traceability.sh --marker 'Trait\("AC"' specs/001-result-core/spec.md tests`
     — every AC covered, no unknown IDs.
  3. Tick the review checklists of spec, plan and tasks; set the spec to `Implemented (YYYY-MM-DD)`.
  4. Write `specs/001-result-core/retrospective.md` (what to change in the workflow, per phase) and
     apply those changes to the `sdd`, `sdd-task` and `tessaro-bootstrap` skills.
- **Done when:** the PR is merged and the milestone is closed.

---

## Resolved questions (2026-11-09, owner approved the recommendations)

1. **Tasks are tracked as GitHub issues** (milestone 1, label `task`), created right after the
   repository; this file has no checkboxes, so the two never disagree.
2. **`Error` lands in T01 with the scaffolding**, so the first CI run has real tests instead of
   failing with "zero tests ran" (exit code 8), the problem tessaro-optional had in T01–T03.

## Review checklist

- [x] Every AC of the spec lands in exactly one task
- [x] Tasks are ordered by dependency; tasks sharing files run in sequence
- [x] Owner-only steps are named inside the task that needs them
- [x] Each task has a branch, a PR title in the ADR 0005 format and a "done when"
- [x] The closing task covers docs, AC traceability, checklists, status and retrospective
