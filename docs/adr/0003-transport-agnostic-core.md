# ADR 0003 — The core package is transport-agnostic

**Status:** Accepted (2026-11-09) · **Date:** 2026-11-09

## Context

`Response.cs` lives in each service's **Domain** project but takes a `System.Net.HttpStatusCode`
in every factory and is annotated with `System.Text.Json` attributes, because the same object is
returned as the HTTP body (`new ObjectResult(result) { StatusCode = (int?)result.StatusCode }`).
This has three costs:

- Domain and application code decide HTTP status codes, so a handler reused from a message
  consumer (`MessagesConsumer` in the processing service) carries HTTP semantics it doesn't use.
- Defaults differ per overload (`Fail(string)` → 400, `Fail(Error)` → 500 in `Response<T>` but 400
  in `Response`) — an easy source of wrong status codes.
- The wire format is whatever FluentResults' types serialize to.

## Decision

- `Tessaro.Results` contains only the result and error model. It references no HTTP,
  ASP.NET Core or serialization types and has no status code concept, for failures or successes.
- Presentation concerns — mapping an error category to a status code, choosing `200`/`201`/`204`
  for a success, producing `ProblemDetails` — belong to adapter packages
  (candidate: `Tessaro.Results.AspNetCore`), each specified in its own spec.

## Consequences

**Positive**

- Domain and application layers stay free of transport concerns; the same handler serves HTTP
  and messaging.
- The mapping from category to status code is defined once, in one adapter, instead of at every
  failure site.

**Negative / trade-offs**

- Services that set a status code for a **success** today (e.g. `Created`) must choose it in the
  controller/adapter instead of in the handler.
- Until the adapter package exists, each service writes a small mapping (category → status code).
  The spec documents a recommended mapping so these stay consistent.

## Alternatives considered

- **Keep an optional status code on the result** — fastest migration, but it is exactly the
  coupling this library removes; it would also make the HTTP mapping ambiguous (category or code?).
- **Ship the ASP.NET Core integration in the core package** — forces a framework reference on
  every consumer, including domain projects and workers.
