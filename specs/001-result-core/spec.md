# Spec 001 — Result core

**Status:** Implemented (2026-11-09) · **Approved:** 2026-11-09 · **Created:** 2026-11-09
**Retrospective:** [retrospective.md](retrospective.md) (2026-11-09)
**Constitution:** [v1.0.0](../../docs/constitution.md) · **ADRs:** [0001](../../docs/adr/0001-result-of-t-derives-from-result.md), [0002](../../docs/adr/0002-error-model.md), [0003](../../docs/adr/0003-transport-agnostic-core.md), [0004](../../docs/adr/0004-namespace-tessaro.md)

> This spec defines **what** the first release offers and **why**. Signatures, target frameworks
> and tooling belong in `plan.md`.

---

## 1. Problem

Four FIAP-SOAT-Net services (`fiap-soat-oficina-mecanica`, `smw-service-order`,
`fiap-soat-hackaton-file-service`, `fiap-soat-hackaton-processing-service`) each carry a copy of
`Response.cs` — `Response`, `Response<T>` and `ResponseFactory` wrapping FluentResults — plus a
copied `ResponseMapper`. The copies work, but:

- `Response<T>` is not a `Response`: returning one where the other is expected re-creates the
  object through an implicit conversion that flattens errors to strings.
- The domain picks HTTP status codes (`HttpStatusCode` in every factory), with inconsistent
  defaults (`Fail(Error)` is 500 on `Response<T>` and 400 on `Response`).
- `Data` returns `default(T)` on a failure, so a forgotten `IsSuccess` check passes `null` along.
- Mapping a failure (`ResponseMapper.Map`) keeps only the **first** error message.
- Every service depends on FluentResults and maintains its own copy.

## 2. Goal

Ship `Tessaro.Results`: one small, dependency-free result model — success with or without a
value, failure with one or more categorised errors — that covers what the services do with
`Response.cs` today and removes the problems above.

**Success looks like:**

```csharp
// Application layer: no HTTP, no FluentResults.
public async Task<Result<Quote>> Handle(UpdateQuoteStatus request)
{
    Quote? quote = await repository.FindAsync(request.Id);
    if (quote is null)
        return Error.NotFound("quote.not_found", $"Quote {request.Id} was not found");

    return quote.ChangeStatus(request.Status);   // Result<Quote>
}

// A Result<T> is a Result: no re-creation, no lost errors.
public async Task<Result> Delete(Guid id) => await repository.RemoveAsync(id); // returns Result<Quote>

// Mapping keeps every error; branching needs no `if (!IsSuccess)`.
Result<QuoteDto> dto = result.Map(QuoteDto.From);
string log = dto.Match(
    onSuccess: q => $"quote {q.Id} updated",
    onFailure: errors => string.Join(" | ", errors.Select(e => e.Message)));
```

## 3. Users

- **Service developer** — writes handlers and use cases that return outcomes instead of throwing.
- **API/worker developer** — turns an outcome into an HTTP response, a log line or a message ack.
- **Maintainer migrating a FIAP-SOAT-Net service** — replaces `Response.cs` with a checklist (§5).

---

## 4. User stories and acceptance criteria

Notation: *success* = result without errors; *failure* = result with at least one error;
*`Result`* = without a value; *`Result<T>`* = with a value of type `T`.
Unless stated otherwise, "throws" means the exception is raised immediately, before any delegate
runs, and the result is not created.

### US-1 — Create successes

*As a service developer, I want to return a success with or without a value, so the caller knows the operation worked.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-1.1 | — | create a success without a value | `IsSuccess` is true, `IsFailure` is false, `Errors` is empty |
| AC-1.2 | a non-null value `v` | create a success with `v` | the result is a success whose `Value` is `v` |
| AC-1.3 | `null` as the value | create a success with it | throws `ArgumentNullException` |
| AC-1.4 | a non-null value `v` | return `v` from a method declared to return `Result<T>` | the caller receives a success whose `Value` is `v` (implicit conversion) |

### US-2 — Create failures

