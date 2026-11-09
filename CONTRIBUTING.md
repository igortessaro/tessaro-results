# Contributing

## How changes are made

This repository uses Spec-Driven Development: every change starts from an approved spec, plan and
task list under [`specs/`](specs/), checked against the [constitution](docs/constitution.md) and the
[architecture decisions](docs/adr/).

- **Branches:** trunk-based, short-lived branches off `main` with Gitflow prefixes:
  `feature/NNN-slug`, `hotfix/slug`, `release/x.y.z`.
- **Pull requests:** title `<Context> | <Imperative summary>`, e.g.
  `Spec 012 (T03) | Document the public API`. The description follows the PR template:
  Summary, Changes, Testing, Notes for reviewer, Checklist, then `Closes #N`
  ([ADR 0005](docs/adr/0005-pull-request-title-and-description.md)). The `pr-title` check enforces
  the title.
- **Merging:** squash only, once every required check is green: `format`, `build-test`,
  `SonarCloud Code Analysis`, `scripts`, `pr-title`. The squash commit uses the PR title.

The local commands are in [`CLAUDE.md`](CLAUDE.md#commands).

## Releasing

A release is one tag pushed by the owner. [`release.yml`](.github/workflows/release.yml) then:
1. validates the tag;
2. runs every quality gate;
3. packs with an API compatibility check;
4. after the owner approves, publishes to nuget.org and creates the GitHub Release
   (see the release spec under [`specs/`](specs/)).

Every service involved is free for this public repository.

### 1. Choose the version

Versions follow [SemVer 2.0](https://semver.org/), and the tag is the only source of the version
(MinVer).

| Change since the last release | Before `1.0.0` | From `1.0.0` on |
|---|---|---|
| Fixes only, no public API change | patch: `0.1.0` → `0.1.1` | patch: `1.2.0` → `1.2.1` |
| New public members | minor or patch | minor: `1.2.0` → `1.3.0` |
| Removed or changed public members (breaking) | **minor**: `0.1.1` → `0.2.0` | **major**: `1.2.0` → `2.0.0` |

- Prereleases add `-rc.N`, e.g. `v0.0.1-rc.1`. A final version may be tagged on the same commit as its rc.
- The release fails if the public API breaks without the bump this table requires. The workflow
  compares against the previous version tag.

### 2. Prepare the release (pull request)

1. Move every entry of `src/Tessaro.Results/PublicAPI.Unshipped.txt` to
   `PublicAPI.Shipped.txt`, keeping it sorted.
2. Leave only `#nullable enable` in `PublicAPI.Unshipped.txt`.
3. Merge the PR, e.g. `Chore | Prepare release v0.1.0`.

A prerelease is public on nuget.org, so its API counts as shipped too.

### 3. Tag and push

Only repository admins can create, update or delete `v*` tags (ruleset `release-tags`).

```bash
git switch main && git pull
git tag v0.1.0
git push origin v0.1.0
```

### 4. Approve the run

The `publish` job waits in the `release` environment. Open the run with:

```bash
gh run list --workflow release.yml --limit 1
```

On that run, choose **Review deployments → release → Approve and deploy**. A run that is left
unapproved publishes nothing.

### 5. Verify

nuget.org takes a few minutes to index a new version.

```bash
gh release view v0.1.0                       # notes, prerelease flag, attached .nupkg/.snupkg
gh run download <run-id> --name package --dir pkg   # the release run's id, from gh run list
gh attestation verify pkg/Tessaro.Results.0.1.0.nupkg --repo igortessaro/tessaro-results
```

Then open `https://www.nuget.org/packages/Tessaro.Results/0.1.0`. It should show the README, the
MIT licence and the repository link.

### When a release fails

| Where it failed | What to do |
|---|---|
| `guard`, `ci` or `pack` | Fix the cause on `main` through a PR, delete the tag (`git push origin :refs/tags/v0.1.0 && git tag -d v0.1.0`), then tag again |
| `publish`, before the push | Re-run the failed job from the run page |
| Push failed with "already exists" | That version is already on nuget.org and can never be overwritten. Release the next patch instead |
| GitHub Release step only | The package is published. Create the release by hand: `gh release create v0.1.0 pkg/* --verify-tag --generate-notes` (add `--prerelease --latest=false` for an rc) |
| A published version is bad | Unlist it on nuget.org (*Manage package → Listing*) and release a fixed patch. Versions cannot be deleted |

### One-time setup

Done once per repository ([spec 003](specs/003-nuget-release/plan.md) §5). Steps marked *owner* need
the owner's accounts or credentials; Claude never handles them.

| # | Step | Who | Done |
|---|---|---|---|
| 1 | nuget.org: the `Tessaro` ID prefix is reserved to the owner's account (*Account → Prefix reservation*) | owner | |
| 2 | nuget.org: a *Trusted Publishing* policy — package `Tessaro.Results`, scope "Push new packages and package versions", repository `igortessaro/tessaro-results`, workflow `release.yml`, environment `release` | owner | |
| 3 | Secret `NUGET_USER` with the nuget.org profile name: `gh secret set NUGET_USER --repo igortessaro/tessaro-results` | owner | |
| 4 | `release` environment: the owner is the required reviewer, and only `v*` tags may deploy (commands below) | Claude, after the owner confirms | |
| 5 | Tag ruleset `release-tags`: only admins create, update or delete `v*` tags (command below) | Claude, after the owner confirms | |

**Step 4 — `release` environment** (reviewer: user id `14225541`, `igortessaro`):

```bash
gh api -X PUT repos/igortessaro/tessaro-results/environments/release --input - <<'EOF'
{"wait_timer":0,"prevent_self_review":false,"reviewers":[{"type":"User","id":14225541}],
 "deployment_branch_policy":{"protected_branches":false,"custom_branch_policies":true}}
EOF
gh api -X POST repos/igortessaro/tessaro-results/environments/release/deployment-branch-policies \
  -f name='v*' -f type=tag
```

**Step 5 — tag ruleset:**

```bash
gh api -X POST repos/igortessaro/tessaro-results/rulesets --input .github/rulesets/release-tags.json
```
