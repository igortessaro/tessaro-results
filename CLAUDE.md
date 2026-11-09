# CLAUDE.md — Tessaro.Results

A zero-dependency, transport-agnostic .NET result type (`Result`, `Result<T>`, `Error`) that
replaces the `Response.cs` classes copied across the FIAP-SOAT-Net services.

## Read first

- `docs/constitution.md` — non-negotiable principles and quality gates. Check every change against it.
- `docs/adr/` — architecture decisions. Don't contradict an accepted ADR; propose a new one instead.
- `specs/NNN-*/` — the active spec, plan and tasks.
- Reference repo for tooling, CI, release and lessons learned:
  `~/Documents/repos/personal/tessaro-optional` (its `specs/*/retrospective.md` in particular).

## How we work (Spec-Driven Development)

1. `spec.md` (what/why) → 2. `plan.md` (how) → 3. `tasks.md` (ordered steps) → 4. implement.
- Use the `sdd` skill to plan and the `sdd-task` skill to execute a task.
- Don't start a phase until the owner (Igor) approved the previous one.
- Implement test-first, one task per branch; open a PR. **Never push to `main`.**
- Branching: **trunk-based** — short-lived branches off `main`, no `develop`. Branch names use the
  Gitflow prefixes: `feature/NNN-slug` for all planned work, `hotfix/slug`, `release/x.y.z`.
- Don't add public API that no approved spec requires (YAGNI).
- PR titles: `<Context> | <Imperative summary>` — `Spec NNN (TNN)`, `Spec NNN`, `Hotfix`, `Deps` or `Chore`;
  description sections Summary, Changes, Testing, Notes for reviewer, Checklist, then `Closes #N`
  (ADR 0005). Squash subject on `main` = PR title + ` (#PR)`.
- Branch commits: Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `ci:`, `chore:`).
- No `Co-Authored-By` trailers and no AI attribution footers, anywhere.

## Code style

- Follow the constitution: KISS, fail fast (validate every argument eagerly), SOLID, DRY.
- Name design patterns explicitly when you use one, and only when they simplify.
- XML docs on every public member.

## Commands

Tests run on Microsoft.Testing.Platform (opted in via `global.json`), so `dotnet test` takes
`--solution`/`--project` and runner options directly (no `--` separator).

```bash
dotnet build Tessaro.Results.slnx -c Release                        # build (warnings are errors)
dotnet test --solution Tessaro.Results.slnx -c Release              # run tests (net10.0)
dotnet test --solution Tessaro.Results.slnx -c Release \
  --coverlet --coverlet-output-format opencover \
  --coverlet-threshold 80 --coverlet-threshold-type line --coverlet-threshold-type branch   # coverage gate
dotnet format Tessaro.Results.slnx --verify-no-changes              # style gate
dotnet pack src/Tessaro.Results/Tessaro.Results.csproj -c Release   # package (version from git tags via MinVer)
for t in tests/scripts/*.test.sh; do bash "$t"; done                # CI script tests
```

A test run with zero tests exits with code 8 (MTP "zero tests ran"); a run below the coverage
threshold exits with code 14.

## Project board

Every task issue is on the [Tessaro.Results board](https://github.com/users/igortessaro/projects/4):
move its card to **In Progress** when the task starts; merging the PR that closes the issue moves it to **Done**.

## Collaboration

- Igor wants to evolve this workflow: when an instruction is worth turning into a skill,
  subagent, hook or rule, say so explicitly.
- Write repository artifacts in English; talk to Igor in Portuguese.
