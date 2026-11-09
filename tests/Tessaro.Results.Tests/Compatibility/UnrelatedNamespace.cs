// NFR-3 / plan 001 §5: the public names compile, without aliases, from a namespace unrelated to
// Tessaro that also imports ASP.NET Core's Microsoft.AspNetCore.Http (Results, TypedResults, IResult).
// This file is the spec's "Success looks like" example; if a name clashes, the project stops compiling.
using Microsoft.AspNetCore.Http;
using Tessaro;

namespace Consumer.Compat;

public sealed record UpdateQuoteStatus(Guid Id, string Status);

public sealed record Quote(Guid Id, string Status)
{
    public Result<Quote> ChangeStatus(string status) =>
        string.IsNullOrWhiteSpace(status)
            ? Error.Validation("quote.status", "Status is required")
            : this with { Status = status };
}

public sealed record QuoteDto(Guid Id, string Status)
{
    public static QuoteDto From(Quote quote) => new(quote.Id, quote.Status);
}

public sealed class QuoteRepository(params Quote[] quotes)
{
    private readonly Dictionary<Guid, Quote> _quotes = quotes.ToDictionary(q => q.Id);

    public Task<Quote?> FindAsync(Guid id) => Task.FromResult(_quotes.GetValueOrDefault(id));

    public Task<Result<Quote>> RemoveAsync(Guid id) =>
        Task.FromResult<Result<Quote>>(_quotes.Remove(id, out Quote? removed)
            ? removed
            : Error.NotFound("quote.not_found", $"Quote {id} was not found"));
}

public sealed class QuoteHandler(QuoteRepository repository)
{
    // Application layer: no HTTP, no FluentResults.
    public async Task<Result<Quote>> Handle(UpdateQuoteStatus request)
    {
        Quote? quote = await repository.FindAsync(request.Id);
        if (quote is null)
        {
            return Error.NotFound("quote.not_found", $"Quote {request.Id} was not found");
        }

        return quote.ChangeStatus(request.Status);   // Result<Quote>
    }

    // A Result<T> is a Result: no re-creation, no lost errors.
    public async Task<Result> Delete(Guid id) => await repository.RemoveAsync(id); // returns Result<Quote>

    // Mapping keeps every error; branching needs no `if (!IsSuccess)`.
    public static string Log(Result<Quote> result)
    {
        Result<QuoteDto> dto = result.Map(QuoteDto.From);
        return dto.Match(
            onSuccess: q => $"quote {q.Id} updated",
            onFailure: errors => string.Join(" | ", errors.Select(e => e.Message)));
    }

    // Presentation layer: the category picks the HTTP status (spec §5, recommended mapping).
    public static IResult ToHttp(Result<Quote> result) =>
        result.Match<IResult>(
            quote => TypedResults.Ok(QuoteDto.From(quote)),
            errors => Results.Problem(detail: errors[0].Message, statusCode: StatusFor(errors[0].Type)));

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation or ErrorType.Failure => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };
}

public class UnrelatedNamespaceTests
{
    private static readonly Guid _id = Guid.Parse("00000000-0000-0000-0000-000000000042");

    [Fact]
    [Trait("NFR", "NFR-3")]
    public async Task The_spec_example_runs_from_an_unrelated_namespace()
    {
        var handler = new QuoteHandler(new QuoteRepository(new Quote(_id, "new")));

        QuoteHandler.Log(await handler.Handle(new UpdateQuoteStatus(_id, "approved"))).ShouldBe($"quote {_id} updated");
        QuoteHandler.Log(await handler.Handle(new UpdateQuoteStatus(_id, " "))).ShouldBe("Status is required");
        QuoteHandler.Log(await handler.Handle(new UpdateQuoteStatus(Guid.Empty, "approved")))
            .ShouldBe($"Quote {Guid.Empty} was not found");

        (await handler.Delete(_id)).IsSuccess.ShouldBeTrue();
        (await handler.Delete(_id)).Errors.ShouldHaveSingleItem().Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    [Trait("NFR", "NFR-3")]
    public async Task Results_map_to_ASP_NET_Core_results_next_to_its_own_Results_type()
    {
        var handler = new QuoteHandler(new QuoteRepository(new Quote(_id, "new")));

        QuoteHandler.ToHttp(await handler.Handle(new UpdateQuoteStatus(_id, "approved")))
            .ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<QuoteDto>>();
        QuoteHandler.ToHttp(await handler.Handle(new UpdateQuoteStatus(Guid.Empty, "approved")))
            .ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>()
            .StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
