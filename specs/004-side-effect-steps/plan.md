# Plan 004 — Side-effect steps

**Status:** Approved (2026-11-11) · **Created:** 2026-11-11
**Spec:** [004 — Side-effect steps](spec.md) (Approved 2026-11-11) · **Constitution:** [v1.0.0](../../docs/constitution.md)

> This plan defines **how** spec 004 is built. The ordered implementation steps belong in `tasks.md`.

---

## 1. Technical context

Unchanged from [plan 001 §1](../001-result-core/plan.md) and [plan 002 §1](../002-async-composition/plan.md):
C# `latest`, `net10.0`, no runtime dependencies, xUnit v3 on Microsoft.Testing.Platform, Shouldly,
coverlet.MTP (80 % gate), SonarCloud, PublicApiAnalyzers, MinVer. **No new package or tool.**

## 2. Repository layout

```
src/Tessaro.Results/
  ResultSideEffectExtensions.cs          # static partial class: steps on a completed result        US-1, US-2, US-3
  ResultSideEffectExtensions.Pending.cs  # same class: steps on Task<Result> / Task<Result<T>>     US-3
  ResultSideEffectExtensions.Guards.cs   # same class: [Obsolete(error: true)] guards (§3.3)       US-5
  ResultTaskExtensions.cs                # Completed(...) helper becomes internal, shared (§3.4)
tests/Tessaro.Results.Tests/
  SuccessSideEffectTests.cs              # AC-1.x
  FailureSideEffectTests.cs              # AC-2.x
  AsyncSideEffectTests.cs                # AC-3.x
  SideEffectFailFastTests.cs             # AC-4.x
  SideEffectGuardTests.cs                # AC-5.x
  NonFunctionalTests.cs                  # + NFR-2 for the new async members
  Compatibility/SideEffectSamples.cs     # NFR-3: spec §2 example, unrelated + Tessaro.* namespaces
```

New tests carry `[Trait("Spec", "004")]` and `[Trait("AC", "AC-x.y")]`.

## 3. Public API

All in namespace `Tessaro`, all in **one static class, `ResultSideEffectExtensions`**. `Errors`
stands for `IReadOnlyList<Error>`. Every member that returns a `Task` ends with `Async`
([ADR 0006](../../docs/adr/0006-async-members-end-with-async.md)).

**Why extension methods constrained to `Result`, not instance members** (resolved question 2): a
side effect returns its receiver, so one generic member `TResult OnFailure<TResult>(this TResult result, …) where TResult : Result`
serves both shapes and keeps the receiver's static type — `Result<Order>` in, `Result<Order>` out
(AC-1.4, AC-2.3, AC-3.7) — with **one implementation** (DRY). Instance members would need a
`Result` version plus a covariant override or a `new` member on `Result<T>` for each step, and
twice the pending members (tasks are invariant). The same generic trick covers pending receivers:
`Task<TResult>` infers `TResult = Result<Order>` from a `Task<Result<Order>>`. Only the success
step that receives the value needs a `Result<T>`-specific member.

### 3.1 Steps on a completed result

```csharp
public static partial class ResultSideEffectExtensions
{
    public static TResult OnSuccess<TResult>(this TResult result, Action action) where TResult : Result;                      // AC-1.2, 1.3, 1.5, 4.1, 4.5
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action) where T : notnull;                         // AC-1.1, 1.3, 1.4, 1.5, 4.1, 4.5
    public static TResult OnFailure<TResult>(this TResult result, Action<Errors> action) where TResult : Result;              // AC-2.1, 2.2, 2.3, 1.5, 4.1, 4.5

    public static Task<TResult> OnSuccessAsync<TResult>(this TResult result, Func<Task> action) where TResult : Result;       // AC-3.1, 3.4, 3.5, 3.6, 4.1, 4.4–4.6
    public static Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action) where T : notnull;          // AC-3.1, 3.4, 3.6, 4.1, 4.4–4.6
    public static Task<TResult> OnFailureAsync<TResult>(this TResult result, Func<Errors, Task> action) where TResult : Result; // AC-3.3, 3.4, 3.5, 3.6, 4.1, 4.4–4.6
}
```

No synchronous action on a completed result's `…Async` members (spec resolved question 4): the
spike shows `r.OnSuccessAsync(o => log.Add(o))` fails to compile (CS1662), as `BindAsync` does.

### 3.2 Steps on a pending result

