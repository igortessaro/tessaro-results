# ADR 0001 — `Result<T>` derives from `Result`

**Status:** Accepted (2026-11-09) · **Date:** 2026-11-09

## Context

In the `Response.cs` being replaced, `Response` and `Response<T>` are unrelated sealed classes.
The only bridge is a user-defined implicit conversion from `Response<T>` to `Response`, which
**re-creates** the object (through `ResponseFactory`) and flattens every error into a string
message. In practice:

- A method declared to return `Response` cannot return a `Response<T>` it got from another call
  without building a new object.
- A collection or a helper typed on `Response` cannot hold or accept a `Response<T>`.
- Conversions don't apply to generics, `is`/`as`, or interfaces, so the gap shows up in pipelines
  and decorators (e.g. MediatR behaviours) as duplicated code.

Tessaro.Optional made `Optional<T>` a `readonly struct` ([its ADR 0001]) to avoid a null optional
and allocations. A struct cannot take part in an inheritance hierarchy, so the two goals conflict
here.

[its ADR 0001]: https://github.com/igortessaro/tessaro-optional/blob/main/docs/adr/0001-optional-as-readonly-struct.md

## Decision

- `Result` is a class representing an outcome without a value; `Result<T>` derives from it and
  adds the value. A `Result<T>` **is a** `Result` (constitution V).
- Both are immutable. The hierarchy is **closed**: constructors are not public, so no type
  outside the library can derive from them (`Result<T>` is `sealed`; `Result` only has
  non-public constructors).
- A result variable can be `null` like any reference; the library never returns `null` and
  rejects `null` results passed to it (constitution II).

## Consequences

**Positive**

- `Result Delete(...) => repository.Remove(...)` works when `Remove` returns `Result<Order>`.
  Pipelines, decorators and helpers are written once against `Result`.
- Failure propagation keeps the original errors untouched; no conversion step can lose them.
- The type model matches what the services already do (classes), so migration is mechanical.

**Negative / trade-offs**

- One heap allocation per result. Acceptable: results sit at use-case boundaries next to I/O,
  not in tight loops. The plan may cache the common success-without-value instance.
- A `Result` reference can be `null`, unlike `Optional<T>`. Mitigated by nullable reference types
  and eager argument validation.
- Code holding a `Result` that is really a `Result<T>` needs a type test to reach the value.
  That is the intended trade: code that needs the value asks for `Result<T>`.

## Alternatives considered

- **Two unrelated `readonly struct`s with implicit conversions** — no allocation, but reproduces
  the exact problem this library exists to fix.
- **`Result<T>` only, with `Result<Unit>` for "no value"** — a single type, but `Unit` is foreign
  to most .NET developers and every non-value call site carries noise.
- **A shared `IResult` interface** — conflicts with ASP.NET Core's `Microsoft.AspNetCore.Http.IResult`,
  boxes structs, and still needs a concrete type hierarchy behind it.
