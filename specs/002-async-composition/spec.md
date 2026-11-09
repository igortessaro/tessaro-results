# Spec 002 — Async composition

**Status:** Implemented (2026-11-09) · **Approved:** 2026-11-09 · **Created:** 2026-11-09
**Retrospective:** [retrospective.md](retrospective.md) (2026-11-09)
**Constitution:** [v1.0.0](../../docs/constitution.md) · **ADRs:** [0001](../../docs/adr/0001-result-of-t-derives-from-result.md), [0002](../../docs/adr/0002-error-model.md), [0004](../../docs/adr/0004-namespace-tessaro.md), [0006](../../docs/adr/0006-async-members-end-with-async.md)
*Amended 2026-11-09 with plan 002 approval: AC-3.7 added (plan resolved question 1).*
**Builds on:** [spec 001 — Result core](../001-result-core/spec.md) (Implemented 2026-11-09)

> This spec defines **what** is offered and **why**. Signatures, file layout and tooling belong
> in `plan.md`.

---

## 1. Problem

The code `Tessaro.Results` replaces is asynchronous end to end. In `fiap-soat-oficina-mecanica`
all 25 use-case handlers return `Task<Response…>` and await repositories; controllers await the
mediator and then map the outcome. With spec 001 alone:

- every step on a pending result needs parentheses around an `await`:
  `(await handler.Handle(request)).Map(QuoteDto.From)`;
- an asynchronous step can't be chained at all: `Bind(q => repository.UpdateAsync(q))` produces
  a `Result` holding a `Task`, or the chain breaks back into `if (result.IsFailure) return …;`
  blocks — the boilerplate spec 001 set out to remove.

## 2. Goal

Let pending results and asynchronous steps compose with the same semantics as spec 001 —
failures pass every error on, delegates are validated eagerly, nothing is swallowed — so an
async handler or controller reads as one expression.

**Success looks like:**

```csharp
// Handler: async steps in one chain.
public Task<Result<ServiceOrder>> Handle(UpdateStatus request, CancellationToken ct) =>
    repository.FindAsync(request.Id, ct)                              // Task<Result<ServiceOrder>>
        .BindAsync(order => order.ChangeStatus(request.Status))      // sync step on a pending result
        .BindAsync(order => repository.UpdateAsync(order, ct))       // async step
        .MapAsync(order => repository.GetDetailedAsync(order.Id, ct)); // async mapper

// Controller: map and branch on a pending result.
public async Task<IResult> Update(Guid id, UpdateStatusRequest body, CancellationToken ct) =>
    await mediator.Send(body.ToCommand(id), ct)                      // Task<Result<ServiceOrder>>
        .MapAsync(ServiceOrderDto.From)
        .MatchAsync(dto => TypedResults.Ok(dto), errors => errors.ToProblem()); // ToProblem: your mapping (spec 001 §5)
```

## 3. Users

- **Service developer** — writes async handlers that chain repository and domain calls.
- **API/worker developer** — awaits a pending result and turns it into a response or a message ack/nack.

---

## 4. User stories and acceptance criteria

Notation as in spec 001 (*success*, *failure*, *`Result`*, *`Result<T>`*). Also:
*pending result* = a `Task` that completes with a `Result` or `Result<T>`;
*async step* = a delegate that returns a `Task` (of a value, a result, or a branch outcome).
"Throws" means the exception is raised **when the member is called**, before any delegate runs
and before the pending result is awaited — not later, from the returned task.

Each story applies to both shapes (`Result` and `Result<T>`) unless it names one.

### US-1 — Continue a pending result with ordinary steps

*As an API/worker developer, I want to map, bind and branch on a pending result without
wrapping it in parentheses, so an awaited call reads as one chain.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-1.1 | a pending success with `v` | `MapAsync(f)` with a synchronous `f` | completes with a success holding `f(v)` |
| AC-1.2 | a pending success with `v` | `BindAsync(f)` with a synchronous `f` returning a result | completes with the result of `f(v)` |
| AC-1.3 | a pending success with `v` | `MatchAsync(onSuccess, onFailure)` with synchronous branches | completes with `onSuccess(v)`; `onFailure` is not called |
| AC-1.4 | a pending failure | `MapAsync`, `BindAsync` or `MatchAsync` with synchronous delegates | completes with a failure with **all** the same errors, in order (for `MatchAsync`: with `onFailure(errors)`); the other delegates are not called |
| AC-1.5 | a pending result without a value | the same members with delegates taking no value | same behaviour as AC-1.1 – AC-1.4 |

### US-2 — Chain asynchronous steps