```csharp
public static partial class ResultSideEffectExtensions
{
    public static Task<TResult> OnSuccessAsync<TResult>(this Task<TResult> result, Action action) where TResult : Result;      // AC-3.2, 3.4, 3.5, 4.1–4.3, 4.5, 4.6
    public static Task<TResult> OnSuccessAsync<TResult>(this Task<TResult> result, Func<Task> action) where TResult : Result;  // AC-3.1, 3.4–3.6, 4.1–4.6, 5.2
    public static Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> result, Action<T> action) where T : notnull;        // AC-3.2, 3.4, 3.7, 4.1–4.3, 4.5, 4.6
    public static Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> result, Func<T, Task> action) where T : notnull;    // AC-3.1, 3.4, 3.6, 3.7, 4.1–4.6, 5.2
    public static Task<TResult> OnFailureAsync<TResult>(this Task<TResult> result, Action<Errors> action) where TResult : Result;      // AC-3.3, 3.4, 3.5, 4.1–4.3, 4.5, 4.6
    public static Task<TResult> OnFailureAsync<TResult>(this Task<TResult> result, Func<Errors, Task> action) where TResult : Result;  // AC-3.3, 3.4–3.6, 4.1–4.6, 5.2
}
```

A lambda returning a `Task` binds to the `Func<…, Task>` member, not the `Action` one (C# prefers a
delegate with a return type for a lambda that has one) — spiked, AC-5.2.

### 3.3 Compile-time guards

All `[Obsolete(message, error: true)]`, `[EditorBrowsable(EditorBrowsableState.Never)]`, body
`throw new NotSupportedException(message)` (reachable only through reflection), XML-documented
and listed in `PublicAPI.Unshipped.txt`, like spec 002's `Map` guards.

**Task-returning action in a synchronous step** (spec AC-5.1) — message:
*"The action returns a Task: use OnSuccessAsync (OnFailureAsync), or the task would not be awaited."*

```csharp
public static TResult OnSuccess<TResult>(this TResult result, Func<Task> action) where TResult : Result;           // AC-5.1
public static Result<T> OnSuccess<T>(this Result<T> result, Func<T, Task> action) where T : notnull;              // AC-5.1
public static TResult OnFailure<TResult>(this TResult result, Func<Errors, Task> action) where TResult : Result;   // AC-5.1
```

**Result-returning action in a success step** (AC-5.3, resolved question 1) — message:
*"The action returns a result: use Bind (BindAsync), or its errors would be lost."*

```csharp
public static TResult OnSuccess<TResult, TOut>(this TResult result, Func<TOut> action) where TResult : Result where TOut : Result;
public static Result<T> OnSuccess<T, TOut>(this Result<T> result, Func<T, TOut> action) where T : notnull where TOut : Result;
public static Task<TResult> OnSuccessAsync<TResult, TOut>(this TResult result, Func<Task<TOut>> action) where TResult : Result where TOut : Result;
public static Task<Result<T>> OnSuccessAsync<T, TOut>(this Result<T> result, Func<T, Task<TOut>> action) where T : notnull where TOut : Result;
public static Task<TResult> OnSuccessAsync<TResult, TOut>(this Task<TResult> result, Func<TOut> action) where TResult : Result where TOut : Result;
public static Task<TResult> OnSuccessAsync<TResult, TOut>(this Task<TResult> result, Func<Task<TOut>> action) where TResult : Result where TOut : Result;
public static Task<Result<T>> OnSuccessAsync<T, TOut>(this Task<Result<T>> result, Func<T, TOut> action) where T : notnull where TOut : Result;
public static Task<Result<T>> OnSuccessAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<TOut>> action) where T : notnull where TOut : Result;
```

Without them, `pending.OnSuccessAsync(o => repository.UpdateAsync(o, ct))` compiles, awaits the
update and **drops its failure** — the chain carries on as a success (spiked). Constitution III:
errors are never lost. Failure steps get no such guard: the chain is already a failure.

Total: 23 public members — 12 working members, 3 Task guards and 8 result guards.

### 3.4 Behaviour details not stated in the spec

