# Spec 004 — Side-effect steps

**Status:** Implemented (2026-11-11) · **Approved:** 2026-11-11 · **Created:** 2026-11-11
**Retrospective:** [retrospective.md](retrospective.md) (2026-11-11)
**Constitution:** [v1.0.0](../../docs/constitution.md) · **ADRs:** [0001](../../docs/adr/0001-result-of-t-derives-from-result.md), [0002](../../docs/adr/0002-error-model.md), [0006](../../docs/adr/0006-async-members-end-with-async.md)
**Builds on:** [spec 001 — Result core](../001-result-core/spec.md) and [spec 002 — Async composition](../002-async-composition/spec.md) (both Implemented 2026-11-09)
*Amended 2026-11-11 with plan 004 approval: AC-5.3 added (plan resolved question 1).*

> This spec defines **what** is offered and **why**. Signatures, file layout and tooling belong
> in `plan.md`.

---

## 1. Problem

The services `Tessaro.Results` replaces do something *next to* the main flow once an outcome is
known — publish a notification after a success, log a warning after a failure — and then carry
on with the same outcome:

- `fiap-soat-oficina-mecanica`, `UpdateQuoteStatusHandler`: after updating the quote it awaits
  `mediator.Publish(new UpdateQuoteStatusNotification(…))` and then returns the updated quote.
- `fiap-soat-hackaton-processing-service`, `MessagesConsumer`: after handling a message it logs
  the errors when the response is not a success, then acks the message either way.

Specs 001 and 002 have no step whose job is a side effect. Today the chain either breaks back
into `if (result.IsFailure) …` blocks, or abuses a transformation and rebuilds the result:

```csharp
// Publish after success, with spec 002 only: the lambda re-wraps a value it did not change.
.BindAsync(async quote =>
{
    await mediator.Publish(new QuoteStatusChanged(quote), ct);
    return Result.Ok(quote);
})
```

That hides the intent (nothing is being bound), repeats the re-wrap in every handler, and makes
it easy to forget the `await` and publish fire-and-forget.

## 2. Goal

Add steps that run a side effect on one outcome — success or failure — and pass the **same
result** on unchanged, synchronously and asynchronously, on completed and pending results, with
the semantics of specs 001 and 002: delegates validated eagerly, other-branch delegates never
called, exceptions and cancellation never swallowed or turned into failures.

**Success looks like:**

```csharp
// Handler: publish after a success, log after a failure, return the outcome unchanged.
public Task<Result<Quote>> Handle(UpdateQuoteStatus request, CancellationToken ct) =>
    repository.FindAsync(request.Id, ct)                                  // Task<Result<Quote>>
        .BindAsync(quote => quote.ChangeStatus(request.Status))
        .BindAsync(quote => repository.UpdateAsync(quote, ct))
        .OnSuccessAsync(quote => mediator.Publish(new QuoteStatusChanged(quote), ct)) // awaited
        .OnFailureAsync(errors => logger.LogWarning("Quote {Id} not updated: {Errors}", request.Id, errors));

// Worker: log a failed outcome, then ack either way.
Result outcome = await mediator.Send(command, ct);
outcome.OnFailure(errors => logger.LogWarning("Message {Tag} had errors: {Errors}", tag, errors));
channel.BasicAck(tag, multiple: false);
```

## 3. Users

- **Service developer** — publishes events or audits after a successful step, inside the chain.
- **API/worker developer** — logs or meters a failed outcome before turning it into a response or an ack.

---

## 4. User stories and acceptance criteria

Notation as in specs 001 and 002 (*success*, *failure*, *`Result`*, *`Result<T>`*, *pending
result*, *async step*). Also: *side effect* = a delegate whose return value, if any, is not a
result and is not kept (an *async side effect* returns a `Task`); *the same instance* = the very
result object the step was called on (or that the pending result completed with), not a copy.
"Throws" has the meaning of spec 002: raised **when the member is called**, before any delegate
runs and before the pending result is awaited — unless the row says "awaiting throws".

Each story applies to both shapes (`Result` and `Result<T>`) unless it names one. On `Result<T>`
the success side effect receives the value; on `Result` it receives nothing. The failure side
effect always receives **all** the errors, in order.

### US-1 — Run a side effect after a success

*As a service developer, I want to run an action when a step succeeded and keep the chain going
with the same result, so "publish after success" reads as one chain.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-1.1 | a success of `Result<T>` with `v` | `OnSuccess(a)` | `a` is called once with `v`; returns the same instance |
| AC-1.2 | a success of `Result` | `OnSuccess(a)` with an `a` taking no value | `a` is called once; returns the same instance |
| AC-1.3 | a failure (either shape) | `OnSuccess(a)` | `a` is not called; returns the same instance, with all the same errors |
| AC-1.4 | a `Result<T>` | `OnSuccess(a)` followed by `Map(f)` | the chain compiles with `f` taking a `T` (the step keeps the value type), and `f` receives `v` |
| AC-1.5 | a chain mixing `OnSuccess`, `OnFailure`, `Map` and `Bind` | run it | every step runs at most once, in the order written |

