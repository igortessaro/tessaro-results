# Plan 002 — Async composition

**Status:** Approved (2026-11-09) · **Created:** 2026-11-09
**Spec:** [002 — Async composition](spec.md) (Implemented 2026-11-09) · **Constitution:** [v1.0.0](../../docs/constitution.md)

> This plan defines **how** spec 002 is built. The ordered implementation steps belong in `tasks.md`.

---

## 1. Technical context

Unchanged from [plan 001 §1](../001-result-core/plan.md): C# `latest`, `net10.0`, no runtime
dependencies, xUnit v3 on Microsoft.Testing.Platform, Shouldly, coverlet.MTP (80 % gate),
SonarCloud, PublicApiAnalyzers, MinVer. **No new package** — asynchrony uses only
`System.Threading.Tasks` from the base class library.

## 2. Repository layout

```
src/Tessaro.Results/
  Result.Async.cs                   # partial Result: async steps on a completed result      US-2, US-3
  ResultOfT.Async.cs                # partial Result<T>: async steps on a completed result   US-2, US-3
  Result.AsyncGuard.cs              # partial Result / Result<T>: Map guard (§3.4, AC-3.7)  US-3
  ResultTaskExtensions.cs           # static partial class: members on Task<Result>         US-1, US-2, US-3
  ResultTaskExtensions.OfT.cs       # same class: members on Task<Result<T>>                 US-1, US-2, US-3
tests/Tessaro.Results.Tests/
  PendingResultTests.cs             # AC-1.x
  AsyncStepTests.cs                 # AC-2.x
  AsyncFailFastTests.cs             # AC-3.x
  NonFunctionalTests.cs             # + NFR-2 (synchronization context)
  Compatibility/AsyncSamples.cs     # NFR-3: spec 002 §2 example, unrelated + Tessaro.* namespaces
```

## 3. Public API

All in namespace `Tessaro`. Every member that returns a `Task` ends with `Async`
([ADR 0006](../../docs/adr/0006-async-members-end-with-async.md)). In the signatures below,
`Errors` stands for `IReadOnlyList<Error>`.

### 3.1 Async steps on a completed result (instance members)

```csharp
public partial class Result
{
    public Task<Result<TOut>> MapAsync<TOut>(Func<Task<TOut>> mapper) where TOut : notnull;                 // AC-2.1, 2.4, 2.5, 3.1, 3.4, 3.5
    public Task<Result> BindAsync(Func<Task<Result>> next);                                                // AC-2.2, 2.4, 2.5, 3.1, 3.4, 3.5
    public Task<Result<TOut>> BindAsync<TOut>(Func<Task<Result<TOut>>> next) where TOut : notnull;          // AC-2.2, 2.4, 2.5, 3.1, 3.4, 3.5
    public Task<TOut> MatchAsync<TOut>(Func<Task<TOut>> onSuccess, Func<Errors, Task<TOut>> onFailure);     // AC-2.3, 2.4, 2.5, 3.1, 3.4
}

public sealed partial class Result<T>
{
    public Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper) where TOut : notnull;              // AC-2.1, 2.4, 3.1, 3.4, 3.5
    public Task<Result> BindAsync(Func<T, Task<Result>> next);                                             // AC-2.2, 2.4, 3.1, 3.4, 3.5
    public Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> next) where TOut : notnull;       // AC-2.2, 2.4, 2.6, 3.1, 3.4, 3.5
    public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<Errors, Task<TOut>> onFailure);  // AC-2.3, 2.4, 3.1, 3.4
}
```

### 3.2 Members on a pending result (extension methods)