| Case | Behaviour | Reason |
|---|---|---|
| Argument checks | Every member is non-`async`: it validates the receiver and the action, then (pending/async) calls an `async` local function | AC-4.1/4.2, same pattern as plan 002 §3.4 |
| `null` receiver on a **completed** result | `ArgumentNullException` (extension methods can be called on `null`) | Constitution II; covered with AC-4.1's tests |
| Returned value | The receiver itself (`return result;`); completed async members on the branch that doesn't run return `Task.FromResult(result)` (no state machine, action not called) | AC-1.1, 1.3, 2.1, 2.2, 3.4 (resolved question 2) |
| Pending members | Await the receiver, then call the completed-result member of §3.1 — one implementation per step | DRY, as plan 002 |
| Pending result completes with `null` | `InvalidOperationException`: `"The pending result completed with null."` — `ResultTaskExtensions.Completed` becomes `internal` and is reused, not copied | AC-4.3; DRY |
| Async action returns a `null` task | `InvalidOperationException`: `"The action returned a null task."` | AC-4.4 |
| Awaits | `ConfigureAwait(false)` everywhere | NFR-2 |
| Faulted or cancelled receiver or action | Not caught: the original exception / `TaskCanceledException` surfaces | AC-4.5, 4.6 |
| `r.OnSuccess(Console.WriteLine)` — a method group with **overloads** for both no argument and one argument | Ambiguous (CS0121) between the `Action` and `Action<T>` members; use a lambda, `o => Console.WriteLine(o)`. Non-overloaded method groups bind normally (spiked) | Cost of the single generic member (§3); README known limitation |
| An action that only throws: `r.OnSuccess(o => throw x)` (expression or block body) | Hits the Task guard (CS0619): a lambda without a return type converts to `Func<T, Task>` too and that delegate wins. Call a method instead: `o => Fail(o)` | Spiked; rare in production code, relevant for AC-4.5 tests (they use a throwing method) |
| `r.OnSuccess(o => { PublishAsync(o); })` — a **block** lambda discarding a task in a statement | Compiles (`Action<T>`): C# gives no warning for a discarded task outside an `async` method; the guards only see a lambda's return type | README known limitation; an analyzer is deferred (§10) |
| Action returning `ValueTask` | Not guarded: binds to `Action`, the `ValueTask` is discarded | `ValueTask` is out of scope (spec §6); README known limitation |
| A non-result return value (`o => set.Add(o)`, returns `bool`) | Binds to `Action<T>`; the value is ignored. The result guards don't apply (`bool` fails the `TOut : Result` constraint) | Spiked |

## 4. Build configuration

No change.

## 5. Test strategy

- One test class per user story; traceability per spec as in `CLAUDE.md`, now for
  `specs/004-side-effect-steps/spec.md` (and still 001 and 002).
- **Matrix:** each AC on both shapes and, where it applies, completed and pending receivers with
  sync and async actions, through parameterised helpers (as plan 002).
- **Same instance** with `ShouldBeSameAs`; **not called** and **once** with counting actions;
  **order** (AC-1.5) with a shared log.
- **Awaited** (AC-3.6, AC-5.2): an action returning a `TaskCompletionSource` task; the step's task
  is not completed until it is set.
- **Before awaiting** (AC-4.1/4.2): a receiver that never completes; the call must throw.
- **AC-4.5/4.6:** throwing method, `Task.FromException`, a fault after `await Task.Yield()`,
  `Task.FromCanceled`; exact exception, and a later step's counter stays at zero.
- **AC-5.1, AC-5.3:** reflection asserts each guard has `ObsoleteAttribute { IsError: true }` and
  its message (`BindingFlags.DeclaredOnly` not needed: one static class). The task's consumer
  check compiles every guarded call shape and must see CS0619 (§8 R1 lists them).
- **NFR-2:** counting `SynchronizationContext`, zero posts for a pending chain of the new members.
- **NFR-3:** `Compatibility/SideEffectSamples.cs` runs the spec §2 handler and worker from
  `Consumer.Compat` and from `Tessaro.Compat`.
- **NFR-4:** the 174 tests of specs 001–002 pass unchanged (spiked: 174/174 with the prototype).
- Coverage gate unchanged. Guard bodies are excluded only if they can't be reached: a reflection
  test invokes one per signature and expects `NotSupportedException`, so no exclusion is needed.

## 6. CI pipeline

No change.

## 7. One-time setup

