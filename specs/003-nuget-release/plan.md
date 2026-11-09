# Plan 003 — NuGet release

**Status:** Approved (2026-11-09) · **Created:** 2026-11-09
**Spec:** [003 — NuGet release](spec.md) (Approved 2026-11-09) · **Constitution:** [v1.0.0](../../docs/constitution.md)
**Reference:** [tessaro-optional plan 002](https://github.com/igortessaro/tessaro-optional/blob/main/specs/002-nuget-release/plan.md)

> This plan defines **how** spec 003 is delivered. The ordered steps belong in `tasks.md`.

---

## 1. Technical context

No new tool, action or package. The pipeline copied from tessaro-optional by `tessaro-bootstrap`
is used as is (reviewed 2026-11-09; the bootstrap already renamed project, package ID and URLs):

| Piece | File | Role |
|---|---|---|
| Release workflow | `.github/workflows/release.yml` | `guard` → `ci` (reusable `ci.yml`, all PR gates) → `pack` (package validation, version and dependency check) → `publish` (environment `release`, attestation, NuGet OIDC login, push, GitHub Release) |
| Release guard | `.github/scripts/release-info.sh` + `tests/scripts/release-info.test.sh` | Tag format, commit on `main`, prerelease flag, breaking-change baseline |
| Tag ruleset | `.github/rulesets/release-tags.json` | Only admins create, update or delete `v*` tags |
| PR title check | `.github/workflows/pr-title.yml` + `check-pr-title.sh` + tests | ADR 0005 (US-5, already active) |
| Routine | `CONTRIBUTING.md` → *Releasing* | Choose version, preparation PR, tag, approve, verify, failure table |

## 2. What changes in the repository

| File | Change | AC |
|---|---|---|
| `CONTRIBUTING.md` | "One-time setup (**already done**, kept for reference)" → a checklist that is true here (done in T02, with dates); version examples use `0.1.0` | AC-4.2 |
| `README.md` | Remove "Not on nuget.org yet"; install command that works for the first release (`dotnet add package Tessaro.Results --prerelease` while only release candidates exist, plain `dotnet add package Tessaro.Results` once `0.1.0` is out); NuGet badge | AC-3.6 |
| `src/Tessaro.Results/PublicAPI.*.txt` | Move every entry of `Unshipped` to `Shipped` (preparation PR, before the first tag) | AC-4.1 |
| `specs/003-nuget-release/verification.md` | Evidence per AC, the reference's format (how + evidence); read by the traceability check | all |

No change to `release.yml` or the scripts is planned; if a real run shows one is needed, it is a
task-level fix with a dated amendment here.

## 3. Behaviour details not stated in the spec

| Case | Behaviour | Reason |
|---|---|---|
| First release `v0.1.0-rc.1` | Guard output `prerelease=true`, `baseline=` (none): package validation runs without a baseline | No earlier version; AC-3.5 applies from `0.1.0` on (spec resolved question 5). Spiked |
| `v0.1.0` after `v0.1.0-rc.1` | May be tagged on the same commit; its baseline is the rc (release guard rule, tested) | Reference behaviour; `0.1.0` must not break the rc's API |
| The README on nuget.org | Packed from the tagged commit, so the README fix must merge **before** `v0.1.0-rc.1` is tagged | AC-3.6 |
| Install command in the README for the rc | Says `--prerelease` while only release candidates exist; updated in the closing task after `0.1.0` | AC-3.6 "works as written" |
| SonarCloud in the release run | Skipped (`ci.yml` called without secrets); the same commit already passed it on `main` | Reference design; no secret leaves the PR/branch context |

## 4. Verification strategy

Same as the reference: `verification.md` lists every AC with *how* it was shown — script tests,
workflow structure, a real run, a command, or the nuget.org page — and the AC traceability check
runs on it with `tests/scripts/` (`--marker '^\| AC-'`, as in the reference).

| AC | Planned evidence |
|---|---|
| AC-1.1, 1.2, 2.1–2.3 | The `v0.1.0-rc.1` run (guard output, push log, GitHub Release), then a fresh consumer adds the package from nuget.org |
| AC-1.3 | `release-info.test.sh` + a deliberate tag on a branch commit (`v0.0.0-guard.1`), deleted afterwards |
| AC-1.4 | Workflow structure (`publish` needs `pack` needs `guard` + `ci`) + the guard run of AC-1.3 showing later jobs skipped |
| AC-1.5 | Re-run the `publish` job of the rc run after it published → push fails "already exists" |
| AC-1.6 | The rc run waiting on `release` before approval: package, page and Release all "not found" |
| AC-1.7 | Ruleset `release-tags` (only admin bypass) + the owner's push reporting a bypassed rule |
| AC-3.1, 3.6 | nuget.org page of the rc |
| AC-3.2 | `.snupkg` PDB source mapping points to the tagged commit (spiked locally: `commit=` in the nuspec matches) |
| AC-3.3 | `pack`'s dependency check + the released nuspec |
| AC-3.4 | `gh attestation verify <nupkg> --repo igortessaro/tessaro-results` |
| AC-3.5 | `release-info.test.sh` + the local spike below, repeated against the real `0.1.0` in the closing task |
| AC-4.1 | The preparation PR |
| AC-4.2 | `CONTRIBUTING.md` *Releasing* after T01/T02 |
| AC-5.1, 5.2 | `check-pr-title.test.sh` in CI; `pr-title` passed on every PR since #7 |
| AC-5.3 | Dependabot PR [#9](https://github.com/igortessaro/tessaro-results/pull/9): `Deps \| Bump xunit.v3 from 4.0.2 to 4.0.3`, `pr-title` passed |

## 5. One-time setup (owner only — Claude never handles credentials)

| # | Step | Command / place |
|---|---|---|
| 1 | Check the `Tessaro` prefix reservation; request it if missing | nuget.org → *Account* → *Prefix reservation* (spec resolved question 3) |
| 2 | Trusted Publishing policy | nuget.org → *Trusted Publishing*: package `Tessaro.Results` ("push new packages and versions"), repository `igortessaro/tessaro-results`, workflow `release.yml`, environment `release` |
| 3 | `NUGET_USER` secret (nuget.org profile name) | `gh secret set NUGET_USER --repo igortessaro/tessaro-results` |
| 4 | `release` environment, owner as reviewer (user id `14225541`, checked 2026-11-09), only `v*` tags deploy | the two `gh api` commands in `CONTRIBUTING.md` — run by Claude in T02 after the owner confirms (resolved question 1) |
| 5 | Tag ruleset | `gh api -X POST repos/igortessaro/tessaro-results/rulesets --input .github/rulesets/release-tags.json` — run by Claude in T02, same |
| 6 | Push tags and approve runs | `git tag … && git push origin …`; *Actions* → run → *Review deployments* |

## 6. Risks

| ID | Risk | Mitigation |
|---|---|---|
| R1 | The copied guard or pack behaves differently for this repository | **Spiked 2026-11-09** on a clone at `55289bd`: `v0.1.0-rc.1` → `version=0.1.0-rc.1 prerelease=true baseline=`; a branch commit → "the tagged commit is not on main"; pack with the workflow's arguments → `Tessaro.Results.0.1.0-rc.1.nupkg` + `.snupkg` (PDB), MIT, README, URLs, `repository commit` = the tagged commit, no dependencies |
| R2 | The breaking-change check doesn't catch a removed member | **Spiked** with a local feed holding `0.1.0`: removing `Result.Ok()` and tagging `v0.1.1` → baseline `0.1.0`, pack fails **CP0002**; `v0.2.0` → no baseline, packs (SemVer `0.y` rule) |
| R3 | Trusted Publishing fails on the first push (policy typo, wrong environment) | The policy fields are listed in §5 exactly as the workflow uses them; failure is safe (nothing published) and the CONTRIBUTING failure table says how to retry |
| R4 | The README published with the rc still says "not on nuget.org yet" | The README change lands in T01, before any tag (§3) |
| R5 | The `Tessaro` prefix isn't reserved, so someone could squat `Tessaro.*` IDs | Owner step 1, before the first push |

## 7. Constitution check

| Principle / gate | How this plan complies |
|---|---|
| I. Simplicity | Reuses the proven pipeline; no new tool or workflow |
| II. Fail fast | Guard rejects bad tags and off-`main` commits before any build; duplicate versions fail; breaking changes fail before publishing |
| VI. Zero runtime dependencies | `pack` fails on any `<dependency>` |
| Gates | The release run reruns every PR gate (`ci.yml`) on the tagged commit (NFR-3) |
| Governance | Tags owner-only (ruleset), approval in the `release` environment, no stored NuGet key, Claude never handles credentials |

## 8. Deferred

- A reusable release workflow for all `Tessaro.*` repositories — candidate spec 005 (spec §6).
- Publishing `0.1.0` waits until the rc is verified (spec resolved question 1).

## 9. Resolved questions (2026-11-09, owner approved the recommendations)

1. **Claude runs §5 steps 4–5** (the `release` environment and the tag ruleset, via `gh api` with
   the owner's login) in T02, after the owner confirms at that moment; steps 1–3 and 6 stay the owner's.
2. **AC-1.3 is exercised** with tag `v0.0.0-guard.1` on a throwaway branch commit, deleted after the run.

## 10. Review checklist

- [x] Every acceptance criterion maps to a change or a planned evidence
- [x] Nothing contradicts the constitution or an accepted ADR
- [x] Tooling choices are justified and minimal
- [x] Owner-only steps (credentials, sign-ins) are clearly separated
- [x] Riskiest assumptions were spiked or are explicitly open
- [x] Open questions answered or deferred
