# Spec 003 — NuGet release

**Status:** Implemented (2026-11-10) · **Approved:** 2026-11-09 · **Created:** 2026-11-09
**Constitution:** [v1.0.0](../../docs/constitution.md) · **ADRs:** [0005](../../docs/adr/0005-pull-request-title-and-description.md)
**Reference:** [tessaro-optional spec 002 — NuGet release](https://github.com/igortessaro/tessaro-optional/blob/main/specs/002-nuget-release/spec.md) (Implemented 2026-11-08)

> This spec defines **what** a release offers and **why**. Workflows, setup commands and tooling
> belong in `plan.md`.

---

## 1. Problem

Specs 001 and 002 are implemented, but no service can use `Tessaro.Results`: it exists only as a
local `dotnet pack` output, and the FIAP-SOAT-Net services that should replace `Response.cs` with
it consume packages from nuget.org.

The release pipeline itself is not the problem. tessaro-optional designed and proved it in its
spec 002, and `tessaro-bootstrap` copied it here (`release.yml`, the release guard and its tests,
the tag ruleset, the release routine in `CONTRIBUTING.md`). What is missing is everything that is
specific to **this** repository and package: the one-time setup, a first real release, and the
evidence that the copied pipeline works here.

## 2. Goal

Publishing a version of `Tessaro.Results` is one deliberate act by the owner — pushing a version
tag and approving the run — after which the package CI built, tested and verified appears on
nuget.org, with a matching GitHub Release. Free tiers only, as in the reference.

**Success looks like:**

```bash
git tag v0.1.0-rc.1 && git push origin v0.1.0-rc.1   # owner, on a commit of main
# … owner approves the "release" run …
dotnet add package Tessaro.Results --prerelease       # any service, minutes later
```

## 3. Users

- **Owner (Igor)** — wants to publish safely, with one command, no secrets to rotate, and the
  same routine as tessaro-optional.
- **Service developer** (FIAP-SOAT-Net) — wants to add the package, read its docs on nuget.org,
  debug into it, and trust its versions.

---

## 4. User stories and acceptance criteria

Notation: *tag* = a git tag named `v<SemVer>`, e.g. `v1.2.3` or `v1.2.3-rc.1`.
The stories and IDs mirror the reference spec, so the two packages are verified the same way.
ACs that need a real run are verified in `verification.md` (as in the reference); some negative
paths are exercised deliberately.

### US-1 — Publish a version from a tag

*As the owner, I want pushing a tag to publish exactly that version, so releases are one deliberate act.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-1.1 | a commit on `main` | the owner pushes tag `vX.Y.Z` and approves the run | `Tessaro.Results` `X.Y.Z` and its symbols package are published to nuget.org |
| AC-1.2 | a commit on `main` | the owner pushes tag `vX.Y.Z-label.N` | the version is published as a **prerelease** |
| AC-1.3 | a commit that is not on `main` | a tag is pushed for it | nothing is published; the run fails saying the commit is not on `main` |
| AC-1.4 | any quality gate fails (build, format, tests, coverage) | the release runs | nothing is published |
| AC-1.5 | the version already exists on nuget.org | the release runs | the run fails; the existing version is untouched |
| AC-1.6 | a release run is waiting | nobody approves it | nothing is published |
| AC-1.7 | someone other than the owner | creates or pushes a `v*` tag | GitHub rejects the tag |

### US-2 — A release record on GitHub

| ID | Given | When | Then |
|---|---|---|---|
| AC-2.1 | a successful publish of `vX.Y.Z` | — | a GitHub Release `vX.Y.Z` exists with notes generated from the merged PRs since the previous tag |
| AC-2.2 | a successful publish | — | the `.nupkg` and `.snupkg` files are attached to the GitHub Release |
| AC-2.3 | a prerelease tag | — | the GitHub Release is marked as a prerelease and not as "latest" |

### US-3 — A trustworthy package for consumers

| ID | Given | When | Then |
|---|---|---|---|
| AC-3.1 | the published package | viewed on nuget.org | it shows the README, the MIT licence, the project and repository URLs |
| AC-3.2 | the published package | a consumer debugs into it | the debugger steps into the exact source of that commit (SourceLink + symbols) |
| AC-3.3 | the published package | its dependencies are inspected | it has none (spec 001 NFR-1) |
| AC-3.4 | the published `.nupkg` | its provenance is checked (`gh attestation verify`) | it is attested as built by this repository's release workflow from the tagged commit |
| AC-3.5 | a version after the first | it removes or changes a public member without the required version bump (major from `1.0.0` on, at least minor before it) | the release fails before publishing (breaking-change check against the previous version) |
| AC-3.6 | the README published with the package | a consumer reads it on nuget.org | it no longer says the package is "not on nuget.org yet", and its install command works as written |

### US-4 — Prepare a release

*As the owner, I want the written release routine to be true for this repository.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-4.1 | members listed in `PublicAPI.Unshipped.txt` | a release is prepared | they are moved to `PublicAPI.Shipped.txt` in a PR merged before tagging |
| AC-4.2 | `CONTRIBUTING.md` | the owner follows its release section | every step and command applies to this repository: preparation PR, tag, approval, verification; the one-time setup it lists has actually been done here |

### US-5 — Readable release notes

Already satisfied by the bootstrapped PR title check (ADR 0005); kept so the evidence is recorded.

| ID | Given | When | Then |
|---|---|---|---|
| AC-5.1 | a PR whose title matches `<Context> \| <Imperative summary>` | it is opened or its title edited | the title check passes |
| AC-5.2 | a PR whose title does not match | it is opened or its title edited | the title check fails with the expected format and an example, and the PR cannot be merged |
| AC-5.3 | a Dependabot PR | it is opened | its title starts with `Deps \| ` and passes the check |

---

## 5. Decisions and constraints

Inherited from the reference spec (§5 there) and unchanged:

| Topic | Decision |
|---|---|
| Cost | Free tiers only: GitHub Actions, environments, Releases and attestations on a public repository; nuget.org |
| Credentials | No long-lived NuGet API key: nuget.org Trusted Publishing gives the run a short-lived key. Claude never handles credentials |
| Version source | The tag is the only source of the version (MinVer) |
| Branching | Tags on `main`; no `release/*` branches |
| Code signing | No author signing; nuget.org's repository signature plus provenance attestation (AC-3.4) |

Specific to this repository:

| Topic | Decision | Why |
|---|---|---|
| Pipeline | Reuse the copied `release.yml`, guard script and tag ruleset; change them only where this repository differs | They are proven in tessaro-optional (its `verification.md`); one implementation for both packages |
| Evidence already available | US-5: AC-5.1/5.2 are covered by `tests/scripts/check-pr-title.test.sh` in CI; AC-5.3 was observed on Dependabot PR [#9](https://github.com/igortessaro/tessaro-results/pull/9) (`Deps \| Bump xunit.v3 from 4.0.2 to 4.0.3`, `pr-title` passed) | Recorded in `verification.md`, not re-built |

## 6. Out of scope (future specs)

- A reusable release workflow shared by all `Tessaro.*` repositories — the global `CLAUDE.md`
  suggests deciding this "with a second package"; now that there are two, it is candidate spec 005,
  after this first release proves the copy works.
- A hand-written `CHANGELOG.md`; automated version bumps; other feeds (as in the reference).
- Migrating a FIAP-SOAT-Net service to the package (that happens in those repositories).

## 7. Non-functional requirements

- **NFR-1** Zero cost (§5).
- **NFR-2** The release workflow never runs on pull requests and cannot publish from a branch.
- **NFR-3** The published package is built by the release run itself, from the tagged commit,
  after all quality gates pass in that same run.
- **NFR-4** The workflow files keep the constitution's gates (actions pinned by SHA,
  least-privilege permissions).

## 8. Resolved questions (2026-11-09, owner approved the recommendations)

1. **First version: `0.1.0-rc.1`, then `0.1.0`** once the rc is verified; `1.0.0` waits for a service
   to run the package in production.
2. **Approval step:** yes — the `release` environment with the owner as required reviewer (AC-1.6).
3. **Package ID prefix:** the owner checks on nuget.org before the first push whether `Tessaro` is
   reserved to their account, and requests the reservation if not.
4. **Re-running a release of a published version fails** (AC-1.5).
5. **Breaking-change check** from the first release; it applies from `0.1.0` on (AC-3.5).
6. **Negative paths on the real pipeline:** AC-1.3 (tag off `main`) and AC-1.5 (duplicate version)
   exercised deliberately; AC-1.6 by leaving one run unapproved until it is cancelled.

## 9. Review checklist

- [x] Every acceptance criterion is testable as written (or has a real-run verification)
- [x] Nothing in this spec prescribes implementation details
- [x] Every service used is free for this repository
- [x] Owner-only steps are identifiable (tag push, approval, nuget.org policy, secret, environment, rulesets)
- [x] Out-of-scope items are acceptable
- [x] Open questions answered or deferred
