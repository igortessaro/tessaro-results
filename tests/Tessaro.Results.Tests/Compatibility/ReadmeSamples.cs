// Every code sample of README.md, compiled and run from the unrelated namespace Consumer.Compat
// (spec 001 T06). The method bodies match the README; where a sample prints, the console is a
// captured writer named `console` so the test can check the output shown in the README comments.
using Microsoft.AspNetCore.Http;
using Tessaro;

namespace Consumer.Compat;

public static class ReadmeSamples
{
    // "Errors"
    public static (Error NotFound, Error Invalid) Errors(TextWriter console)
    {
        Error notFound = Error.NotFound("quote.not_found", "Quote 42 was not found");
        Error invalid = Error.Validation("quote.status", "Status is required");

        console.WriteLine(notFound);        // NotFound quote.not_found: Quote 42 was not found
        console.WriteLine(notFound == Error.NotFound("quote.not_found", "Quote 42 was not found")); // True
        return (notFound, invalid);
    }

    // "Create results"
    public static Result[] Create(Error notFound, Error invalid)
    {
        Result ok = Result.Ok();                                  // success without a value
        Result<int> answer = Result.Ok(42);                       // success with a value
        Result failed = Result.Fail(notFound);                    // one error
        Result<int> invalidInput = Result.Fail<int>([notFound, invalid]);   // several errors, in order
        Result general = Result.Fail("Something went wrong");     // code general.failure, type Failure
        return [ok, answer, failed, invalidInput, general];
    }

    public static Result<int> Parse(string text) =>
        int.TryParse(text, out var value) ? value : Error.Validation("int.parse", $"'{text}' is not a number");

    // "Inspect"
    public static void Inspect(TextWriter console)
    {
        Result<int> parsed = Parse("7");

        if (parsed.IsSuccess)
        {
            console.WriteLine(parsed.Value);     // 7
        }

        Result<int> broken = Parse("x");
        console.WriteLine(broken.IsFailure);     // True
        console.WriteLine(broken.Errors[0].Code); // int.parse
        console.WriteLine(broken);               // Failure(Validation int.parse: 'x' is not a number)
    }

    // "A Result<T> is a Result"
    public static async Task<Result> Delete(QuoteRepository repository, Guid id) => await repository.RemoveAsync(id); // returns Result<Quote>

    public static Result<Quote> Approve(Quote quote, Result permission) =>
        permission.IsFailure ? permission.ToFailure<Quote>() : quote.ChangeStatus("approved");

    // "Transform and chain" and "Branch on the outcome"
    public static async Task<(Result<QuoteDto> Dto, Result<int> Total, string Log)> Chain(QuoteHandler handler, UpdateQuoteStatus request)
    {
        Result<QuoteDto> dto = (await handler.Handle(request))  // Result<Quote>
            .Bind(quote => quote.ChangeStatus("approved"))      // Result<Quote>
            .Map(QuoteDto.From);                                // Result<QuoteDto>

        Result<int> total = Parse("2").Bind(x => Parse("3").Map(y => x + y)); // Success(5)

        string log = dto.Match(
            onSuccess: q => $"quote {q.Id} updated",
            onFailure: errors => string.Join(" | ", errors.Select(e => e.Message)));

        return (dto, total, log);
    }

    // "Branch on the outcome" (ASP.NET Core): the README's ToHttp is QuoteHandler.ToHttp in UnrelatedNamespace.cs.
    public static IResult ToHttp(Result<Quote> result) => QuoteHandler.ToHttp(result);
}

public class ReadmeSamplesTests
{
    private static readonly Guid _id = Guid.Parse("00000000-0000-0000-0000-000000000042");

    [Fact]
    [Trait("Docs", "README")]
    public void Errors_and_inspect_print_what_the_README_shows()
    {
        using var console = new StringWriter { NewLine = "\n" };

        ReadmeSamples.Errors(console);
        ReadmeSamples.Inspect(console);

        console.ToString().ShouldBe(
            "NotFound quote.not_found: Quote 42 was not found\nTrue\n" +
            "7\nTrue\nint.parse\nFailure(Validation int.parse: 'x' is not a number)\n");
    }

    [Fact]
    [Trait("Docs", "README")]
    public void Create_samples_produce_the_described_results()
    {
        (Error notFound, Error invalid) = ReadmeSamples.Errors(TextWriter.Null);

        Result[] results = ReadmeSamples.Create(notFound, invalid);

        results.Select(r => r.IsSuccess).ShouldBe([true, true, false, false, false]);
        ((Result<int>)results[1]).Value.ShouldBe(42);
        results[3].Errors.ShouldBe([notFound, invalid]);
        results[4].Errors.ShouldHaveSingleItem().Code.ShouldBe("general.failure");
    }

    [Fact]
    [Trait("Docs", "README")]
    public async Task Hierarchy_samples_return_and_carry_over_results()
    {
        var repository = new QuoteRepository(new Quote(_id, "new"));
        var quote = new Quote(_id, "new");

        (await ReadmeSamples.Delete(repository, _id)).IsSuccess.ShouldBeTrue();
        ReadmeSamples.Approve(quote, Result.Ok()).Value.Status.ShouldBe("approved");
        ReadmeSamples.Approve(quote, Result.Fail(Error.Forbidden("quote.approve", "Not allowed")))
            .Errors.ShouldHaveSingleItem().Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    [Trait("Docs", "README")]
    public async Task Chain_and_match_samples_produce_the_described_values()
    {
        var handler = new QuoteHandler(new QuoteRepository(new Quote(_id, "new")));

        var (dto, total, log) = await ReadmeSamples.Chain(handler, new UpdateQuoteStatus(_id, "approved"));

        dto.Value.Status.ShouldBe("approved");
        total.ToString().ShouldBe("Success(5)");
        log.ShouldBe($"quote {_id} updated");
        ReadmeSamples.ToHttp(Result.Fail<Quote>(Error.Conflict("quote.locked", "Locked")))
            .ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>()
            .StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }
}
