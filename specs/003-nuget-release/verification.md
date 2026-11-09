# Verification 003 — NuGet release

**Started:** 2026-11-09 · **First release:** `0.1.0-rc.1`, published by [run 42711651307](https://github.com/igortessaro/tessaro-results/actions/runs/42711651307) (attempt 3) from commit `48dc126` · **Spec:** [003](spec.md) · **Plan:** [003 §4](plan.md#4-verification-strategy)
**Reference:** [tessaro-optional verification 002](https://github.com/igortessaro/tessaro-optional/blob/main/specs/002-nuget-release/verification.md)

Evidence for every acceptance criterion. Logic covered by script tests is listed with the test file;
behaviour that only a real run can show is listed with the run, command or page that shows it.
Rows marked **Pending** are not shown yet; the closing task (T04) leaves none. The AC traceability
check reads this table:
`bash ~/.claude/skills/sdd/scripts/check-ac-traceability.sh --marker '^\| AC-' specs/003-nuget-release/spec.md specs/003-nuget-release/verification.md`

| AC | How | Evidence |
|---|---|---|
| AC-1.1 | Real run | [run 42711651307](https://github.com/igortessaro/tessaro-results/actions/runs/42711651307) attempt 3, from tag `v0.1.0-rc.1` on `48dc126`: `publish` pushed `Tessaro.Results.0.1.0-rc.1.nupkg` ("Your package was pushed.", 22:35:05 UTC) and the `.snupkg` to the symbol server ("Created"). Attempts 1–2 failed at `NuGet login` with HTTP 401 ("No matching trust policy") because the nuget.org policy had *Repository Owner* `igor.tessaro` (the nuget.org user) instead of the GitHub owner `igortessaro`; nothing was published by them, and re-running the failed job after the fix was enough (CONTRIBUTING failure table) |
| AC-1.2 | Script tests + real run | `release-info.test.sh` (prerelease cases); the run's `guard` output: `version=0.1.0-rc.1`, `prerelease=true`, `baseline=` |
| AC-1.3 | Script tests + real run | **Pending** — `release-info.test.sh` (not-on-`main` cases) and a `v0.0.0-guard.1` run on a branch commit (T04) |
| AC-1.4 | Workflow structure + real run | **Pending** — `publish` needs `pack`, which needs `guard` and `ci`; the guard run of AC-1.3 (T04) |
| AC-1.5 | Real run | **Pending** — re-run of the `v0.1.0` `publish` job (T04) |
| AC-1.6 | Real run | At 22:26:29 UTC the run was `waiting`, `publish` pending on environment `release` (reviewer `igortessaro`), every earlier job green; nuget.org index and package page returned 404 and the GitHub Release did not exist. Publishing happened only after the owner approved (attempt 3) |
| AC-1.7 | Ruleset + rule-suite record | Ruleset `release-tags` (id 26897681): creation, update and deletion of `refs/tags/v*` restricted, bypass only for the admin role. Rule suite 5007294291: the push of `refs/tags/v0.1.0-rc.1` by `igortessaro` → `release-tags / creation`: "Cannot create ref due to creations being restricted", suite result **bypass** |
| AC-2.1 | Real run | [GitHub Release v0.1.0-rc.1](https://github.com/igortessaro/tessaro-results/releases/tag/v0.1.0-rc.1) with notes generated from PRs #7–#29 ("What's Changed") |
| AC-2.2 | Real run | Release assets: `Tessaro.Results.0.1.0-rc.1.nupkg`, `Tessaro.Results.0.1.0-rc.1.snupkg` |
| AC-2.3 | Real run | `isPrerelease=true`, not a draft; `GET /repos/igortessaro/tessaro-results/releases/latest` returns 404 (no release is marked latest) |
| AC-3.1 | nuget.org page | [Tessaro.Results 0.1.0-rc.1](https://www.nuget.org/packages/Tessaro.Results/0.1.0-rc.1) (200 from 22:40 UTC) renders the README — summary, usage, async section, `Response.cs` migration checklist — the MIT licence, the project and source repository links, the prerelease mark and the `net10.0` target |
| AC-3.2 | Symbols package | The `.snupkg` PDB maps sources to `https://raw.githubusercontent.com/igortessaro/tessaro-results/48dc12654399d7746a8e93249cb93d87bb3d4bb4/*` (the tagged commit); the nuspec's `repository commit` is the same; the `.snupkg` was pushed to nuget.org's symbol server |
| AC-3.3 | Workflow check + package | `pack` fails on any `<dependency>` (`release.yml`); the released nuspec has one empty group, `net10.0`, and no dependency |
| AC-3.4 | `gh attestation verify` | Exit 0 for the released `.nupkg` (sha256 `597f2bec8bbadb3a…`): signer `.github/workflows/release.yml@refs/tags/v0.1.0-rc.1`, source digest `48dc126…`, trigger `push`. Three identical attestations exist because the attestation step ran in each of the three attempts |
| AC-3.5 | Script tests + local pack | **Pending** — `release-info.test.sh` (baseline cases) and the CP0002 check against the real `0.1.0` (T04) |
| AC-3.6 | nuget.org page + consumer | The README on the rc's page has no "Not on nuget.org yet" and shows `dotnet add package Tessaro.Results --prerelease`. A fresh consumer (`net10.0`, empty package cache, nuget.org as the only source) ran that command at 22:42:14 UTC → `PackageReference` `Tessaro.Results` `0.1.0-rc.1`; a program using `Result<int>` and `MapAsync` on a pending result printed `Success(value 42)`. (At 22:40 the page was live but the package index was not yet, so the same command first answered "no versions available": nuget.org indexes the page before the package index) |
| AC-4.1 | Pull request | [#29](https://github.com/igortessaro/tessaro-results/pull/29) moved all 81 entries to `PublicAPI.Shipped.txt` before `v0.1.0-rc.1` was tagged on its merge commit `48dc126` |
| AC-4.2 | Documentation | [CONTRIBUTING.md — Releasing](../../CONTRIBUTING.md#releasing): the routine, plus a *One-time setup* checklist written for this repository in T01 (replacing tessaro-optional's "already done" note); its *Done* column is filled in T02 |
| AC-5.1 | Script tests + real PRs | `tests/scripts/check-pr-title.test.sh` (19 cases, run by the `scripts` CI job); `pr-title` passed on every PR from [#8](https://github.com/igortessaro/tessaro-results/pull/8) to [#27](https://github.com/igortessaro/tessaro-results/pull/27) (checked 2026-11-09; #7 predates the workflows) |
| AC-5.2 | Script tests + ruleset | `check-pr-title.test.sh` — the failing cases (Conventional Commit title, lower-case summary, trailing period, trailing space, 73 characters, malformed spec/task, unknown context) and "a failure shows the format and an example"; ruleset `main` (id 26873631) requires `pr-title`, so a failing title blocks the merge |
| AC-5.3 | Real PR | Dependabot PR [#9](https://github.com/igortessaro/tessaro-results/pull/9): `Deps \| Bump xunit.v3 from 4.0.2 to 4.0.3`, `pr-title` passed (`dependabot.yml` prefix `Deps \|`) |
