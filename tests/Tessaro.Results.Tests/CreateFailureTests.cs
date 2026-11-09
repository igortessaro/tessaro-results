namespace Tessaro.Tests;

public class CreateFailureTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    public static TheoryData<bool> BothShapes => [false, true];

    // A failure of either shape: without a value (Result) or with one (Result<int>).
    private static Result FailWith(bool withValue, Error error) =>
        withValue ? Result.Fail<int>(error) : Result.Fail(error);

    private static Result FailWith(bool withValue, IEnumerable<Error> errors) =>
        withValue ? Result.Fail<int>(errors) : Result.Fail(errors);

    private static Result FailWith(bool withValue, string message) =>
        withValue ? Result.Fail<int>(message) : Result.Fail(message);

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.1")]
    public void Fail_from_one_error_holds_exactly_that_error(bool withValue)
    {
        Result result = FailWith(withValue, _first);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldBe([_first]);
    }

    [Fact]
    [Trait("AC", "AC-2.1")]
    public void Fail_of_T_returns_a_Result_of_T() => Result.Fail<int>(_first).ShouldBeOfType<Result<int>>();

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.2")]
    public void Fail_from_several_errors_keeps_them_in_order(bool withValue) =>
        FailWith(withValue, [_first, _second]).Errors.ShouldBe([_first, _second]);

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.3")]
    public void Fail_from_a_message_creates_a_general_failure(bool withValue)
    {
        Result result = FailWith(withValue, "Something went wrong");

        Error error = result.Errors.ShouldHaveSingleItem();
        error.Message.ShouldBe("Something went wrong");
        error.Type.ShouldBe(ErrorType.Failure);
        error.Code.ShouldBe("general.failure");
    }

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.4")]
    public void Fail_from_an_empty_list_throws_ArgumentException(bool withValue)
    {
        ArgumentException ex = Should.Throw<ArgumentException>(() => FailWith(withValue, Array.Empty<Error>()));

        ex.ShouldNotBeOfType<ArgumentNullException>();
        ex.ParamName.ShouldBe("errors");
    }

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.5")]
    public void Fail_from_a_null_error_throws_ArgumentNullException(bool withValue) =>
        Should.Throw<ArgumentNullException>(() => FailWith(withValue, (Error)null!)).ParamName.ShouldBe("error");

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.5")]
    public void Fail_from_a_null_list_throws_ArgumentNullException(bool withValue) =>
        Should.Throw<ArgumentNullException>(() => FailWith(withValue, (IEnumerable<Error>)null!)).ParamName.ShouldBe("errors");

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.5")]
    public void Fail_from_a_list_containing_null_throws_ArgumentNullException(bool withValue) =>
        Should.Throw<ArgumentNullException>(() => FailWith(withValue, [_first, null!])).ParamName.ShouldBe("errors");

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.6")]
    public void Fail_from_a_null_message_throws_ArgumentNullException(bool withValue) =>
        Should.Throw<ArgumentNullException>(() => FailWith(withValue, (string)null!)).ParamName.ShouldBe("message");

    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "  ")]
    [InlineData(true, "")]
    [InlineData(true, "\t")]
    [Trait("AC", "AC-2.6")]
    public void Fail_from_a_blank_message_throws_ArgumentException(bool withValue, string message)
    {
        ArgumentException ex = Should.Throw<ArgumentException>(() => FailWith(withValue, message));

        ex.ShouldNotBeOfType<ArgumentNullException>();
        ex.ParamName.ShouldBe("message");
    }

    [Fact]
    [Trait("AC", "AC-2.7")]
    public void Returning_an_error_from_a_Result_method_creates_a_failure()
    {
        Result result = ReturnError(_first);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldBe([_first]);
    }

    [Fact]
    [Trait("AC", "AC-2.7")]
    public void Returning_an_error_from_a_Result_of_T_method_creates_a_failure()
    {
        Result<int> result = ReturnErrorOf<int>(_first);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldBe([_first]);
    }

    [Fact]
    [Trait("AC", "AC-2.7")]
    public void Implicit_conversion_from_a_null_error_throws_ArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => ReturnError(null!)).ParamName.ShouldBe("error");
        Should.Throw<ArgumentNullException>(() => ReturnErrorOf<int>(null!)).ParamName.ShouldBe("error");
    }

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.8")]
    public void Changing_the_source_list_later_does_not_change_the_failure(bool withValue)
    {
        List<Error> source = [_first];
        Result result = FailWith(withValue, source);

        source.Add(_second);
        source[0] = _second;

        result.Errors.ShouldBe([_first]);
    }

    [Theory]
    [MemberData(nameof(BothShapes))]
    [Trait("AC", "AC-2.9")]
    public void Errors_of_a_failure_cannot_be_modified(bool withValue)
    {
        var errors = (IList<Error>)FailWith(withValue, [_first]).Errors;

        Should.Throw<NotSupportedException>(() => errors.Add(_second));
        Should.Throw<NotSupportedException>(() => errors[0] = _second);
        Should.Throw<NotSupportedException>(() => errors.RemoveAt(0));
        Should.Throw<NotSupportedException>(errors.Clear);
        errors.ShouldBe([_first]);
    }

    [Fact]
    [Trait("AC", "AC-2.9")]
    public void Errors_of_a_success_cannot_be_modified()
    {
        var errors = (IList<Error>)Result.Ok().Errors;

        Should.Throw<NotSupportedException>(() => errors.Add(_first));
        errors.ShouldBeEmpty();
    }

    private static Result ReturnError(Error error) => error;

    private static Result<T> ReturnErrorOf<T>(Error error) where T : notnull => error;
}