None. Claude creates the spec 004 milestone and `task` issues and adds them to the
[board](https://github.com/users/igortessaro/projects/4) once `tasks.md` is approved.

## 8. Risks

| ID | Risk | Mitigation |
|---|---|---|
| R1 | A Task-returning action binds silently to the synchronous `Action` member (fire-and-forget, `async void`) | **Spiked 2026-11-11** (prototype with analyzers on, warnings as errors; consumer cases with declared types; must-fail control `Result<Order> x = r.Map(o => o.Id)` → CS0029): CS0619 for expression lambda, method group, `async` expression and block lambdas, `Result` and `Result<T>`, `OnFailure`, and a `Task<Result<…>>`-returning lambda — 9/9 |
| R2 | On pending results the `Action` and `Func<…, Task>` members compete | **Spiked:** Task-returning lambdas, `async` lambdas and method groups bind to `Func<…, Task>` and are awaited (a `TaskCompletionSource` gate kept the step's task pending until released); void lambdas bind to `Action` |
| R3 | The generic receiver `TResult : Result` also matches a `Task<…>` receiver, or loses the value type | **Spiked:** candidates whose `TResult` fails the constraint are dropped (C# 7.3+); `Result<Order>` in → `Result<Order>` out, pending too; the spec §2 chain compiles as `Task<Result<Order>>` and runs |
| R4 | New overloads change existing call shapes (NFR-4) | **Spiked:** the new members have new names; spec 001–002's 174 tests, including the compatibility samples, pass against the prototype. `r.Map<string>(_ => (string)null!)`, `r.Map(o => o.Id)`, `pending.MapAsync(o => o.Id)` compile as before |
| R5 | A result-returning action drops its errors (`OnSuccessAsync(o => repository.UpdateAsync(o))`) | **Found in the spike** (compiles and continues as a success). §3.3 result guards turn all 8 shapes into CS0619 pointing to `Bind`; `bool`/`int`-returning lambdas still bind to `Action` (spiked). Spec AC-5.3 (resolved question 1) |
| R6 | Overloaded method groups (`Console.WriteLine`) become ambiguous; throw-only lambdas hit the Task guard | **Found in the spike**; accepted and documented (§3.4) |
| R7 | A consumer spike reports "compiles" for code that never compiled | Every case declares its type; the library build was checked to succeed first; the must-fail control fails |

## 9. Constitution check

| Principle / gate | How this plan complies |
|---|---|
| I. Simplicity | Two step names; every working member traces to an AC; guards trace to AC-5.1 and AC-5.3. No `Tap` alias, `ValueTask` or tokens |
| II. Fail fast | Receiver and action validated at the call, before awaiting; `null` tasks and results throw; guards move silent bugs to compile time |
| III. Expected failures are values | Failures pass on as the same instance; exceptions stay exceptions; the result guards stop a step from dropping errors |
| IV. Transport-agnostic core | No new dependency; ASP.NET Core only in the compatibility tests |
| V. Clean design (DRY) | One generic member per step for both shapes; pending members delegate to completed ones; `Completed` helper shared, not copied |
| VI. Zero runtime dependencies | BCL only |
| VII. Recorded deviations | Spec §5 maps each step to its spec 001/002 counterpart |
| Gates | Unchanged CI; XML docs and PublicAPI tracking for every member, guards included |

## 10. Deferred (not part of plan 004)

- `ValueTask` actions and a guard for them; `CancellationToken` parameters (spec §6).
- A Roslyn analyzer for tasks discarded inside block lambdas (§3.4) — revisit if a service hits it.
- Side effects on both outcomes, side effects that can fail the chain, error-type filters (spec §6).

## 11. Resolved questions (2026-11-11, owner approved the recommendations)

1. **The 8 result guards (§3.3) are added**, and the spec gains **AC-5.3**: a success step that
   receives an action returning a result (or a task of one) doesn't compile and points to
   `Bind`/`BindAsync`.
2. **Extension methods constrained to `Result`** (§3), one static class `ResultSideEffectExtensions`,
   instead of instance members; the overloaded-method-group ambiguity is a README known limitation.
3. **ADR 0006 gets a dated note** that its "never accepts an async delegate by accident"
   consequence also relies on the `OnSuccess`/`OnFailure` guards. No new ADR.

## 12. Review checklist

- [x] Every acceptance criterion maps to an interface member and a test location
- [x] Nothing contradicts the constitution or an accepted ADR
- [x] Tooling choices are justified and minimal
- [x] CI enforces every quality gate in the constitution
- [x] Owner-only steps (credentials, sign-ins) are clearly separated
- [x] Riskiest assumptions were spiked or are explicitly open
- [x] Open questions answered or deferred
