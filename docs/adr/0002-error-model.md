# ADR 0002 — A failure carries a non-empty list of `Error` values

**Status:** Accepted (2026-11-09) · **Date:** 2026-11-09

## Context

`Response.cs` stores FluentResults `Reasons` (`List<IReason>`): a mutable list that mixes errors
and successes, exposes FluentResults types in the public contract, and is serialized straight to
the HTTP body. What the services actually do with it:

- Create failures from a **single message** (`Fail("Quote not found", HttpStatusCode.NotFound)`),
  an `Error` or a list of errors.
- Read the messages to log them (`string.Join(" | ", response.Reasons.Select(r => r.Message))`) or
  return them in a response body.
- Pick the HTTP status code **at the place where the failure is created**, because the error
  itself does not say what kind of failure it is.
- When mapping a failure (`ResponseMapper.Map`), keep only the **first** message.

So the requirements are: several errors per failure (validation produces many), a message for
humans, a stable identifier for machines, and a category an adapter can map to a status code
without the domain knowing about HTTP ([ADR 0003](0003-transport-agnostic-core.md)).

## Decision

- An `Error` is an immutable value with:
  - **`Code`** — a stable, machine-readable identifier (e.g. `"order.not_found"`);
  - **`Message`** — a human-readable description;
  - **`Type`** — a category from a small, closed set (`Failure`, `Validation`, `NotFound`,
    `Conflict`, `Unauthorized`, `Forbidden`, `Unexpected`), with `Failure` as the general default.
- Factories per category keep creation short (`Error.NotFound("order.not_found", "Order 42 was not found")`).
- A failed result holds a **read-only, non-empty** list of errors. Creating a failure from one
  message, one error or several errors is supported; an empty list or a `null` element throws.
- A successful result holds no errors and no "success reasons".
- Every operation that propagates a failure keeps **all** of its errors (constitution III).

## Consequences

**Positive**

- No dependency on FluentResults; the public contract is the library's own small type.
- The category replaces the `HttpStatusCode` argument: the domain says *what* failed, an adapter
  decides *how* to present it.
- Codes make errors testable and translatable without matching on message text.

**Negative / trade-offs**

- A closed set of categories may not fit every domain. Custom meaning goes in `Code`; a new
  category is a new minor version.
- Services migrating from `Fail("message", HttpStatusCode.X)` must choose a code and a category
  per call site (a mechanical but non-zero change).
- Errors carry no extra data (e.g. the field name of a validation error). Deferred until a spec
  needs it — see resolved question 2.

## Resolved questions (2026-11-09, owner approved the recommendations)

1. **`Code` is required** in the `Error` factories, so errors are always identifiable. A
   message-only shortcut on the result (`Result.Fail("message")`) uses a generic code and the
   `Failure` category, keeping today's one-liner call sites cheap.
2. **No metadata in spec 001.** A key/value bag (e.g. the field name of a validation error) is
   added with the ASP.NET Core adapter spec, when `ProblemDetails.errors` needs it.
3. **`Error` is sealed in spec 001**; extensible error types are revisited only with a real use case.

## Alternatives considered

- **`Result<T, TError>` with a consumer-defined error type** (Rust style) — most flexible, but
  every signature carries a second type argument, adapters cannot map errors they don't know,
  and it does not match how the services use errors today.
- **A single error per failure** — simpler, but validation needs several and the current code
  already returns lists.
- **Keep FluentResults `IError`/`IReason`** — violates constitution VI and keeps the mixed
  success/error list.
- **A status code inside `Error`** — violates constitution IV.