### US-2 — Run a side effect after a failure

*As an API/worker developer, I want to run an action when a result failed, with every error, and
keep the same result, so logging a failure doesn't need an `if` or a `Match` that rebuilds it.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-2.1 | a failure (either shape) with errors `e1, e2` | `OnFailure(a)` | `a` is called once with `[e1, e2]`, in order; returns the same instance |
| AC-2.2 | a success (either shape) | `OnFailure(a)` | `a` is not called; returns the same instance |
| AC-2.3 | a failed `Result<T>` | `OnFailure(a)` followed by `Map(f)` | the chain compiles with `f` taking a `T`, and returns a failure with the same errors; `f` is not called |

### US-3 — Side effects on pending results and asynchronous side effects

*As a service developer, I want the same steps on a pending result and with asynchronous
actions, and I want them awaited, so an awaited publish is never fire-and-forget.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-3.1 | a success (completed or pending) with `v` | `OnSuccessAsync(a)` with an async `a` | `a` is called once with `v`; completes with the same instance |
| AC-3.2 | a pending success with `v` | `OnSuccessAsync(a)` with a synchronous `a` | `a` is called once with `v`; completes with the same instance |
| AC-3.3 | a failure (completed or pending) with errors `e1, e2` | `OnFailureAsync(a)` with an async `a` (or a synchronous one, on a pending failure) | `a` is called once with `[e1, e2]`; completes with the same instance |
| AC-3.4 | a failure for `OnSuccessAsync`, or a success for `OnFailureAsync` (completed or pending) | call it with any `a` | `a` is not called; completes with the same instance |
| AC-3.5 | a result without a value (completed or pending) | the same members with an `a` taking no value | same behaviour as AC-3.1 – AC-3.4 |
| AC-3.6 | an async side effect whose task has not completed yet | the step's returned task | does not complete before the side effect's task completes (the side effect is awaited) |
| AC-3.7 | a pending `Result<T>` | `OnSuccessAsync(a)` followed by `MapAsync(f)` | the chain compiles with `f` taking a `T`, and `f` receives `v` |

### US-4 — Fail fast and propagate

*As a service developer, I want invalid calls to fail immediately and a failing side effect to
reach me unchanged, so a broken publish is never mistaken for a business failure or hidden.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-4.1 | a `null` delegate | call any member of this spec, on a success or a failure, completed or pending | throws `ArgumentNullException` immediately (the pending result is not awaited) |
| AC-4.2 | a `null` pending result (`Task` reference) | call any async member of this spec on it | throws `ArgumentNullException` immediately |
| AC-4.3 | a pending result that completes with a `null` result | await an async member of this spec on it | awaiting throws `InvalidOperationException` |
| AC-4.4 | a result on the branch the side effect runs on | the async side effect returns a `null` task | awaiting throws `InvalidOperationException` |
| AC-4.5 | a side effect that throws `x`, an async side effect that faults with `x`, or a pending result that faults with `x` | call (sync) or await (async) the member | throws `x` itself (not wrapped, not turned into a failure); later steps in the chain don't run |
| AC-4.6 | a pending result or an async side effect that is cancelled | await the member | throws `OperationCanceledException` (cancellation is not turned into a failure) |

### US-5 — An asynchronous action can't be dropped by accident

