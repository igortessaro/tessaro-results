namespace Tessaro.Tests;

public class InspectTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    [Fact]
    [Trait("AC", "AC-4.1")]
    public void Value_of_a_success_is_the_value() => Result.Ok("quote").Value.ShouldBe("quote");

    [Fact]
    [Trait("AC", "AC-4.2")]
    public void Value_of_a_failure_throws_InvalidOperationException_listing_the_errors()
    {
        Result<int> result = Result.Fail<int>([_first, _second]);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => result.Value);

        ex.Message.ShouldContain("Cannot read the value of a failed result");
        ex.Message.ShouldContain(_first.ToString());
        ex.Message.ShouldContain(_second.ToString());
    }

    [Fact]
    [Trait("AC", "AC-4.3")]
    public void Errors_of_a_success_without_value_is_empty_not_null() => Result.Ok().Errors.ShouldNotBeNull().ShouldBeEmpty();

    [Fact]
    [Trait("AC", "AC-4.3")]
    public void Errors_of_a_success_with_value_is_empty_not_null() => Result.Ok(1).Errors.ShouldNotBeNull().ShouldBeEmpty();

    [Fact]
    [Trait("AC", "AC-4.4")]
    public void ToString_of_a_success_without_value() => Result.Ok().ToString().ShouldBe("Success");

    [Fact]
    [Trait("AC", "AC-4.4")]
    public void ToString_of_a_success_with_value_shows_the_value() => Result.Ok(42).ToString().ShouldBe("Success(42)");

    [Fact]
    [Trait("AC", "AC-4.4")]
    public void ToString_of_a_failure_lists_every_error()
    {
        string expected = $"Failure({_first}; {_second})";

        Result.Fail([_first, _second]).ToString().ShouldBe(expected);
        Result.Fail<int>([_first, _second]).ToString().ShouldBe(expected);
    }
}
