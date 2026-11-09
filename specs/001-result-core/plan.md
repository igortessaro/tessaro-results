# Plan 001 — Result core

**Status:** Approved (2026-11-09) · **Created:** 2026-11-09
**Spec:** [001 — Result core](spec.md) (Approved 2026-11-09) · **Constitution:** [v1.0.0](../../docs/constitution.md)

> This plan defines **how** spec 001 is built. The ordered implementation steps belong in `tasks.md`.

---

## 1. Technical context

| Item | Choice | Reason |
|---|---|---|
| Language / runtime | C# `latest`, .NET SDK pinned in `global.json` (10.0.102, `latestFeature`) | Same as tessaro-optional; spiked 2026-11-09 |
| Target frameworks | `net10.0` (resolved question 1) | Current LTS; the consuming services are on `net10.0` and `net9.0` |
| Dependencies | None at runtime; `PublicApiAnalyzers` and `MinVer` with `PrivateAssets=all` | Constitution VI, NFR-1 |
| Test framework | xUnit v3 on Microsoft.Testing.Platform, Shouldly | Same as tessaro-optional; spiked |
| Coverage / analysis | `coverlet.MTP` with an 80 % line + branch threshold; SonarCloud | Constitution quality gates |
| Versioning | MinVer, tags `vX.Y.Z` | Version only from tags (ADR 0005 context) |
| Repository setup | `tessaro-bootstrap` skill (copies tessaro-optional's `.github/`, props, editorconfig, script tests) | One source of truth for shared setup |

Package versions: latest stable at implementation time, kept current by Dependabot.

## 2. Repository layout

```
src/Tessaro.Results/
  Tessaro.Results.csproj
  ErrorType.cs                 # the seven categories                        US-3
  Error.cs                     # Code, Message, Type, factories, equality    US-3
  Result.cs                    # state, factories, implicit Error, ToString  US-1, US-2, US-4, US-5
  Result.Composition.cs        # partial: Map / Bind / Match without value   US-6, US-7
  ResultOfT.cs                 # Result<T>: Value, implicit T/Error, ToString US-1, US-2, US-4
  ResultOfT.Composition.cs     # partial: Map / Bind / Match with value      US-6, US-7
  PublicAPI.Shipped.txt · PublicAPI.Unshipped.txt
tests/Tessaro.Results.Tests/
  CreateSuccessTests.cs        # AC-1.x
  CreateFailureTests.cs        # AC-2.x
  ErrorTests.cs                # AC-3.x
  InspectTests.cs              # AC-4.x
  HierarchyTests.cs            # AC-5.x
  TransformTests.cs            # AC-6.x
  MatchTests.cs                # AC-7.x
  NonFunctionalTests.cs        # NFR-1 (referenced assemblies), NFR-2 (immutability)
  Compatibility/               # NFR-3: compile-only consumers (see §5)
    UnrelatedNamespace.cs      #   namespace Consumer.Compat, with Microsoft.AspNetCore.Http
    SiblingNamespace.cs        #   namespace Tessaro.Compat, with Tessaro.Optional
tests/scripts/                 # from tessaro-bootstrap (PR title, release guard)
.github/ · .editorconfig · Directory.Build.props · Directory.Packages.props · global.json · dotnet-tools.json
Tessaro.Results.slnx · README.md · CONTRIBUTING.md · LICENSE · CLAUDE.md
```

Public types live in namespace `Tessaro`, tests in `Tessaro.Tests` ([ADR 0004](../../docs/adr/0004-namespace-tessaro.md)).
`Result` and `Result<T>` are split into `partial` files by concern so each file maps to one or
two user stories.

---

## 3. Public API

All in namespace `Tessaro`. Every delegate and reference argument is validated with
`ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` before anything
else runs (constitution II).

### 3.1 `ErrorType` and `Error`

```csharp
public enum ErrorType { Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden, Unexpected } // AC-3.1

[SuppressMessage("Naming", "CA1716", Justification = "...")]                   // see §8 R3
public sealed record Error                                                       // AC-3.3, AC-3.4 (record equality)
{
    private Error(string code, string message, ErrorType type);                 // AC-3.2 (validates both strings)
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    public static Error Failure(string code, string message);                   // AC-3.1, AC-3.2
    public static Error Validation(string code, string message);
    public static Error NotFound(string code, string message);
    public static Error Conflict(string code, string message);
    public static Error Unauthorized(string code, string message);
    public static Error Forbidden(string code, string message);
    public static Error Unexpected(string code, string message);

    public override string ToString();                                          // AC-3.5: "NotFound order.not_found: Order 42 was not found"
}
```

### 3.2 `Result` — outcome without a value

```csharp
public class Result
{
    private protected Result(...);                                               // AC-5.5 (closed hierarchy)

    public bool IsSuccess { get; }                                               // AC-1.1, AC-2.1
    public bool IsFailure { get; }                                               // AC-1.1, AC-2.1
    public IReadOnlyList<Error> Errors { get; }                                  // AC-1.1, AC-2.1, AC-2.2, AC-2.8, AC-2.9, AC-4.3

    public static Result Ok();                                                   // AC-1.1
    public static Result<T> Ok<T>(T value) where T : notnull;                    // AC-1.2, AC-1.3

    public static Result Fail(Error error);                                      // AC-2.1, AC-2.5
    public static Result Fail(IEnumerable<Error> errors);                        // AC-2.2, AC-2.4, AC-2.5, AC-2.8
    public static Result Fail(string message);                                   // AC-2.3, AC-2.6
    public static Result<T> Fail<T>(Error error) where T : notnull;              // AC-2.1, AC-2.5
    public static Result<T> Fail<T>(IEnumerable<Error> errors) where T : notnull; // AC-2.2, AC-2.4, AC-2.5, AC-2.8
    public static Result<T> Fail<T>(string message) where T : notnull;           // AC-2.3, AC-2.6

    public Result<T> ToFailure<T>() where T : notnull;                           // AC-5.3, AC-5.4

    public Result<TOut> Map<TOut>(Func<TOut> mapper) where TOut : notnull;       // AC-6.6, AC-6.7, AC-6.8
    public Result Bind(Func<Result> next);                                       // AC-6.5, AC-6.7, AC-6.9
    public Result<TOut> Bind<TOut>(Func<Result<TOut>> next) where TOut : notnull; // AC-6.5, AC-6.7, AC-6.9
    public TOut Match<TOut>(Func<TOut> onSuccess,
                            Func<IReadOnlyList<Error>, TOut> onFailure);         // AC-7.3, AC-7.4

    public static implicit operator Result(Error error);                         // AC-2.7
    public override string ToString();                                           // AC-4.4
}
```

### 3.3 `Result<T>` — outcome with a value

```csharp
public sealed class Result<T> : Result where T : notnull                         // AC-5.1, AC-5.2 (is-a Result)
{
    public T Value { get; }                                                      // AC-4.1, AC-4.2

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) where TOut : notnull;    // AC-6.1, AC-6.2, AC-6.7, AC-6.8
    public Result Bind(Func<T, Result> next);                                    // AC-6.3, AC-6.4, AC-6.7, AC-6.9
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next) where TOut : notnull; // AC-6.3, AC-6.4, AC-6.7, AC-6.9
    public TOut Match<TOut>(Func<T, TOut> onSuccess,
                            Func<IReadOnlyList<Error>, TOut> onFailure);         // AC-7.1, AC-7.2, AC-7.4

    public static implicit operator Result<T>(T value);                          // AC-1.3, AC-1.4
    public static implicit operator Result<T>(Error error);                      // AC-2.7
    public override string ToString();                                           // AC-4.4
}
```

The members without a value (`Map(Func<TOut>)`, `Bind(Func<Result>)`, `Match(Func<TOut>, …)`)
are inherited by `Result<T>` too. They are overloads, not hidden members, because the delegate
arity differs (spiked, §8 R1).

### 3.4 Behaviour details not stated in the spec

| Case | Behaviour | Reason |
|---|---|---|
| `Result.Ok()` | Always returns the same cached instance | Immutable, so sharing is safe; no allocation per call |
| Storing errors | Defensive copy into an array, exposed through `ReadOnlyCollection<Error>`; success shares one empty collection | AC-2.8, AC-2.9. A bare array cast to `IList<Error>` allows `errors[0] = x` (found in the spike) |
| `Fail(IEnumerable<Error>)` with a `null` element | `ArgumentNullException` with `paramName` `errors` | AC-2.5 |
| `Fail(string)` | Error with code `general.failure` and type `Failure` | AC-2.3, spec resolved question 2 |
| `Value` on a failure | `InvalidOperationException`: `"Cannot read the value of a failed result: {errors}"` | AC-4.2 |
| `ToFailure<T>()` on a success | `InvalidOperationException`: `"A successful result has no errors to carry over."` | AC-5.4 |
| `Map` / `Bind` delegate returns `null` | `InvalidOperationException` naming the delegate and suggesting a failure instead | AC-6.8, AC-6.9. Nullable annotations already reject `Map(q => (string?)null)` at compile time (spiked), so this guards `!`-suppressed code |
| `Bind` / `Map` on a failure of the same shape | `Result.Bind(Func<Result>)` and `Result<T>.Bind(Func<T, Result>)` return `this`; other shapes copy the error list reference (no new list) | AC-6.2, AC-6.4; errors are immutable |
| `ToString()` | `Success` · `Success(42)` (value's `ToString()`) · `Failure(NotFound order.not_found: …; Validation …)` | AC-4.4, for logs |
| `Result<Quote>.Ok()` / `.Fail(...)` | Compiles (static members are inherited) but returns `Result`; assigning it to `Result<Quote>` is a compile error (CS0266, spiked). Docs show `Result.Ok(...)` / `Result.Fail<T>(...)` only | Language rule; no silent wrong type |
| Implicit conversion from an **interface-typed** expression (`IReadOnlyList<int> x; return x;`) | Not available: C# forbids user-defined conversions from interfaces (CS0029, spiked). A concrete type works (`return new List<int>();`). Docs say to use `Result.Ok(x)` | Language rule |
| `Result<Error>` | Ambiguous conversions (CS0457, spiked); documented as unsupported | Spec resolved question 3 |
| `r.Bind(q => q.Status)` (lambda returns a plain value) | Does not infer `TOut`; write `Bind<string>(…)` or use `Map` | Type inference does not use user-defined conversions (spiked) |
| `error with { … }` | Only copies; properties are get-only, so `with { Code = "" }` does not compile (spiked) | AC-3.2 cannot be bypassed |

---

## 4. Build configuration

Shared files come from tessaro-optional through the `tessaro-bootstrap` skill:
`Directory.Build.props` (`TreatWarningsAsErrors`, nullable, `AnalysisLevel=latest-recommended`,
`EnforceCodeStyleInBuild`, docs file, deterministic CI build), `Directory.Packages.props`
(central package management), `.editorconfig`, `global.json` (SDK pin + MTP runner).

`Tessaro.Results.csproj` follows tessaro-optional's: `RootNamespace=Tessaro`, package ID
`Tessaro.Results`, MIT, README packed, symbols (`snupkg`), `MinVerTagPrefix=v`,
`PublicAPI.*.txt` as additional files. Its own description and tags
(`result;error-handling;functional`); the spike showed that copying the csproj also copies
Optional's description, so it is rewritten, not renamed.

`CA1716` is suppressed on `Error` only (§8 R3). No other suppressions.

## 5. Test strategy

- **Test-first**, one test class per user story; each test carries `[Trait("AC", "AC-x.y")]`
  so the `sdd` traceability script maps every AC to at least one test.
- **Both result shapes:** every AC that applies to `Result` and `Result<T>` is tested on both.
- **Every delegate is validated on success and failure** (AC-6.7, AC-7.4): parameterised tests
  pass `null` for each delegate in each state, and assert the delegate that must not run was not called.
- **Immutability (AC-2.8, AC-2.9, NFR-2):** mutate the source list after creation; cast `Errors`
  to `IList<Error>` and try both `Add` and the indexer setter.
- **NFR-1:** a test asserts the library assembly references only BCL assemblies
  (`System.*`, `netstandard`, `mscorlib`); the release workflow also checks the packed `.nuspec`
  has no dependency (already in the copied `release.yml`).
- **NFR-3 / consumer checks:** compile-only code under `tests/.../Compatibility/` uses the public
  names from an unrelated namespace importing `Microsoft.AspNetCore.Http` (the test project
  references the `Microsoft.AspNetCore.App` framework) and from `Tessaro.Compat` with the
  `Tessaro.Optional` package. If a name clashes, the test project stops compiling.
  A small test calls each, so it also counts toward the traceability check.
- **Coverage gate:** the CI test run fails below 80 % line and branch (spiked: exit code 14 with
  too few tests). SonarCloud's default gate applies 80 % to new code.

## 6. CI pipeline

Reused unchanged from tessaro-optional (via `tessaro-bootstrap`): `ci.yml` with the jobs
`format`, `build-test` (coverage gate), `scripts` (ShellCheck + `tests/scripts/*.test.sh`) and
SonarCloud analysis; `pr-title.yml` (ADR 0005); `release.yml`, dormant until the release spec
(candidate spec 003) sets up its environment and Trusted Publishing; Dependabot weekly with the
`Deps |` prefix; rulesets as code under `.github/rulesets/`. Workflows set
`defaults.run.shell: bash`; actions are pinned as in the reference.

## 7. One-time setup

| # | Step | Who |
|---|---|---|
| 1 | `git init` and an initial commit of the planning documents on `main` (prepared locally) | Claude |
| 2 | Create the public repo and push `main`: `gh repo create igortessaro/tessaro-results --public --source . --push` | Owner |
| 3 | SonarCloud: import `igortessaro/tessaro-results` (project key `igortessaro_tessaro-results`), turn off automatic analysis, create the token, then `gh secret set SONAR_TOKEN --repo igortessaro/tessaro-results` | Owner |
| 4 | Import the `main` ruleset after CI ran green once (required checks only exist then): `gh api -X POST repos/igortessaro/tessaro-results/rulesets --input .github/rulesets/main.json` | Owner |
| 5 | GitHub issues per task, milestone "Spec 001", label `task` (if the owner wants them, as in tessaro-optional) | Claude |

The release environment, the release-tags ruleset and nuget.org Trusted Publishing are set up in
the release spec, not here.

## 8. Risks

| ID | Risk | Mitigation |
|---|---|---|
| R1 | Overload resolution between inherited value-less members and `Result<T>` members (`Map`, `Bind`, `Match`), method groups (`Map(QuoteDto.From)`), and the implicit conversions inside `async` methods | **Spiked 2026-11-09:** all of §2's "Success looks like" compiles and runs; `Bind` with a lambda mixing `Result.Ok(1)` and `Result.Fail<int>(…)` resolves; `Match(() => …)` on a `Result<T>` resolves to the base overload |
| R2 | `Result`/`Error` clash with ASP.NET Core (`Results`, `TypedResults`, `IResult`) or with `Tessaro.Optional` in the shared `Tessaro` namespace | **Spiked:** compiled and ran a net8.0 web app (global `Microsoft.AspNetCore.Http` usings) in namespace `Consumer.App`, plus a `Tessaro.Sibling` namespace using `Optional<T>`; no clash, no alias needed. Kept as a permanent check (§5) |
| R3 | `Error` triggers CA1716 (VB.NET keyword) under `latest-recommended` | **Spiked:** confirmed. Suppress on the type with a justification (ecosystem-standard name; VB callers write `[Error]`), as tessaro-optional did for `Optional` |
| R4 | Toolchain: SDK 10.0.102, xUnit v3 on MTP, coverlet threshold, pack | **Spiked:** build (warnings as errors) ✓, `dotnet test` on MTP ✓, coverage threshold fails at 80 % with 3 tests (exit 14) and passes at 10 % ✓, `dotnet format --verify-no-changes` catches the `_field` naming rule ✓, `dotnet pack` from tag `v0.0.1-rc.1` gives `0.0.1-rc.1` with no dependency groups ✓ |
| R5 | Implicit conversion silently unavailable for interface-typed values | **Spiked:** compile error (CS0029), not silent. Documented (§3.4) |
| R6 | `Errors` mutable through a cast | **Found in the spike** (array behind `IReadOnlyList`). Fixed by `ReadOnlyCollection` and a test on the indexer setter (§5) |
| R7 | .NET 8 and .NET 9 leave support on 2026-11-10; newer SDKs may warn (`NETSDK1138`) for out-of-support targets, and warnings are errors | Resolved: `net10.0` only (resolved question 1). tessaro-optional targets `net8.0` and will hit the same warning — candidate follow-up there |

## 9. Constitution check

| Principle / gate | How this plan complies |
|---|---|
| I. Simplicity | Every public member in §3 carries the AC that requires it; nothing else is public |
| II. Fail fast | All arguments and delegates validated first, in both states (§3, §5); `Value` throws on failure; `null` successes rejected |
| III. Expected failures are values | Factories, implicit conversions and `Map`/`Bind`/`Match` keep handlers free of `if`; every propagation keeps all errors |
| IV. Transport-agnostic core | No HTTP/serialization types in `src/`; ASP.NET Core appears only in the test project's compatibility check |
| V. Clean design | `Result<T> : Result`; one `Validate` path for all failure factories; partial files per concern |
| VI. Zero runtime dependencies | Only `PrivateAssets=all` packages; NFR-1 test + release nuspec check |
| VII. Recorded deviations | Spec §5 is the migration checklist; README repeats it |
| Build / Format / Docs / Public API gates | Shared `Directory.Build.props`, `dotnet format` job, XML docs with `<exception>` tags, PublicApiAnalyzers |
| Tests / Coverage / Static analysis | xUnit v3 with AC traits, 80 % threshold in CI, SonarCloud |
| Pull request title | Copied `pr-title.yml` (ADR 0005) |

## 10. Deferred (not part of plan 001)

- **Async composition** — spec 002.
- **Release to nuget.org** — candidate spec 003: the release environment, release-tags ruleset and
  Trusted Publishing policy (owner steps), reusing the copied `release.yml`.
- **Allocation guarantees / benchmarks** — no NFR asks for them; revisit if a service shows a hot path.
- **ASP.NET Core adapter** — its own package and spec.

## 11. Resolved questions (2026-11-09, owner approved the recommendations)

1. **Target framework is `net10.0` only.** The services use `net10.0` and `net9.0`; .NET 8 and 9
   leave support on 2026-11-10. One target halves the test matrix and avoids `NETSDK1138` on an
   unsupported target; the `net9.0` services move to .NET 10 when they adopt the library.
2. **The "carry this failure over" member is `ToFailure<T>()`** (AC-5.3, AC-5.4).
3. **The GitHub repository is created after `tasks.md` is approved**, so T00 is the first PR.

## 12. Review checklist

- [x] Every acceptance criterion maps to an interface member and a test location
- [x] Nothing contradicts the constitution or an accepted ADR
- [x] Tooling choices are justified and minimal
- [x] CI enforces every quality gate in the constitution
- [x] Owner-only steps (credentials, sign-ins) are clearly separated
- [x] Riskiest assumptions were spiked or are explicitly open
- [x] Open questions answered or deferred