```csharp
public static partial class ResultTaskExtensions
{
    // On Task<Result<T>> — T : notnull, TOut : notnull where a result holds it
    public static Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> result, Func<T, TOut> mapper);                    // AC-1.1, 1.4, 3.1–3.3, 3.5, 3.6
    public static Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<TOut>> mapper);              // AC-2.1, 2.4, 3.1–3.6
    public static Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> result, Func<T, Result<TOut>> next);             // AC-1.2, 1.4, 3.1–3.3, 3.6
    public static Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<Result<TOut>>> next);       // AC-2.2, 2.4, 2.6, 3.1–3.6
    public static Task<Result> BindAsync<T>(this Task<Result<T>> result, Func<T, Result> next);                               // AC-1.2, 1.4, 3.1–3.3
    public static Task<Result> BindAsync<T>(this Task<Result<T>> result, Func<T, Task<Result>> next);                         // AC-2.2, 2.4, 3.1–3.6
    public static Task<TOut> MatchAsync<T, TOut>(this Task<Result<T>> result, Func<T, TOut> onSuccess, Func<Errors, TOut> onFailure);                   // AC-1.3, 1.4, 3.1–3.3
    public static Task<TOut> MatchAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<TOut>> onSuccess, Func<Errors, Task<TOut>> onFailure);       // AC-2.3, 2.4, 3.1–3.4

    // On Task<Result> — the same eight members with delegates taking no value               // AC-1.5, 2.5 (+ the IDs above)
    public static Task<Result<TOut>> MapAsync<TOut>(this Task<Result> result, Func<TOut> mapper);
    public static Task<Result<TOut>> MapAsync<TOut>(this Task<Result> result, Func<Task<TOut>> mapper);
    public static Task<Result<TOut>> BindAsync<TOut>(this Task<Result> result, Func<Result<TOut>> next);
    public static Task<Result<TOut>> BindAsync<TOut>(this Task<Result> result, Func<Task<Result<TOut>>> next);
    public static Task<Result> BindAsync(this Task<Result> result, Func<Result> next);
    public static Task<Result> BindAsync(this Task<Result> result, Func<Task<Result>> next);
    public static Task<TOut> MatchAsync<TOut>(this Task<Result> result, Func<TOut> onSuccess, Func<Errors, TOut> onFailure);
    public static Task<TOut> MatchAsync<TOut>(this Task<Result> result, Func<Task<TOut>> onSuccess, Func<Errors, Task<TOut>> onFailure);
}
```

A `Task<Result<T>>` is not a `Task<Result>` (tasks are invariant), so both receiver sets are
needed; they never compete for the same call.

### 3.3 Compile-time guard on the synchronous `Map` (resolved question 1)

```csharp
public partial class Result
{
    [Obsolete("The mapper returns a Task: use MapAsync, or the result would hold an unawaited task.", error: true)]
    public Result<Task<TOut>> Map<TOut>(Func<Task<TOut>> mapper);                     // AC-3.7
}

public sealed partial class Result<T>
{
    [Obsolete("The mapper returns a Task: use MapAsync, or the result would hold an unawaited task.", error: true)]
    public Result<Task<TOut>> Map<TOut>(Func<T, Task<TOut>> mapper);                  // AC-3.7
}
```

### 3.4 Behaviour details not stated in the spec

