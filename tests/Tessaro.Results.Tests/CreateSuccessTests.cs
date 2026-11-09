namespace Tessaro.Tests;

public class CreateSuccessTests
{
    [Fact]
    [Trait("AC", "AC-1.1")]
    public void Ok_without_value_is_a_success_with_no_errors()
    {
        Result result = Result.Ok();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("AC", "AC-1.2")]
    public void Ok_with_a_reference_value_holds_it()
    {
        var value = new object();

        Result<object> result = Result.Ok(value);

        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Value.ShouldBeSameAs(value);
    }

    [Fact]
    [Trait("AC", "AC-1.2")]
    public void Ok_with_a_value_type_holds_it() => Result.Ok(42).Value.ShouldBe(42);

    [Fact]
    [Trait("AC", "AC-1.3")]
    public void Ok_with_null_throws_ArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => Result.Ok<string>(null!)).ParamName.ShouldBe("value");

    [Fact]
    [Trait("AC", "AC-1.3")]
    public void Implicit_conversion_from_null_throws_ArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ReturnValue<string>(null!)).ParamName.ShouldBe("value");

    [Fact]
    [Trait("AC", "AC-1.4")]
    public void Returning_a_value_from_a_Result_of_T_method_creates_a_success()
    {
        Result<string> result = ReturnValue("quote");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("quote");
    }

    [Fact]
    [Trait("AC", "AC-1.4")]
    public void Returning_a_value_type_from_a_Result_of_T_method_creates_a_success() =>
        ReturnValue(7).Value.ShouldBe(7);

    private static Result<T> ReturnValue<T>(T value) where T : notnull => value;
}
