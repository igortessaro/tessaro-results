# Tessaro.Results

> A zero-dependency result type for .NET — return success or categorised errors instead of
> throwing, and compose outcomes with `Map`, `Bind` and `Match`.

[![CI](https://github.com/igortessaro/tessaro-results/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/igortessaro/tessaro-results/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Tessaro.Results)](https://www.nuget.org/packages/Tessaro.Results)

```csharp
using Tessaro;

public async Task<Result<Quote>> Handle(UpdateQuoteStatus request)
{
    Quote? quote = await repository.FindAsync(request.Id);
    if (quote is null)
    {
        return Error.NotFound("quote.not_found", $"Quote {request.Id} was not found");
    }

    return quote.ChangeStatus(request.Status);   // Result<Quote>
}
```

- **One model:** `Result` (no value), `Result<T>` (with a value) and `Error`. A `Result<T>` **is a** `Result`.
- **Every error kept:** a failure holds a read-only, non-empty list of errors, and every operation passes all of them on.
- **Transport-agnostic:** errors have a category (`NotFound`, `Validation`, …), never an HTTP status code.
- **Fail fast:** `null` values, `null` delegates and empty error lists are rejected immediately.
- **Async-friendly:** `MapAsync`, `BindAsync` and `MatchAsync` compose pending results (`Task<Result<T>>`) and async steps with the same rules.
- **Side effects in the chain:** `OnSuccess` and `OnFailure` (and their `Async` forms) publish, log or audit on one outcome and pass the same result on.
- **Zero runtime dependencies** — only the .NET base class library. Targets `net10.0`.

## Installation

```bash
dotnet add package Tessaro.Results
```

All public types live in the `Tessaro` namespace (the same as [Tessaro.Optional](https://github.com/igortessaro/tessaro-optional)):

```csharp
using Tessaro;
```

## Usage

### Errors

An `Error` has a stable `Code`, a human-readable `Message` and a `Type` category. Create it with the factory of its category:

```csharp
Error notFound = Error.NotFound("quote.not_found", "Quote 42 was not found");
Error invalid = Error.Validation("quote.status", "Status is required");

Console.WriteLine(notFound);        // NotFound quote.not_found: Quote 42 was not found
Console.WriteLine(notFound == Error.NotFound("quote.not_found", "Quote 42 was not found")); // True
```

Categories: `Failure`, `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `Unexpected`.
A `null`, empty or white-space code or message throws.

### Create results

```csharp
Result ok = Result.Ok();                                  // success without a value
Result<int> answer = Result.Ok(42);                       // success with a value
Result failed = Result.Fail(notFound);                    // one error
Result<int> invalidInput = Result.Fail<int>([notFound, invalid]);   // several errors, in order
Result general = Result.Fail("Something went wrong");     // code general.failure, type Failure
```

Inside a method that returns a result, return the value or the error directly:

```csharp
public static Result<int> Parse(string text) =>
    int.TryParse(text, out var value) ? value : Error.Validation("int.parse", $"'{text}' is not a number");
```

### Inspect

```csharp
Result<int> parsed = Parse("7");

if (parsed.IsSuccess)
{
    Console.WriteLine(parsed.Value);     // 7
}

Result<int> broken = Parse("x");
Console.WriteLine(broken.IsFailure);     // True
Console.WriteLine(broken.Errors[0].Code); // int.parse
Console.WriteLine(broken);               // Failure(Validation int.parse: 'x' is not a number)
```

`Value` throws `InvalidOperationException` (listing the errors) on a failure. `Errors` is empty, never `null`, on a success.

### A `Result<T>` is a `Result`

Return or pass a `Result<T>` wherever a `Result` is expected — it is the same instance, with the same errors:

```csharp
public async Task<Result> Delete(Guid id) => await repository.RemoveAsync(id); // returns Result<Quote>
```

To return a failure without a value from a method that returns `Result<T>`, carry it over:

```csharp
public static Result<Quote> Approve(Quote quote, Result permission) =>
    permission.IsFailure ? permission.ToFailure<Quote>() : quote.ChangeStatus("approved");
```

`ToFailure<T>()` throws on a success, which has no errors to carry over.

### Transform and chain

`Map` transforms the value; `Bind` chains an operation that can fail. On a failure, neither runs its delegate, and every error is passed on.

```csharp
Result<QuoteDto> dto = (await handler.Handle(request))  // Result<Quote>
    .Bind(quote => quote.ChangeStatus("approved"))      // Result<Quote>
    .Map(QuoteDto.From);                                // Result<QuoteDto>

Result<int> total = Parse("2").Bind(x => Parse("3").Map(y => x + y)); // Success(5)
```

Both also exist without a value (`result.Map(() => …)`, `result.Bind(() => …)`).

### Branch on the outcome

`Match` handles both outcomes in one expression — for a log line, an HTTP response or a message ack:

```csharp
string log = dto.Match(
    onSuccess: q => $"quote {q.Id} updated",
    onFailure: errors => string.Join(" | ", errors.Select(e => e.Message)));
```

With ASP.NET Core, the category picks the status code in the presentation layer:

```csharp
public static IResult ToHttp(Result<Quote> result) =>
    result.Match<IResult>(
        quote => TypedResults.Ok(QuoteDto.From(quote)),
        errors => Results.Problem(detail: errors[0].Message, statusCode: StatusFor(errors[0].Type)));
```

where `StatusFor` maps the category with this table (an adapter package may provide it later):

| Category | Recommended status |
|---|---|
| `Validation`, `Failure` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Unexpected` | 500 |

### Async

Every member that returns a `Task` ends with `Async` ([ADR 0006](docs/adr/0006-async-members-end-with-async.md)).
On a **pending result** (`Task<Result<T>>` or `Task<Result>`) the steps can be synchronous or asynchronous:

```csharp
public Task<Result<ServiceOrder>> Handle(UpdateStatus request, CancellationToken ct) =>
    repository.FindAsync(request.Id, ct)                              // Task<Result<ServiceOrder>>
        .BindAsync(order => order.ChangeStatus(request.Status))      // sync step on a pending result
        .BindAsync(order => repository.UpdateAsync(order, ct))       // async step
        .MapAsync(order => repository.GetDetailedAsync(order.Id, ct)); // async mapper
```

`MatchAsync` turns a pending result into a response in one expression:

```csharp
public async Task<IResult> Update(Guid id, UpdateStatusRequest body, CancellationToken ct) =>
    await mediator.Send(body.ToCommand(id), ct)                      // Task<Result<ServiceOrder>>
        .MapAsync(ServiceOrderDto.From)
        .MatchAsync(dto => TypedResults.Ok(dto), errors => errors.ToProblem()); // ToProblem: your mapping (spec 001 §5)
```

On a **completed result**, `MapAsync`/`BindAsync`/`MatchAsync` take asynchronous steps:

```csharp
Result<ServiceOrder> saved = await order.ChangeStatus("approved").BindAsync(o => repository.UpdateAsync(o, ct));
```

- A failure passes every error on and no later step runs.
- `null` arguments throw **when you call** the member, before anything is awaited.
- Exceptions and cancellation from the pending result or a step reach you unchanged — they never become failures.
- The library never resumes on your synchronization context (`ConfigureAwait(false)`).
- The synchronous `Map` refuses an async mapper at compile time, instead of returning a result that holds an unawaited task:

```csharp
var wrong = result.Map(o => repository.GetDetailedAsync(o.Id, ct)); // does not compile (CS0619): use MapAsync
```

### Side effects

`OnSuccess` runs an action after a success, `OnFailure` after a failure (with **every** error), and
both pass the **same result** on: nothing is transformed or re-created, and a `Result<Quote>` stays a
`Result<Quote>`. The action of the other outcome never runs.

```csharp
public Task<Result<Quote>> Handle(UpdateQuoteStatus request, CancellationToken ct) =>
    repository.FindAsync(request.Id, ct)                                  // Task<Result<Quote>>
        .BindAsync(quote => quote.ChangeStatus(request.Status))
        .BindAsync(quote => repository.UpdateAsync(quote, ct))
        .OnSuccessAsync(quote => mediator.Publish(new QuoteStatusChanged(quote), ct)) // awaited
        .OnFailureAsync(errors => logger.LogWarning("Quote {Id} not updated: {Errors}", request.Id, errors));
```

On a completed result the synchronous steps chain like `Map` and `Bind`:

```csharp
Result<string> status = result
    .OnSuccess(quote => audit.Add($"quote {quote.Id} is {quote.Status}"))
    .OnFailure(errors => audit.Add($"{errors.Count} error(s)"))
    .Map(quote => quote.Status);

Result outcome = await mediator.Send(command, ct);
outcome.OnFailure(errors => logger.LogWarning("Message {Tag} had errors: {Errors}", tag, errors));
channel.BasicAck(tag, multiple: false);                                   // ack either way
```

- `OnSuccessAsync`/`OnFailureAsync` **await** the action before the chain continues; on a pending
  result they also take synchronous actions, on a completed result only asynchronous ones.
- An exception or cancellation from an action reaches you unchanged — it never becomes a failure.
- The compiler refuses the two mistakes that would silently lose work:

```csharp
result.OnSuccess(q => mediator.Publish(new QuoteStatusChanged(q), ct)); // does not compile (CS0619): use OnSuccessAsync
pending.OnSuccessAsync(q => repository.UpdateAsync(q, ct));             // does not compile (CS0619): use BindAsync
```

The first would never await the task (an `async` lambda would become `async void`); the second
would ignore a failed update. An operation that can fail belongs in `Bind`/`BindAsync`.

## Known limitations

- **Call the factories on `Result`.** `Result<Quote>.Ok()` compiles (static members are inherited) but returns a `Result`; assigning it to a `Result<Quote>` is a compile error.
- **No implicit conversion from an interface-typed value.** C# does not allow it: `IReadOnlyList<int> items; return items;` does not compile. Use `Result.Ok(items)`, or return a concrete type.
- **`T` must not be `Error`.** `Result<Error>` makes the two implicit conversions ambiguous.
- **`Bind` with a lambda that returns a plain value** does not infer the type: use `Map`, or write `Bind<string>(…)`.
- **`Map<T>(x => null!)` with an explicit type argument is ambiguous** (because of the async guard above): type the `null`, e.g. `Map<string>(_ => (string)null!)`.
- **On a completed result, `BindAsync` takes only async steps**; for a synchronous step use `Bind`.
- **No `ValueTask` or `CancellationToken` parameters**; steps capture the caller's token, as in the handler above. An action returning a `ValueTask` is not guarded: `OnSuccess` discards it.
- **`OnSuccess(Console.WriteLine)` is ambiguous** when a method group has overloads both with and without one argument: use a lambda, `q => Console.WriteLine(q)`.
- **An action that only throws** (`OnSuccess(q => throw …)`) hits the `Task` guard: call a method that throws instead.
- **A block lambda that discards a task** (`OnSuccess(q => { mediator.Publish(q, ct); })`) compiles: the guards only see a lambda's return type.
- **An untyped `null` action** (`OnFailure(null!)`) binds to a guard and does not compile: type it, e.g. `(Action<IReadOnlyList<Error>>)null!`.

## Migrating from `Response.cs`

For services that copied `Response` / `Response<T>` / `ResponseFactory` (FluentResults-based):

| `Response.cs` | `Tessaro.Results` |
|---|---|
| `ResponseFactory.Ok(value, HttpStatusCode.OK)` | `Result.Ok(value)` or `return value;` — choose `200`/`201`/`204` in the controller |
| `ResponseFactory.Fail<T>("Quote not found", HttpStatusCode.NotFound)` | `Error.NotFound("quote.not_found", "Quote not found")` — pick a code and the category that matches the old status |
| `Response.Fail(string)` (message only) | `Result.Fail("message")` (code `general.failure`) |
| `response.Data` (`default` on failure) | `result.Value` (throws on failure) — or `Match` |
| `response.Reasons` (`List<IReason>`) | `result.Errors` (read-only `IReadOnlyList<Error>`) |
| `Response<T>` converted to `Response` (re-created, errors flattened) | `Result<T>` **is** a `Result` — return it as is |
| `ResponseMapper.Map(response, converter)` (first error only) | `result.Map(converter)` (every error) |
| `response.StatusCode` / `ObjectResult { StatusCode = … }` | map `Errors[0].Type` to a status in the presentation layer (table above) |
| `Response.Ok(object)` → `Response<object>` | `Result.Ok<T>(value)` with the real type |
| `implicit operator Result` (FluentResults) | removed — no FluentResults dependency |
| `[JsonIgnore]` on the response | the core has no serialization attributes; build the response body in the presentation layer |

## Project docs

- [Constitution](docs/constitution.md) — principles and quality gates.
- [Architecture decisions](docs/adr/) — e.g. why `Result<T>` derives from `Result` ([ADR 0001](docs/adr/0001-result-of-t-derives-from-result.md)) and why there is no status code ([ADR 0003](docs/adr/0003-transport-agnostic-core.md)).
- [Spec 001 — Result core](specs/001-result-core/spec.md), its [plan](specs/001-result-core/plan.md) and [tasks](specs/001-result-core/tasks.md).
- [Spec 002 — Async composition](specs/002-async-composition/spec.md), its [plan](specs/002-async-composition/plan.md) and [tasks](specs/002-async-composition/tasks.md).
- [Spec 004 — Side-effect steps](specs/004-side-effect-steps/spec.md), its [plan](specs/004-side-effect-steps/plan.md) and [tasks](specs/004-side-effect-steps/tasks.md).
- [Contributing](CONTRIBUTING.md).

## License

[MIT](LICENSE)
