# Tessaro.Results — Constitution

> The non-negotiable principles of this project. Every spec, plan, task and pull request
> is checked against this document. Changing it requires an ADR in `docs/adr/`.

**Version:** 1.0.0 · **Ratified:** 2026-11-09 · **Owner:** Igor Tessaro

---

## Mission

Give .NET applications one well-designed, dependency-free way to return the outcome of an
operation — success with or without a value, or failure with one or more errors — instead of
throwing exceptions for expected failures. It replaces the `Response` / `Response<T>` /
`ResponseFactory` classes that are copied, with only the namespace changed, into several
FIAP-SOAT-Net services (`fiap-soat-oficina-mecanica`, `smw-service-order`,
`fiap-soat-hackaton-file-service`, `fiap-soat-hackaton-processing-service`). It keeps what those
classes do well and fixes their known problems: `Response<T>` cannot be returned where a
`Response` is expected, the domain depends on `HttpStatusCode` and on FluentResults, and mapping
a failure keeps only the first error message.

---

## Principles

### I. Simplicity first (KISS · YAGNI)

- A public member exists only if an **approved spec** requires it. No speculative API.
- Prefer the smallest design that satisfies the spec; clever code needs a written justification.
- New features land in a new numbered spec (`specs/NNN-*`), never by "while I'm here" changes.

### II. Fail fast

- Invalid input is rejected **immediately**, at the call site, with the standard .NET exception
  (`ArgumentNullException`, `ArgumentException`, `InvalidOperationException`, …) and a clear message.
- **Every delegate argument is validated eagerly**, whether the result is a success or a failure.
- A failure always carries **at least one error**; creating a failure with no errors, or with a
  `null` error, throws.
- Reading the value of a failed result throws `InvalidOperationException`; there is no
  "value or default" that silently hides a failure behind `default(T)`.
- No silent fallbacks, no swallowed exceptions.

### III. Expected failures are values, not exceptions

- Outcomes the caller is expected to handle (not found, validation, conflict, …) are returned
  as failed results. Exceptions are reserved for programmer errors (principle II) and truly
  unexpected conditions.
- The API makes the common flows expressible as a single fluent expression (transform,
  chain, branch) without `if (!result.IsSuccess)` boilerplate.
- Errors are never lost: every operation that propagates a failure propagates **all** of its errors.

### IV. Transport-agnostic core

- The core package knows nothing about HTTP, ASP.NET Core, messaging or serialization
  frameworks: no `HttpStatusCode`, no `IActionResult`, no `[JsonIgnore]`.
- Errors describe **what went wrong** (a category, a code, a message); mapping that to a status
  code or a response body is the job of an adapter package, each with its own spec.

### V. Clean design (SOLID · DRY)

- A result with a value **is a** result (Liskov): anything that accepts a result without a value
  accepts a result with one, with no conversion or re-creation.
- Aliases and convenience overloads delegate to one primary implementation — one source of truth.
- Types are small and cohesive; extension methods are grouped by the concept they extend.

### VI. Zero runtime dependencies

- The shipped package depends only on the .NET base class library — not on FluentResults,
  not on `Tessaro.Optional`. Interop with other packages lives in separate packages.
- Test and build tooling may use third-party packages.

### VII. Recorded deviations

- The `Response.cs` copies are the reference being replaced. Every behaviour that differs from
  them is listed in the spec (or an ADR) with the reason, so migrating a service is a
  checklist, not a surprise.

---

## Quality gates (enforced by CI)

| Gate | Rule |
|---|---|
| Build | `TreatWarningsAsErrors=true`, nullable reference types enabled, .NET analyzers on (`latest-recommended`) |
| Format | `dotnet format --verify-no-changes` passes |
| Tests | Test-first (TDD). Every public member has tests for the success case, the failure case and invalid arguments; every acceptance criterion has a test tagged with its ID |
| Coverage | ≥ 80 % line and branch coverage, enforced by the CI test run; SonarCloud's default quality gate (80 % on new code) must also pass |
| Static analysis | SonarCloud quality gate green; no new bugs, vulnerabilities or code smells above *minor* |
| Documentation | XML docs on 100 % of the public API, including `<exception>` tags |
| Public API | Changes tracked with `Microsoft.CodeAnalysis.PublicApiAnalyzers`; breaking changes only in a major version (SemVer) |
| Pull request title | Checked in CI against [ADR 0005](adr/0005-pull-request-title-and-description.md) |

---

## Workflow (Spec-Driven Development)

1. **Spec** (`spec.md`) — *what* and *why*, user stories, acceptance criteria. No implementation details.
2. **Plan** (`plan.md`) — *how*: signatures, target frameworks, tooling, test strategy, spikes.
3. **Tasks** (`tasks.md`) — small, ordered, independently verifiable steps, tests first.
4. **Implement** — one task per branch → pull request → green CI → merge.

Each phase is reviewed and approved by the owner before the next one starts.

## Repository governance

- **Branching: trunk-based.** Short-lived branches off `main`; no `develop` branch. Branch names
  use the Gitflow prefixes: `feature/NNN-slug` for all planned work (features, docs, CI, chores,
  tests), `hotfix/slug`, `release/x.y.z`. Commit types (`feat`, `ci`, …) are never branch prefixes.
- `main` is protected (rulesets as code in `.github/rulesets/`): no direct pushes, no
  force-pushes, no deletion; pull request with all required checks green; linear history.
  Only the owner may merge or bypass, or tells Claude explicitly to merge a specific PR.
- Claude (AI assistant) works exclusively on feature branches and opens pull requests;
  it never pushes to `main` and never handles credentials or tokens.
- Commits on feature branches follow [Conventional Commits](https://www.conventionalcommits.org/).
  Pull request titles and descriptions follow [ADR 0005](adr/0005-pull-request-title-and-description.md);
  the squash commit on `main` is the PR title + ` (#PR)`.
- **No AI attribution:** no `Co-Authored-By` trailer in any commit (squash merges included) and no
  "Generated with Claude Code" footer in pull requests.
- Releases follow the routine in `CONTRIBUTING.md` (tag on `main` → CI publishes to nuget.org with
  Trusted Publishing, after the owner's approval), reused from `tessaro-optional` spec 002.