*As a service developer, I want to return a failure with one or more errors, so the caller knows why the operation failed.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-2.1 | an error `e` | create a failure (with or without a value type) from `e` | `IsFailure` is true and `Errors` is exactly `[e]` |
| AC-2.2 | errors `e1, e2` | create a failure from them | `Errors` is `[e1, e2]`, in order |
| AC-2.3 | a message `m` | create a failure from `m` alone | `Errors` has one error with message `m`, type `Failure` and code `general.failure` (resolved question 2) |
| AC-2.4 | an empty list of errors | create a failure from it | throws `ArgumentException` |
| AC-2.5 | `null` (error, list, or a list containing `null`) | create a failure from it | throws `ArgumentNullException` |
| AC-2.6 | `null`, empty or whitespace message | create a failure from it | throws `ArgumentException` (`ArgumentNullException` for `null`) |
| AC-2.7 | an error `e` | return `e` from a method declared to return `Result` or `Result<T>` | the caller receives a failure whose `Errors` is `[e]` (implicit conversion) |
| AC-2.8 | a failure created from a caller's list | the caller later modifies that list | the failure's `Errors` is unchanged |
| AC-2.9 | any failure | try to modify `Errors` | not possible through the public API (read-only) |

### US-3 — Describe errors

*As a service developer, I want errors that say what went wrong in a machine- and human-readable way, without HTTP.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-3.1 | a code `c` and message `m` | create an error of each category (`Failure`, `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Unexpected`) | the error has `Code` `c`, `Message` `m` and that `Type` |
| AC-3.2 | `null`, empty or whitespace code or message | create an error | throws `ArgumentException` (`ArgumentNullException` for `null`) |
| AC-3.3 | two errors with the same code, message and type | compare them | they are equal and have the same hash code |
| AC-3.4 | two errors differing in code, message or type | compare them | they are not equal |
| AC-3.5 | an error | call `ToString()` | the text contains its type, code and message |

### US-4 — Inspect a result

*As an API/worker developer, I want to read a result's state, so I can decide what to do with it.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-4.1 | a success with value `v` | read `Value` | returns `v` |
| AC-4.2 | a failure of `Result<T>` | read `Value` | throws `InvalidOperationException` whose message lists the errors |
| AC-4.3 | a success | read `Errors` | empty, never `null` |
| AC-4.4 | a success / a failure | call `ToString()` | describes the state and the value or every error (for logs) |

### US-5 — A result with a value is a result *(fixes `Response<T>` → `Response`)*

*As a service developer, I want to pass a `Result<T>` wherever a `Result` is expected, so I never rebuild results.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-5.1 | a `Result<T>` (success or failure) | use it where a `Result` is expected (return, argument, collection) | it compiles without conversion, and it is the same instance |
| AC-5.2 | a failed `Result<T>` used as `Result` | read `IsFailure` and `Errors` | the same state and the same errors, in order |
| AC-5.3 | a failed `Result` (without a value) | turn it into a failed `Result<U>` for any `U` | the new result is a failure with the same errors |
| AC-5.4 | a successful `Result` | try to turn it into a `Result<U>` that way | throws `InvalidOperationException` (a success has no value to give) |
| AC-5.5 | — | try to create another kind of result outside the library (subclass) | not possible (closed hierarchy, ADR 0001) |

### US-6 — Transform and chain without `if`

*As a service developer, I want to transform a value and chain operations that can fail, so failures propagate on their own.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-6.1 | a success with `v` | `Map(f)` | a success with `f(v)` |
| AC-6.2 | a failure | `Map(f)` | a failure with **all** the same errors, in order; `f` is not called |
| AC-6.3 | a success with `v` | `Bind(f)` where `f` returns a result | the result of `f(v)` (success or failure) |
| AC-6.4 | a failure | `Bind(f)` | a failure with all the same errors; `f` is not called |
| AC-6.5 | a success / failure without a value | `Bind(f)` with an `f` taking no value (returning `Result` or `Result<U>`) | the result of `f()` / the same errors without calling `f` |
| AC-6.6 | a success / failure without a value | `Map(f)` with an `f` taking no value and returning `U` | a success with `f()` / the same errors without calling `f` |
| AC-6.7 | a `null` delegate | `Map` or `Bind` on a success **or** a failure | throws `ArgumentNullException` |
| AC-6.8 | a success | `Map(f)` where `f` returns `null` | throws `InvalidOperationException` (a success never holds `null`) |
| AC-6.9 | a success | `Bind(f)` where `f` returns a `null` result | throws `InvalidOperationException` |

### US-7 — Branch on the outcome

*As an API/worker developer, I want one expression that handles both outcomes, so mapping to a response or a log line needs no `if`.*

| ID | Given | When | Then |
|---|---|---|---|
| AC-7.1 | a success with `v` | `Match(onSuccess, onFailure)` | returns `onSuccess(v)`; `onFailure` is not called |
| AC-7.2 | a failure | `Match(onSuccess, onFailure)` | returns `onFailure(errors)` with all errors; `onSuccess` is not called |
| AC-7.3 | a result without a value | `Match` with an `onSuccess` taking no value | same behaviour as AC-7.1 / AC-7.2 |
| AC-7.4 | a `null` delegate | `Match` on a success **or** a failure | throws `ArgumentNullException` |