| Case | Behaviour | Reason |
|---|---|---|
| Argument checks | Every member is a **non-`async`** method that validates its arguments (receiver task, delegates) and only then calls an `async` local function | AC-3.1/3.2: an `async` method would put the exception into the returned task. Spiked: `ArgumentNullException` is thrown synchronously, even on a task that never completes |
| Awaits | Every `await` in the library uses `ConfigureAwait(false)` | NFR-2. Spiked: 0 posts to the caller's context; removing it gives 6 |
| Completed failure + async step | Returns `Task.FromResult(…)` with the failure carried over (no state machine, no delegate call) | AC-2.4; same carrying-over as spec 001 (`ToFailure`, or `this` for the same shape) |
| Pending members | Await the receiver, then call the completed-result member (`Map`, `Bind`, `Match`, or the async ones of §3.1) — one implementation per operation | DRY; semantics identical to spec 001 by construction |
| Pending result completes with `null` | `InvalidOperationException`: `"The pending result completed with null."` | AC-3.3 |
| Async step returns a `null` task | `InvalidOperationException`: `"The mapper returned a null task."` / `"The next operation returned a null task."` / `"The branch returned a null task."` | AC-3.4 |
| Task completes with `null` value / result | The spec 001 messages (`MapperReturnedNull`, `NextReturnedNull`) | AC-3.4 |
| Faulted or cancelled receiver or step | Not caught: `await` rethrows the original exception; cancellation surfaces as `TaskCanceledException` (an `OperationCanceledException`) | AC-3.5, AC-3.6. Spiked |
| `MatchAsync` with sync branches on a pending result whose branches return different types | Name the type: `MatchAsync<OrderDto, IResult>(…)` | Same as spec 001 `Match<IResult>`; documented |
| `result.BindAsync(o => o.ChangeStatus(…))` on a **completed** result (sync step) | Compile error (only async steps exist on completed results); use `Bind` | Spiked: CS0029, by design |
| `result.Map(o => repository.GetAsync(o.Id))` (async lambda in the sync `Map`) | Without the guard: compiles and returns `Result<Task<Order>>`. With the §3.3 guard: **CS0619** pointing to `MapAsync` | Spiked — see §8 R2 |
| `result.Map<string>(_ => null!)` (explicit type argument and an untyped `null`) | *Amended 2026-11-09 (T01):* ambiguous with the guard (CS0121) — the `null` literal fits both `Func<T, string>` and `Func<T, Task<string>>`. Type it: `_ => (string)null!`. Spec 001's AC-6.8 test was adjusted that way | Side effect of the AC-3.7 guard; only an untyped `null` with an explicit type argument is affected (spiked in the T01 consumer check) |

## 4. Build configuration

No change. The `[Obsolete(error: true)]` guards are public API: they go into
`PublicAPI.Unshipped.txt` like any member, with XML docs (`<exception>` not applicable; they throw
`NotSupportedException` only if called through reflection).

## 5. Test strategy

- One test class per user story, each test tagged `[Trait("AC", "AC-x.y")]`; the `sdd`
  traceability script must pass for `specs/002-async-composition/spec.md` (and still for 001).
- **Matrix:** each AC runs on both shapes and, where it applies, on completed and pending
  receivers, with sync and async steps. Parameterised helpers build each combination, so the
  number of test methods stays small.
- **"Not called"** is asserted with counting delegates; **"before awaiting"** with a receiver from a
  `TaskCompletionSource` that never completes (the call must throw, not hang).
- **AC-3.5/3.6:** `Task.FromException`, a step that throws after `await Task.Yield()`,
  `Task.FromCanceled`; assert the exact exception type and message.
- **AC-3.7:** reflection asserts each guard carries `ObsoleteAttribute { IsError: true }`
  with the message; the T-task's consumer check shows the CS0619 at the call site.
- **NFR-2:** install a counting `SynchronizationContext`, start a chain on a receiver that completes
  later, assert zero posts. Proved able to fail (spike: 6 posts without `ConfigureAwait(false)`).
- **NFR-3:** `Compatibility/AsyncSamples.cs` compiles and runs the spec §2 handler and controller
  from `Consumer.Compat` (with ASP.NET Core) and a chain inside `Tessaro.Compat`.
- Coverage gate unchanged (80 % line and branch); the async local functions are covered by the matrix.

## 6. CI pipeline

No change (plan 001 §6).

## 7. One-time setup

