# Tasks 003 — NuGet release

**Status:** Approved (2026-11-09) · **Created:** 2026-11-09
**Spec:** [003](spec.md) (Approved 2026-11-09) · **Plan:** [003](plan.md) (Approved 2026-11-09)

> Ordered, small, independently verifiable steps that deliver plan 003. Each task is **one branch
> and one pull request**; some tasks finish only after an owner action that follows the merge (a tag,
> an approval), so their PR says `Part of #N` and the issue is closed by hand when "done when" is met.
> Execute each with the `sdd-task` skill.

**Progress is tracked on GitHub**, not in this file: one issue per task in milestone
[003 — NuGet release](https://github.com/igortessaro/tessaro-results/milestone/3), labelled `task`, on the
[Tessaro.Results board](https://github.com/users/igortessaro/projects/4) (one source of truth for state).

---

## How each task is done

As in [tasks 001](../001-result-core/tasks.md#how-each-task-is-done), plus, for this spec:

- **Evidence goes into `verification.md`** (plan §4) as soon as it exists — a run URL, a command and
  its output, a page — in the reference's *How / Evidence* format; rows not yet shown are marked
  **Pending** and the traceability check is run with `--allow-pending` until T04.
- **Owner-only steps** (nuget.org, the `NUGET_USER` secret, tag pushes, approvals) are named in the
  task that needs them, with the exact command. Claude reads the resulting state (run, attempt,
  pending deployments) before asking for the next step.

## Definition of done (every task)

- The task's ACs have evidence in `verification.md` (or a passing script test it links to)
- CI is green on the PR's head commit
- The PR title follows ADR 0005; the PR closes (or is `Part of`) the task's issue; the card ends in **Done**

---

## Overview

| ID | Issue | Task | Covers | Branch | PR title |
|---|---|---|---|---|---|
| T00 | (this PR) | Spec, plan and tasks | — | `feature/003-docs-spec` | `Spec 003 \| Add NuGet release spec, plan and tasks` |
| T01 | [#23](https://github.com/igortessaro/tessaro-results/issues/23) | Pre-release docs | AC-4.2, AC-5.1 – AC-5.3 | `feature/003-release-docs` | `Spec 003 (T01) \| Prepare docs for the first release` |
| T02 | [#24](https://github.com/igortessaro/tessaro-results/issues/24) | One-time setup and `0.1.0-rc.1` | AC-1.1, 1.2, 1.6, 1.7, 2.1 – 2.3, 3.1 – 3.4, 3.6, 4.1 | `feature/003-release-rc` | `Spec 003 (T02) \| Prepare release v0.1.0-rc.1` |
| T03 | [#25](https://github.com/igortessaro/tessaro-results/issues/25) | Publish `0.1.0` | — (final version of the verified rc) | `feature/003-release-final` | `Spec 003 (T03) \| Prepare release v0.1.0` |
| T04 | [#26](https://github.com/igortessaro/tessaro-results/issues/26) | Negative paths, verification and closing | AC-1.3, 1.4, 1.5, 3.5 | `feature/003-verify-close` | `Spec 003 (T04) \| Verify the release and close spec 003` |

Every AC is shown by exactly one task; T04 also completes `verification.md` with the evidence the
earlier tasks recorded. Tasks run in order: each one needs the previous release state.

---

## Tasks

### T00 — Spec, plan and tasks

- **Before the PR:** milestone "003 — NuGet release", one `task` issue per task (T01–T04) on the board as **Todo**, issue column filled. — done 2026-11-09: milestone 3, issues #23–#26.
- **Done when:** the PR is merged.

### T01 — Pre-release docs

- **Goal:** the docs that ship in the first package are right, and the release routine describes this repository.
- **Covers:** AC-4.2; AC-5.1 – AC-5.3 (evidence recorded) · plan §2.
- **Steps:**
  1. `README.md`: remove "Not on nuget.org yet"; install `dotnet add package Tessaro.Results --prerelease`
     (with a line saying `--prerelease` is needed while only release candidates exist); NuGet badge.
  2. `CONTRIBUTING.md`: "One-time setup (already done…)" → a checklist of plan §5 with a *Done* column
     (empty until T02); version examples stay `0.1.0`.
  3. `verification.md`: every AC of the spec with its planned *How*; AC-5.1 – AC-5.3 filled
     (script tests, PRs #7–#22, Dependabot PR #9); all others **Pending**.
- **Done when:** the PR is merged.

### T02 — One-time setup and `0.1.0-rc.1`

- **Goal:** the first release candidate is on nuget.org and every positive-path AC is shown.
- **Covers:** AC-1.1, AC-1.2, AC-1.6, AC-1.7, AC-2.1 – AC-2.3, AC-3.1 – AC-3.4, AC-3.6, AC-4.1 · plan §3, §5.
- **Owner-only, before the PR merges:** plan §5 steps 1–3 — check/request the `Tessaro` prefix;
  Trusted Publishing policy (package `Tessaro.Results`, repository `igortessaro/tessaro-results`,
  workflow `release.yml`, environment `release`); `gh secret set NUGET_USER --repo igortessaro/tessaro-results`.
- **Claude, after the owner confirms:** plan §5 steps 4–5 — the `release` environment (reviewer
  `igortessaro`, `v*` tags only) and the `release-tags` ruleset; read both back.
- **PR:** move `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt` (AC-4.1); `CONTRIBUTING.md` setup
  checklist dated; body says `Part of #N`.
- **Owner-only, after the merge:** `git tag v0.1.0-rc.1 <merge commit> && git push origin v0.1.0-rc.1`;
  while `publish` waits, Claude records AC-1.6 (package, page and Release "not found"); then the owner
  approves.
- **Then Claude:** records AC-1.1, 1.2, 1.7, 2.1 – 2.3, 3.1 – 3.4, 3.6 in `verification.md` (on T03's branch).
- **Done when:** `0.1.0-rc.1` is on nuget.org and a fresh consumer adds it with the README's command; issue closed by hand.

### T03 — Publish `0.1.0`

- **Goal:** the verified rc becomes the first stable version.
- **PR:** README install without `--prerelease`; the T02 evidence in `verification.md`; `Part of #N`.
- **Owner-only, after the merge:** `git tag v0.1.0 <merge commit> && git push origin v0.1.0`, approve.
- **Done when:** `0.1.0` is on nuget.org, its GitHub Release is marked *latest*, and `dotnet add package Tessaro.Results` (no flag) installs it; issue closed by hand.

### T04 — Negative paths, verification and closing

- **Goal:** the failure paths are shown on the real pipeline and spec 003 closes with full evidence.
- **Covers:** AC-1.3, AC-1.4, AC-1.5, AC-3.5 · plan §4.
- **Steps:**
  1. AC-1.3: owner tags a throwaway branch commit `v0.0.0-guard.1` and pushes it; the run's `guard`
     fails with "not on main" and every later job is skipped (also AC-1.4); the tag and branch are
     deleted afterwards.
  2. AC-1.5: re-run the `publish` job of the `v0.1.0` run → "already exists"; nothing changes.
  3. AC-3.5: repeat the plan's local spike against the **real** `0.1.0` from nuget.org (remove a member, `v0.1.1` → CP0002; `v0.2.0` → packs).
  4. `verification.md` complete (no **Pending**); traceability:
     `bash ~/.claude/skills/sdd/scripts/check-ac-traceability.sh --marker '^\| AC-' specs/003-nuget-release/spec.md specs/003-nuget-release/verification.md`.
  5. Checklists ticked; spec `Implemented (YYYY-MM-DD)`; `retrospective.md`, lessons applied to the skills; propose spec 004/005 as follow-ups.
- **Done when:** the PR is merged and the milestone is closed.

---

## Resolved questions (2026-11-09, owner approved the recommendations)

1. **Two release tasks:** `0.1.0-rc.1` in T02, `0.1.0` in T03 — the README packed into `0.1.0` loses
   `--prerelease` in its own merge before the tag.
2. **AC evidence lives in `verification.md`** (linking the script tests where they apply), as in the reference.

## Review checklist

- [x] Every AC of the spec lands in exactly one task
- [x] Tasks are ordered by dependency (each needs the previous release state)
- [x] Owner-only steps are named inside the task that needs them, with exact commands
- [x] Each task has a branch, a PR title in the ADR 0005 format and a "done when"
- [x] The closing task covers verification, traceability, checklists, status and retrospective