*As a service developer, I want the compiler to stop me from passing an asynchronous action to a
synchronous step, or an operation that can fail to a side-effect step, so `OnSuccess(q => mediator.Publish(…))`
never compiles into fire-and-forget and a failed update is never ignored.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-5.1 | a result (either shape) | its synchronous `OnSuccess` or `OnFailure` receives a delegate that returns a `Task` (method group, expression lambda or `async` lambda) | the code does not compile, and the message points to `OnSuccessAsync` / `OnFailureAsync` (the task would otherwise be discarded, or the lambda become `async void`) |
| AC-5.2 | a pending result | `OnSuccessAsync` or `OnFailureAsync` receives a lambda that returns a `Task` | the task is awaited (AC-3.6), not discarded as if the lambda were synchronous |
| AC-5.3 | a result (either shape, completed or pending) | a success step (`OnSuccess`, `OnSuccessAsync`) receives an action that returns a result, or a task of one | the code does not compile, and the message points to `Bind` / `BindAsync` (the action's errors would otherwise be lost) |

---

## 5. Consistency with specs 001 and 002

There is no `Response.cs` counterpart to deviate from (the services write these steps by hand,
§1). These are deliberate rules: the new steps mirror the existing ones.

| Existing behaviour | Side-effect steps |
|---|---|
| `Map` / `Bind` / `Match` and their `Async` forms ([ADR 0006](../../docs/adr/0006-async-members-end-with-async.md)) | `OnSuccess` / `OnFailure`, and `OnSuccessAsync` / `OnFailureAsync` for every member that returns a `Task` |
| A failure passes every error on; the other branch's delegate doesn't run | Same (AC-1.3, AC-2.1, AC-2.2, AC-3.4), and the result itself is passed on, not rebuilt |
| `Match`'s `onFailure` receives all the errors | The failure side effect receives all the errors, in order (AC-2.1, AC-3.3) |
| `null` delegates throw on success and failure, before awaiting | Same (AC-4.1, AC-4.2) |
| A `null` task or a pending `null` result throws `InvalidOperationException` | Same (AC-4.3, AC-4.4) |
| Exceptions propagate unwrapped; cancellation stays cancellation | Same (AC-4.5, AC-4.6) |
| Synchronous `Map` refuses a `Task`-returning mapper at compile time (spec 002 AC-3.7) | Synchronous `OnSuccess`/`OnFailure` refuse a `Task`-returning action (AC-5.1); success steps refuse a result-returning action (AC-5.3) |
| On a completed result, `BindAsync` takes only async steps | Same for `OnSuccessAsync`/`OnFailureAsync` (resolved question 4) |
| `Result<T>` is a `Result` (ADR 0001) | On `Result<T>` the steps keep the value type (AC-1.4, AC-2.3, AC-3.7) |

## 6. Out of scope (future specs)

- **A side effect on either outcome** (`Tap(result => …)`, `OnBoth`) — for "log every outcome";
  call `OnSuccess` and `OnFailure`, or `Match`. Candidate for a later spec if a service needs it.
- **A side effect that can itself fail the chain** (a publish returning a `Result`, keeping the
  original value on success — `Check`/`BindTap` elsewhere) — use `Bind`/`BindAsync` today.
  Candidate spec, together with `Ensure`.
- **Side effects filtered by error type or code** (only on `NotFound`, …).
- `Ensure`, `MapErrors`, value fallbacks (`ValueOr`), combining several results.
- `ValueTask` receivers or actions; `CancellationToken` parameters (actions capture the caller's
  token, as in §2); turning exceptions into failures (`Try`).

## 7. Non-functional requirements

- **NFR-1** — Zero runtime dependencies (unchanged).
- **NFR-2** — The async members don't resume on the caller's synchronization context (as spec 002 NFR-2).
- **NFR-3** — The new members compile and run from an unrelated namespace and from a `Tessaro.*`
  namespace next to ASP.NET Core and `Tessaro.Optional` (the compatibility checks grow to cover them);
  the §2 example compiles and runs there.
- **NFR-4** — Adding the new members doesn't change which overload any existing call shape of
  specs 001 and 002 binds to (no new ambiguity, as found in spec 002 T01).

## 8. Resolved questions (2026-11-11, owner approved the recommendations)

1. **Two names only: `OnSuccess` and `OnFailure`**, plus `OnSuccessAsync`/`OnFailureAsync` for
   every member that returns a `Task` ([ADR 0006](../../docs/adr/0006-async-members-end-with-async.md)).
   No `Tap` alias (DRY, YAGNI). They mirror `IsSuccess`/`IsFailure` and `Match`'s parameters; the
   README states that they never transform (older CSharpFunctionalExtensions used `OnSuccess` for
   what is now `Map`/`Bind`).
2. **The steps pass on the same instance**, not a copy (AC-1.1 et al.).
3. **The synchronous steps refuse `Task`-returning actions at compile time** (US-5), with the
   `[Obsolete(error: true)]` approach of spec 002 AC-3.7, spiked in the plan. No new ADR; the plan
   amends ADR 0006's consequence note.
4. **On a completed result, `OnSuccessAsync`/`OnFailureAsync` take only async actions**, as
   `BindAsync` does; use `OnSuccess`/`OnFailure` for a synchronous action.
5. **One failure-action shape**, receiving all the errors (like `Match`'s `onFailure`).

## 9. Review checklist

- [x] The services' side-effect patterns (publish after success, log after failure) are covered or listed in §6
- [x] Every acceptance criterion is testable as written
- [x] Nothing in this spec prescribes implementation details
- [x] Out-of-scope items are acceptable for this release
- [x] Open questions answered or deferred
