namespace Tessaro.Tests;

public class TransformTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");
    private static readonly Error _downstream = Error.Conflict("quote.locked", "Quote is locked");

    private static Result<int> Failed() => Result.Fail<int>([_first, _second]);

    private static Result FailedWithoutValue() => Result.Fail([_first, _second]);

    // Counts calls, so tests can assert that a delegate did or did not run.
    private sealed class Calls
    {
        public int Count { get; private set; }

        public TOut Track<TOut>(TOut value)
        {
            Count++;
            return value;
        }
    }

    // --- US-6 with a value --------------------------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-6.1")]
    public void Map_on_a_success_applies_the_mapper()
    {
        Result<string> mapped = Result.Ok(21).Map(v => (v * 2).ToString(System.Globalization.CultureInfo.InvariantCulture));

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe("42");
    }

    [Fact]
    [Trait("AC", "AC-6.1")]
    public void Map_accepts_a_method_group() => Result.Ok("quote").Map(Describe).Value.ShouldBe("quote!");

    [Fact]
    [Trait("AC", "AC-6.2")]
    public void Map_on_a_failure_keeps_every_error_and_does_not_call_the_mapper()
    {
        var calls = new Calls();

        Result<string> mapped = Failed().Map(v => calls.Track(v.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        mapped.IsFailure.ShouldBeTrue();
        mapped.Errors.ShouldBe([_first, _second]);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-6.3")]
    public void Bind_on_a_success_returns_the_next_success()
    {
        Result<string> bound = Result.Ok(7).Bind(v => Result.Ok($"quote {v}"));

        bound.Value.ShouldBe("quote 7");
    }

    [Fact]
    [Trait("AC", "AC-6.3")]
    public void Bind_on_a_success_returns_the_next_failure()
    {
        Result<string> bound = Result.Ok(7).Bind(_ => Result.Fail<string>(_downstream));

        bound.Errors.ShouldBe([_downstream]);
    }

    [Fact]
    [Trait("AC", "AC-6.3")]
    public void Bind_to_a_result_without_value_returns_the_next_result()
    {
        Result next = Result.Fail(_downstream);

        Result.Ok(7).Bind(_ => next).ShouldBeSameAs(next);
        Result.Ok(7).Bind(_ => Result.Ok()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("AC", "AC-6.4")]
    public void Bind_on_a_failure_keeps_every_error_and_does_not_call_next()
    {
        var calls = new Calls();
        Result<int> failed = Failed();

        Result<string> typed = failed.Bind(v => calls.Track(Result.Ok("x")));
        Result untyped = failed.Bind(v => calls.Track(Result.Ok()));

        typed.Errors.ShouldBe([_first, _second]);
        untyped.Errors.ShouldBe([_first, _second]);
        untyped.ShouldBeSameAs(failed);
        calls.Count.ShouldBe(0);
    }

    // --- US-6 without a value -----------------------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-6.5")]
    public void Bind_without_value_on_a_success_returns_the_next_result()
    {
        Result next = Result.Fail(_downstream);

        Result.Ok().Bind(() => next).ShouldBeSameAs(next);
        Result.Ok().Bind(() => Result.Ok(5)).Value.ShouldBe(5);
    }

    [Fact]
    [Trait("AC", "AC-6.5")]
    public void Bind_without_value_on_a_failure_keeps_every_error_and_does_not_call_next()
    {
        var calls = new Calls();
        Result failed = FailedWithoutValue();

        Result untyped = failed.Bind(() => calls.Track(Result.Ok()));
        Result<int> typed = failed.Bind(() => calls.Track(Result.Ok(5)));

        untyped.ShouldBeSameAs(failed);
        typed.Errors.ShouldBe([_first, _second]);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-6.6")]
    public void Map_without_value_on_a_success_produces_a_value() => Result.Ok().Map(() => 42).Value.ShouldBe(42);

    [Fact]
    [Trait("AC", "AC-6.6")]
    public void Map_without_value_on_a_failure_keeps_every_error_and_does_not_call_the_mapper()
    {
        var calls = new Calls();

        Result<int> mapped = FailedWithoutValue().Map(() => calls.Track(42));

        mapped.Errors.ShouldBe([_first, _second]);
        calls.Count.ShouldBe(0);
    }

    // --- Invalid delegates and results (fail fast, in both states) ---------------------------------

    public static TheoryData<bool> SuccessOrFailure => [true, false];

    private static Result<int> Typed(bool success) => success ? Result.Ok(1) : Failed();

    private static Result Untyped(bool success) => success ? Result.Ok() : FailedWithoutValue();

    [Theory]
    [MemberData(nameof(SuccessOrFailure))]
    [Trait("AC", "AC-6.7")]
    public void Null_delegates_throw_ArgumentNullException(bool success)
    {
        Should.Throw<ArgumentNullException>(() => Typed(success).Map((Func<int, string>)null!)).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => Typed(success).Bind((Func<int, Result<string>>)null!)).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => Typed(success).Bind((Func<int, Result>)null!)).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => Untyped(success).Map((Func<string>)null!)).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => Untyped(success).Bind((Func<Result<string>>)null!)).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => Untyped(success).Bind((Func<Result>)null!)).ParamName.ShouldBe("next");
    }

    [Fact]
    [Trait("AC", "AC-6.8")]
    public void Map_returning_null_throws_InvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => Result.Ok(1).Map<string>(_ => null!)).Message.ShouldContain("mapper returned null");
        Should.Throw<InvalidOperationException>(() => Result.Ok().Map<string>(() => null!)).Message.ShouldContain("mapper returned null");
    }

    [Fact]
    [Trait("AC", "AC-6.9")]
    public void Bind_returning_a_null_result_throws_InvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => Result.Ok(1).Bind<string>(_ => null!)).Message.ShouldContain("returned a null result");
        Should.Throw<InvalidOperationException>(() => Result.Ok(1).Bind(_ => (Result)null!)).Message.ShouldContain("returned a null result");
        Should.Throw<InvalidOperationException>(() => Result.Ok().Bind<string>(() => null!)).Message.ShouldContain("returned a null result");
        Should.Throw<InvalidOperationException>(() => Result.Ok().Bind(() => (Result)null!)).Message.ShouldContain("returned a null result");
    }

    private static string Describe(string value) => value + "!";
}