None: repository, SonarCloud (including the Dependabot secret) and the board already exist.
Claude creates the spec 002 milestone, the `task` issues and adds them to the
[board](https://github.com/users/igortessaro/projects/4) once `tasks.md` is approved.

## 8. Risks

| ID | Risk | Mitigation |
|---|---|---|
| R1 | Inside `MapAsync`/`BindAsync`/`MatchAsync`, the sync-step and async-step overloads compete for a lambda returning a `Task` (ADR 0006) | **Spiked 2026-11-09**, each case with its type declared and a **must-fail control**: 16 cases bind as intended — async lambda, `async` keyword, method groups (`MapAsync(OrderDto.From)`, `BindAsync(repo.UpdateAsync)`), value-type `TOut`, `Bind` to `Result` vs `Result<TOut>`, `(Result)Error…` returns, the full spec §2 chain, `MatchAsync` sync/async branches, async steps on completed results |
| R2 | The synchronous `Map` accepts an async lambda and returns `Result<Task<Order>>` — ADR 0006's "never by accident" does not hold without help | **Found in the spike.** `[Obsolete(error: true)] Map(Func<…, Task<TOut>>)` overloads turn it into CS0619 with a pointer to `MapAsync`; ordinary `Map` calls (lambdas, method groups) are unaffected (spiked). Spec amended with AC-3.7 (resolved question 1) |
| R3 | Argument exceptions surface from the returned task instead of at the call | **Spiked:** non-`async` wrapper + `async` local function throws synchronously, even on a never-completing receiver |
| R4 | Continuations resume on the caller's context (deadlock risk for blocking callers) | **Spiked:** `ConfigureAwait(false)` everywhere → 0 posts; mutant without it → 6 |
| R5 | Exceptions wrapped (`AggregateException`) or cancellation turned into a failure | **Spiked:** original `TimeoutException`/`FormatException` and `TaskCanceledException` reach the caller |
| R6 | Analyzers (`latest-recommended`) reject the async patterns or the `Obsolete` guard | **Spiked:** prototype builds with analyzers on and warnings as errors, 0 findings |
| R7 | A consumer spike reports "compiles" for code that never compiled | **Happened in this spike** (the library failed RS0016 first). Every case now declares its type and the run includes a must-fail control; the lesson was added to the `sdd` skill |

## 9. Constitution check

| Principle / gate | How this plan complies |
|---|---|
| I. Simplicity | Every member traces to an AC of spec 002 (the guard: AC-3.7); no `ValueTask`, tokens or `Tap` (spec §6) |
| II. Fail fast | Arguments validated at the call, before awaiting (§3.4); `null` tasks and results throw; the guard moves a silent bug to compile time |
| III. Expected failures are values | Failures flow through every async step with all errors; exceptions stay exceptions (AC-3.5/3.6) |
| IV. Transport-agnostic core | No new dependency; ASP.NET Core only in the compatibility tests |
| V. Clean design (DRY) | Pending members delegate to the completed-result members; one carrying-over path (`ToFailure`) |
| VI. Zero runtime dependencies | BCL only (NFR-1, existing test) |
| VII. Recorded deviations | Spec §5 maps every async member to its spec 001 counterpart |
| Gates | Unchanged CI; XML docs and PublicAPI tracking for all 26 members |

## 10. Deferred (not part of plan 002)

- `ValueTask` overloads, `CancellationToken` parameters, `Tap`, `Try`, `WhenAll` (spec §6).
- A Roslyn analyzer for unawaited result tasks — the `Async` names plus the compiler's own
  "unawaited task" warnings (CS4014) cover the common case.

## 11. Resolved questions (2026-11-09, owner approved the recommendations)

1. **The compile-time guard (§3.3) is added**, and the spec gains **AC-3.7**. ADR 0006 has a dated
   note that its "never by accident" consequence relies on this guard.
2. **One extension class, `ResultTaskExtensions`**, for both receiver shapes, in two partial files.

## 12. Review checklist

- [x] Every acceptance criterion maps to an interface member and a test location
- [x] Nothing contradicts the constitution or an accepted ADR
- [x] Tooling choices are justified and minimal
- [x] CI enforces every quality gate in the constitution
- [x] Owner-only steps (credentials, sign-ins) are clearly separated
- [x] Riskiest assumptions were spiked or are explicitly open
- [x] Open questions answered or deferred
