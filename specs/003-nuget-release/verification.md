# Verification 003 — NuGet release

**Started:** 2026-11-09 · **Spec:** [003](spec.md) · **Plan:** [003 §4](plan.md#4-verification-strategy)
**Reference:** [tessaro-optional verification 002](https://github.com/igortessaro/tessaro-optional/blob/main/specs/002-nuget-release/verification.md)

Evidence for every acceptance criterion. Logic covered by script tests is listed with the test file;
behaviour that only a real run can show is listed with the run, command or page that shows it.
Rows marked **Pending** are not shown yet; the closing task (T04) leaves none. The AC traceability
check reads this table:
`bash ~/.claude/skills/sdd/scripts/check-ac-traceability.sh --marker '^\| AC-' specs/003-nuget-release/spec.md specs/003-nuget-release/verification.md`

| AC | How | Evidence |
|---|---|---|
| AC-1.1 | Real run | **Pending** — `v0.1.0-rc.1` run (T02) |
| AC-1.2 | Script tests + real run | **Pending** — `release-info.test.sh` (prerelease cases) and the rc run's guard output (T02) |
| AC-1.3 | Script tests + real run | **Pending** — `release-info.test.sh` (not-on-`main` cases) and a `v0.0.0-guard.1` run on a branch commit (T04) |
| AC-1.4 | Workflow structure + real run | **Pending** — `publish` needs `pack`, which needs `guard` and `ci`; the guard run of AC-1.3 (T04) |
| AC-1.5 | Real run | **Pending** — re-run of the `v0.1.0` `publish` job (T04) |
| AC-1.6 | Real run | **Pending** — the rc run waiting on the `release` environment before approval (T02) |
| AC-1.7 | Ruleset + real push | **Pending** — ruleset `release-tags` and the owner's tag push (T02) |
| AC-2.1 | Real run | **Pending** — GitHub Release `v0.1.0-rc.1` with generated notes (T02) |
| AC-2.2 | Real run | **Pending** — release assets (T02) |
| AC-2.3 | Real run | **Pending** — prerelease flag, not latest (T02) |
| AC-3.1 | nuget.org page | **Pending** — package page of `0.1.0-rc.1` (T02) |
| AC-3.2 | Symbols package | **Pending** — `.snupkg` source mapping to the tagged commit (T02) |
| AC-3.3 | Workflow check + package | **Pending** — `pack`'s dependency check and the released nuspec (T02) |
| AC-3.4 | `gh attestation verify` | **Pending** — attestation of the released `.nupkg` (T02) |
| AC-3.5 | Script tests + local pack | **Pending** — `release-info.test.sh` (baseline cases) and the CP0002 check against the real `0.1.0` (T04) |
| AC-3.6 | nuget.org page + consumer | **Pending** — the README on the rc's page and its install command run by a fresh consumer (T02) |
| AC-4.1 | Pull request | **Pending** — the preparation PR moving `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt` (T02) |
| AC-4.2 | Documentation | [CONTRIBUTING.md — Releasing](../../CONTRIBUTING.md#releasing): the routine, plus a *One-time setup* checklist written for this repository in T01 (replacing tessaro-optional's "already done" note); its *Done* column is filled in T02 |
| AC-5.1 | Script tests + real PRs | `tests/scripts/check-pr-title.test.sh` (19 cases, run by the `scripts` CI job); `pr-title` passed on every PR from [#8](https://github.com/igortessaro/tessaro-results/pull/8) to [#27](https://github.com/igortessaro/tessaro-results/pull/27) (checked 2026-11-09; #7 predates the workflows) |
| AC-5.2 | Script tests + ruleset | `check-pr-title.test.sh` — the failing cases (Conventional Commit title, lower-case summary, trailing period, trailing space, 73 characters, malformed spec/task, unknown context) and "a failure shows the format and an example"; ruleset `main` (id 26873631) requires `pr-title`, so a failing title blocks the merge |
| AC-5.3 | Real PR | Dependabot PR [#9](https://github.com/igortessaro/tessaro-results/pull/9): `Deps \| Bump xunit.v3 from 4.0.2 to 4.0.3`, `pr-title` passed (`dependabot.yml` prefix `Deps \|`) |
