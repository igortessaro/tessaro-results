namespace Tessaro.Tests;

public class MatchTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    private sealed class Calls
    {
        public int Count { get; private set; }

        public TOut Track<TOut>(TOut value)
        {
            Count++;
            return value;
        }
    }

    [Fact]
    [Trait("AC", "AC-7.1")]
    public void Match_on_a_success_returns_onSuccess_of_the_value_and_skips_onFailure()
    {
        var calls = new Calls();

        string text = Result.Ok(42).Match(v => $"value {v}", errors => calls.Track("failure"));

        text.ShouldBe("value 42");
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-7.2")]
    public void Match_on_a_failure_returns_onFailure_of_every_error_and_skips_onSuccess()
    {
        var calls = new Calls();
        IReadOnlyList<Error>? seen = null;

        string text = Result.Fail<int>([_first, _second]).Match(
            v => calls.Track("success"),
            errors =>
            {
                seen = errors;
                return string.Join(" | ", errors.Select(e => e.Code));
            });

        text.ShouldBe("quote.not_found | quote.status");
        seen.ShouldBe([_first, _second]);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-7.3")]
    public void Match_without_value_on_a_success_returns_onSuccess_and_skips_onFailure()
    {
        var calls = new Calls();

        Result.Ok().Match(() => "ok", errors => calls.Track("failure")).ShouldBe("ok");
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-7.3")]
    public void Match_without_value_on_a_failure_returns_onFailure_and_skips_onSuccess()
    {
        var calls = new Calls();

        int count = Result.Fail([_first, _second]).Match(() => calls.Track(-1), errors => errors.Count);

        count.ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    public static TheoryData<bool> SuccessOrFailure => [true, false];

    [Theory]
    [MemberData(nameof(SuccessOrFailure))]
    [Trait("AC", "AC-7.4")]
    public void Null_delegates_throw_ArgumentNullException(bool success)
    {
        Result<int> typed = success ? Result.Ok(1) : Result.Fail<int>(_first);
        Result untyped = success ? Result.Ok() : Result.Fail(_first);

        Should.Throw<ArgumentNullException>(() => typed.Match(null!, _ => 0)).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => typed.Match(v => v, null!)).ParamName.ShouldBe("onFailure");
        Should.Throw<ArgumentNullException>(() => untyped.Match((Func<int>)null!, _ => 0)).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => untyped.Match(() => 0, null!)).ParamName.ShouldBe("onFailure");
    }
}