*As a service developer, I want to chain steps that are themselves asynchronous, on a completed
or a pending result, so repository calls fit in the chain.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-2.1 | a success (completed or pending) with `v` | `MapAsync(f)` with an async `f` | completes with a success holding the value `f(v)` completes with |
| AC-2.2 | a success (completed or pending) with `v` | `BindAsync(f)` with an async `f` returning a result | completes with the result `f(v)` completes with |
| AC-2.3 | a success (completed or pending) with `v` | `MatchAsync` with async branches | completes with what `onSuccess(v)` completes with; `onFailure` is not called |
| AC-2.4 | a failure (completed or pending) | `MapAsync`, `BindAsync` or `MatchAsync` with async delegates | completes with a failure with all the same errors (for `MatchAsync`: with what `onFailure(errors)` completes with); the other delegates are not called |
| AC-2.5 | a result without a value (completed or pending) | the same members with async delegates taking no value | same behaviour as AC-2.1 – AC-2.4 |
| AC-2.6 | a chain of `BindAsync` steps where step *k* fails | await the chain | completes with step *k*'s errors; steps after *k* are not called |

### US-3 — Fail fast and propagate, asynchronously

*As a service developer, I want invalid calls to fail immediately and real exceptions to reach
me unchanged, so async code is as predictable as sync code.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-3.1 | a `null` delegate | call any async member, on a success or a failure, completed or pending | throws `ArgumentNullException` immediately (the pending result is not awaited) |
| AC-3.2 | a `null` pending result (`Task` reference) | call any async member on it | throws `ArgumentNullException` immediately |
| AC-3.3 | a pending result that completes with a `null` result | await an async member on it | throws `InvalidOperationException` |
| AC-3.4 | a success | an async step returns a `null` task, or a task completing with `null` (value or result) | awaiting throws `InvalidOperationException` (a success never holds `null`) |
| AC-3.5 | a pending result or a step that faults with exception `x` | await the member | throws `x` itself (not wrapped, not turned into a failure) |
| AC-3.6 | a pending result or a step that is cancelled | await the member | throws `OperationCanceledException` (cancellation is not turned into a failure) |
| AC-3.7 | a result (either shape) | its synchronous `Map` receives a mapper that returns a `Task` | the code does not compile, and the message points to `MapAsync` (the result would otherwise hold an unawaited task) |

---

## 5. Consistency with spec 001

These are deliberate rules, not deviations: the async members mirror the synchronous ones.

| Spec 001 (synchronous) | Spec 002 (asynchronous) |
|---|---|
| `Map` / `Bind` / `Match` | `MapAsync` / `BindAsync` / `MatchAsync` ([ADR 0006](../../docs/adr/0006-async-members-end-with-async.md)) |
| A failure passes every error on; delegates of the other branch don't run | Same (AC-1.4, AC-2.4, AC-2.6) |
| `null` delegates throw `ArgumentNullException` on success and failure | Same, and **before** awaiting (AC-3.1) |
| A mapper returning `null` throws `InvalidOperationException` | Same for a `null` task or a task completing with `null` (AC-3.4) |
| Exceptions thrown by delegates propagate | Same, unwrapped; cancellation stays cancellation (AC-3.5, AC-3.6) |
| No members that return a `Task` | All new members return a `Task` and end with `Async` |

## 6. Out of scope (future specs)

- **Side-effect steps** (`Tap`/`OnSuccess`/`OnFailure`, sync and async) — the handlers' "log and
  publish after success" pattern; candidate spec 004, once the release (spec 003) is out.
- `ValueTask` receivers or steps; `IAsyncEnumerable`.
- `CancellationToken` parameters on the members (steps capture the caller's token, as in §2).
- Turning exceptions into failures (`Try`/`TryAsync`).
- Combining several pending results (`WhenAll`-style, collecting errors).

## 7. Non-functional requirements

- **NFR-1** — Zero runtime dependencies (unchanged).
- **NFR-2** — The async members don't resume on the caller's synchronization context (library
  code), so they can't deadlock a caller that blocks on them.
- **NFR-3** — The new members compile and run from an unrelated namespace and from a `Tessaro.*`
  namespace next to ASP.NET Core and `Tessaro.Optional` (the spec 001 compatibility checks grow
  to cover them).

## 8. Resolved questions (2026-11-09, owner approved the recommendations)

1. **Every task-returning member ends with `Async`** — [ADR 0006](../../docs/adr/0006-async-members-end-with-async.md)
   accepted. Which overload a lambda returning a `Task` binds to inside `MapAsync`/`BindAsync` is
   spiked in the plan; AC-2.1 tests the outcome.
2. **Async steps apply to completed and pending results** (US-2 as written).
3. **`MatchAsync` with async branches is included** (AC-2.3), for workers that ack/nack asynchronously.
4. **Arguments are validated before awaiting** (AC-3.1, AC-3.2); the plan shows how.
5. **Side-effect steps (`Tap`) stay out**; candidate spec 004, after the release (spec 003).

## 9. Review checklist

- [x] The async patterns of the services (handlers awaiting repositories, controllers awaiting the mediator) are covered or listed in §6
- [x] Every acceptance criterion is testable as written
- [x] Nothing in this spec prescribes implementation details
- [x] Out-of-scope items are acceptable for this release
- [x] Open questions answered or deferred
