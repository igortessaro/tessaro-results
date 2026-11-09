# ADR 0004 — Public types live in the `Tessaro` namespace

**Status:** Accepted (2026-11-09) · **Date:** 2026-11-09

## Context

Tessaro.Optional found ([its ADR 0004]) that putting a type in a namespace whose last segment
resembles it makes the compiler resolve the name to the namespace first from inside any
`Tessaro.*` namespace (`CS0234`). The package here is `Tessaro.Results` and its main types will be
`Result`, `Result<T>` and `Error`. A `Tessaro.Results` namespace does not clash with `Result`
(singular) the way `Tessaro.Optional` clashed with `Optional`, but consistency across `Tessaro.*`
packages has value of its own: one `using Tessaro;` brings in every Tessaro type.

[its ADR 0004]: https://github.com/igortessaro/tessaro-optional/blob/main/docs/adr/0004-namespace-tessaro.md

## Decision

- Public types are declared in the namespace **`Tessaro`**. Package ID, assembly name and folders
  stay `Tessaro.Results`. Tests use `Tessaro.Tests`.
- Type names must not collide with other `Tessaro.*` packages sharing the namespace
  (today: `Optional`, `Optional<T>`, `OptionalExtensions`).
- The plan phase runs a **consumer spike** that compiles the public names from an unrelated
  namespace, from a `Tessaro.*` namespace, and from an ASP.NET Core project that also imports
  `Microsoft.AspNetCore.Http` (which has `Results`, `TypedResults` and `IResult`).

## Consequences

**Positive**

- Same `using Tessaro;` as Tessaro.Optional; no aliases or `global::` between sibling packages.

**Negative / trade-offs**

- Namespace and package ID differ; the README must show `using Tessaro;`.
- The shared namespace makes every new type name a cross-package decision.

## Alternatives considered

- **`Tessaro.Results` namespace** — matches the package ID, but breaks consistency with
  Tessaro.Optional and puts `Tessaro.Results` next to ASP.NET Core's `Results` class in reading.
