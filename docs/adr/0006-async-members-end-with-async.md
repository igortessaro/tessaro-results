# ADR 0006 — Members that return a `Task` end with `Async`

**Status:** Accepted (2026-11-09) · **Date:** 2026-11-09

## Context

Spec 002 adds asynchronous composition: `Map`, `Bind` and `Match` on a pending result
(`Task<Result<T>>`) and with asynchronous steps (`Func<T, Task<TOut>>`). Two naming styles are
common in .NET result libraries:

- **Same names, overloaded** (CSharpFunctionalExtensions): `Map` works on `Result<T>` and on
  `Task<Result<T>>`, with sync or async delegates.
- **`Async` suffix** (LanguageExt, and the .NET guideline that task-returning methods end with
  `Async`): `MapAsync`, `BindAsync`, `MatchAsync`.

With one overloaded name, the type a call returns depends on the delegate: `result.Map(q => repository.SaveAsync(q))`
could return a `Result<…>` or a `Task<Result<…>>` depending on which overload wins, and if only
the synchronous `Map(Func<T, TOut>)` applies it silently returns a `Result<Task<Quote>>` — a
result holding an unawaited task.

## Decision

- Every member that returns a `Task` ends with `Async`: `MapAsync`, `BindAsync`, `MatchAsync`,
  whether the receiver is pending (`Task<Result<T>>`), the step is asynchronous, or both.
- Members that return a result synchronously keep the names of spec 001 (`Map`, `Bind`, `Match`)
  and are not overloaded with asynchronous delegates.

## Consequences

**Positive**

- The return type is visible in the name; an `await` is never forgotten silently, because the
  compiler and analyzers (CA2007-style rules, "unawaited task" warnings) see a `Task`.
- A synchronous `Map`/`Bind`/`Match` never accepts an async delegate by accident and returns a
  result holding a task: the async members live under other names.
  *Amended 2026-11-09 (plan 002 spike):* for `Map` this holds only with help — a generic `TOut`
  accepts a `Task`, so `result.Map(o => repository.GetAsync(o.Id))` would compile to a
  `Result<Task<…>>`. Spec 002 AC-3.7 adds `[Obsolete(error: true)]` `Map` overloads for mappers
  returning a `Task`, which turn that call into a compile error pointing to `MapAsync`. `Bind`
  already rejects it (a `Task` is not a result).
  *Amended 2026-11-11 (plan 004 spike):* the same holds for the side-effect steps of spec 004:
  an action returning a `Task` (or an `async` lambda, which would become `async void`) converts to
  the synchronous `OnSuccess`/`OnFailure`'s `Action`. `[Obsolete(error: true)]` overloads (spec 004
  AC-5.1) turn those calls into a compile error pointing to `OnSuccessAsync`/`OnFailureAsync`.
- Follows the .NET naming guideline for asynchronous methods.

**Negative / trade-offs**

- Longer names in fluent chains: `await GetAsync(id).MapAsync(QuoteDto.From)` even when the
  mapper itself is synchronous.
- Two names per operation to learn (sync and async), documented side by side.
- Inside `MapAsync`/`BindAsync`/`MatchAsync`, the overload taking a synchronous delegate and the one
  taking an async delegate still compete for a lambda that returns a `Task`. C#'s
  better-function-member rules should pick the async one (`Task<TOut>` is more specific than
  `TOut`); plan 002 spikes this before approval.

## Alternatives considered

- **Overload `Map`/`Bind`/`Match` for every receiver and delegate** — shortest chains, but the
  same name returns a `Result` or a `Task`, a forgotten `await` is easy to miss, and it departs
  from the .NET guideline.
- **`Async` suffix only when the delegate is asynchronous** (`Map` on a `Task` receiver with a sync
  mapper) — fewer suffixes, but a member returning a `Task` without `Async` breaks the guideline
  and hides the need to `await`.
