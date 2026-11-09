# Retrospective 001 — Result core

**Date:** 2026-11-09 · **Spec:** [001](spec.md) (Implemented 2026-11-09) · **Tasks:** T00–T06, one PR each
(plus two `Chore` PRs for SonarCloud findings and one `Deps` PR)

## Outcome

- `Result`, `Result<T>` and `Error` replace the copied `Response.cs`: a `Result<T>` **is a**
  `Result` (ADR 0001), errors carry a category instead of an HTTP status (ADR 0003), and every
  operation keeps all errors.
- All 40 acceptance criteria have at least one test tagged with their ID (checked with the `sdd`
  traceability script: none missing, none unknown); NFR-1 to NFR-3 have 1, 2 and 3 tests.
- 135 tests on `net10.0`, 100 % line and branch coverage; CI enforces ≥ 80 %. SonarCloud: no bugs,
  vulnerabilities or hotspots; all code smells cleared.
- Every guard was proved able to fail with mutants (immutability, carrying errors over, delegate
  validation, null returns, `Match`, and a deliberate name clash with ASP.NET Core's `Results`).
- Every README sample is compiled and run in `tests/.../Compatibility/ReadmeSamples.cs`.

## What went well

- **Spikes before the plan was approved** found three things that would otherwise have surfaced
  in a task: `Errors` was mutable through an `IList` cast (an array behind `IReadOnlyList`),
  `Error` triggers CA1716, and implicit conversions don't apply to interface-typed values.
- **The bootstrap skill** turned the shared setup into one command plus a short review; the first
  PR ran every CI gate green.
- **Shipping `Error` with the scaffolding (T01)** avoided the "zero tests ran" CI failure that
  tessaro-optional had in its first tasks.
- **Consumer checks inside the solution (T05)** make the namespace/ASP.NET Core compatibility a
  permanent test instead of a one-off spike.

## What we would change

| Problem | When it showed up | Change for next time |
|---|---|---|
| The bootstrap report missed a runtime line (`dotnet-version: 8.0.x`) after the new package moved to `net10.0` only | T01 | `tessaro-bootstrap` now also flags target-framework and runtime-version lines (done in T01, with a test) |
| Copied script tests used `ac "AC-x.y"` labels from tessaro-optional's spec 002, colliding with this spec's AC IDs | T01 | Relabelled here; bootstrap step 4 already says to drop foreign AC tags |
| The first CI run on `main` failed the SonarCloud quality gate: the first `main` analysis has no new-code baseline yet (tessaro-optional had the same) | after T01 | Expected once per repository; `sdd-task` and `tessaro-bootstrap` now say so, so it isn't misdiagnosed |
| A Dependabot PR could never merge: Dependabot runs get no repository secrets, so the required `SonarCloud Code Analysis` check never reported | first Deps PR | A secret that a required check needs must also be a **Dependabot secret**; added to the owner steps of `tessaro-bootstrap` and to `sdd` (plan phase) |
| Two `main` runs failed with "Internal server error" and zero jobs during a GitHub Actions outage; the first diagnosis blamed SonarCloud | after T02 | `sdd-task`: when a run has no jobs or an internal error, check githubstatus.com before diagnosing |
| `dotnet format` caught a naming rule (`_` prefix) that the build did not | T02 | Already in `sdd-task` step 7 (format before every push); kept |
| macOS `sed` has no `\b`, so a rename silently did nothing | T02 | `sdd-task`: use `perl -pi` for word-boundary edits, and re-check the result |
| Cleaning up branches deleted a branch that belonged to another session's worktree (no commits lost; restored) | after T03 | `sdd-task` step 10: delete only branches this task created |
| The reference checkout was pulled without asking first, although the skill said to ask | plan phase | `tessaro-bootstrap` step 1 now says the pull is the owner's call even when it is a fast-forward |
| Issues were created but not added to a GitHub Project board; the owner manages work from the board | after T00 | `sdd` and `sdd-task` now add every issue to the repo's board and keep its status in sync (applied mid-spec) |
| A squash merge done from another session used a Conventional Commits subject instead of the PR title | tessaro-optional#36 | Already in `sdd-task` step 10; merges in this spec all used the PR title |

These changes were applied to the `sdd`, `sdd-task` and `tessaro-bootstrap` skills.

## Follow-ups

- **Async composition** (`Map`/`Bind`/`Match` over `Task<Result<T>>`) — candidate spec 002.
- **Release to nuget.org** — candidate spec 003, reusing the copied `release.yml`.
- **ASP.NET Core adapter** (`Tessaro.Results.AspNetCore`): status mapping, `ProblemDetails`, error metadata.
- tessaro-optional: 27 `IDE0007`/`IDE0008` style findings in SonarCloud, and 3 `shell:S1192` findings to mark as accepted (they are its AC tags); its `net8.0` target leaves support on 2026-11-10.