---

## 5. Deliberate deviations from `Response.cs`

This table is the migration checklist for a service.

| `Response.cs` | `Tessaro.Results` | Why |
|---|---|---|
| `Response<T>` and `Response` are unrelated; implicit conversion re-creates the object | `Result<T>` **is a** `Result` (AC-5.1) | ADR 0001 |
| Every factory takes an `HttpStatusCode` (also for successes, e.g. `Created`) | No status code; errors have a category, the presentation layer picks the status | ADR 0003; see recommended mapping below |
| `Reasons`: mutable `List<IReason>` of FluentResults types | `Errors`: read-only, library-owned `Error` values; empty on success, non-empty on failure | ADR 0002, constitution VI |
| `Fail(string)` creates a message-only error | Still supported (AC-2.3) with a generic code; factories of `Error` require a code | ADR 0002 resolved question 1 |
| `Data` returns `default(T)` on failure | `Value` throws on failure (AC-4.2) | Constitution II |
| `Ok(null)` is allowed | Rejected (AC-1.3) | Constitution II; Q1 |
| `ResponseFactory` static class, plus duplicate factories on both types | Factories on the result types only (names in the plan) | DRY |
| `Response.Ok(object)` returns `Response<object>` | Removed | Loses the type; use the generic factory |
| `ResponseMapper.Map` keeps only the first error message | `Map`/`Bind` keep every error (AC-6.2, AC-6.4) | Constitution III |
| `[JsonIgnore]` attributes; the object itself is the HTTP body | No serialization attributes or wire format in the core | ADR 0003; adapter spec |
| Implicit conversion to FluentResults `Result` | Removed | Constitution VI |

**Recommended category → HTTP status mapping** (informative; the adapter spec makes it normative):
`Validation` 400 · `Failure` 400 · `Unauthorized` 401 · `Forbidden` 403 · `NotFound` 404 ·
`Conflict` 409 · `Unexpected` 500.

## 6. Out of scope (future specs)

- **Async composition** (`Map`/`Bind`/`Match` over `Task<Result<T>>`) — candidate **spec 002**, next;
  handlers are async, so this follows immediately (Q4).
- **NuGet release** of `Tessaro.Results` — candidate spec 003, reusing tessaro-optional's release
  workflow through the `tessaro-bootstrap` skill.
- **ASP.NET Core adapter** (`Tessaro.Results.AspNetCore`): status mapping, success status
  (`201`/`204`), `ProblemDetails`, error metadata (ADR 0002 resolved question 2).
- Side-effect helpers (`Tap`, `OnSuccess`, `OnFailure`), `Ensure`, `MapErrors`, value fallbacks
  (`ValueOr`), combining several results into one (collecting validation errors).
- JSON serialization of results and errors.
- Interop with `Tessaro.Optional` (separate package).
- Value equality between results (errors have it; results compare by reference).

## 7. Non-functional requirements

- **NFR-1** — Zero runtime dependencies: the package references only the .NET base class library.
- **NFR-2** — Results and errors are immutable and safe to share between threads.
- **NFR-3** — Public types live in the `Tessaro` namespace and compile next to `Tessaro.Optional`
  and ASP.NET Core's `Microsoft.AspNetCore.Http` without aliases (ADR 0004; consumer spike in the plan).

## 8. Resolved questions (2026-11-09, owner approved the recommendations)

1. **`null` values in a success are rejected** (AC-1.3); "maybe absent" is a `NotFound` failure
   or a `Tessaro.Optional` value.
2. **The generic code for message-only failures is `general.failure`** — lower-case,
   dot-separated, matching the code style of the examples (AC-2.3).
3. **Implicit conversions from `T` and from `Error` are kept** (AC-1.4, AC-2.7); the docs state
   that `T` must not itself be `Error`, since `Result<Error>` would be ambiguous.
4. **Async composition is spec 002**, started right after 001.
5. **Operation names are `Map`, `Bind`, `Match`**; aliases (`Then`, `AndThen`) only if a later
   spec needs them.
6. **No first-error shortcut** (`result.Error`); use `Errors[0]`.

## 9. Review checklist

- [x] Every use of `Response.cs` found in the four services is covered or listed in §5 with a reason
- [x] Every acceptance criterion is testable as written
- [x] Nothing in this spec prescribes implementation details
- [x] Out-of-scope items are acceptable for this release
- [x] Open questions answered or deferred
