namespace Tessaro.Tests;

public class ErrorTests
{
    private const string Code = "order.not_found";
    private const string Message = "Order 42 was not found";

    public static TheoryData<ErrorType> AllTypes => [.. Enum.GetValues<ErrorType>()];

    private static Error Create(ErrorType type, string code, string message) => type switch
    {
        ErrorType.Failure => Error.Failure(code, message),
        ErrorType.Validation => Error.Validation(code, message),
        ErrorType.NotFound => Error.NotFound(code, message),
        ErrorType.Conflict => Error.Conflict(code, message),
        ErrorType.Unauthorized => Error.Unauthorized(code, message),
        ErrorType.Forbidden => Error.Forbidden(code, message),
        ErrorType.Unexpected => Error.Unexpected(code, message),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    [Fact]
    [Trait("AC", "AC-3.1")]
    public void There_are_exactly_seven_categories() =>
        Enum.GetNames<ErrorType>().ShouldBe(
            ["Failure", "Validation", "NotFound", "Conflict", "Unauthorized", "Forbidden", "Unexpected"]);

    [Theory]
    [MemberData(nameof(AllTypes))]
    [Trait("AC", "AC-3.1")]
    public void Factory_sets_code_message_and_type(ErrorType type)
    {
        Error error = Create(type, Code, Message);

        error.Code.ShouldBe(Code);
        error.Message.ShouldBe(Message);
        error.Type.ShouldBe(type);
    }

    [Theory]
    [MemberData(nameof(AllTypes))]
    [Trait("AC", "AC-3.2")]
    public void Null_code_throws_ArgumentNullException(ErrorType type) =>
        Should.Throw<ArgumentNullException>(() => Create(type, null!, Message)).ParamName.ShouldBe("code");

    [Theory]
    [MemberData(nameof(AllTypes))]
    [Trait("AC", "AC-3.2")]
    public void Null_message_throws_ArgumentNullException(ErrorType type) =>
        Should.Throw<ArgumentNullException>(() => Create(type, Code, null!)).ParamName.ShouldBe("message");

    [Theory]
    [InlineData(ErrorType.Failure, "")]
    [InlineData(ErrorType.Failure, "   ")]
    [InlineData(ErrorType.Unexpected, "\t")]
    [Trait("AC", "AC-3.2")]
    public void Blank_code_throws_ArgumentException(ErrorType type, string code)
    {
        ArgumentException ex = Should.Throw<ArgumentException>(() => Create(type, code, Message));

        ex.ShouldNotBeOfType<ArgumentNullException>();
        ex.ParamName.ShouldBe("code");
    }

    [Theory]
    [InlineData(ErrorType.Validation, "")]
    [InlineData(ErrorType.Validation, "   ")]
    [InlineData(ErrorType.NotFound, "\n")]
    [Trait("AC", "AC-3.2")]
    public void Blank_message_throws_ArgumentException(ErrorType type, string message)
    {
        ArgumentException ex = Should.Throw<ArgumentException>(() => Create(type, Code, message));

        ex.ShouldNotBeOfType<ArgumentNullException>();
        ex.ParamName.ShouldBe("message");
    }

    [Theory]
    [MemberData(nameof(AllTypes))]
    [Trait("AC", "AC-3.3")]
    public void Errors_with_the_same_values_are_equal(ErrorType type)
    {
        Error a = Create(type, Code, Message);
        Error b = Create(type, Code, Message);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.Equals((object)b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Theory]
    [InlineData("other.code", Message, ErrorType.NotFound)]
    [InlineData(Code, "Another message", ErrorType.NotFound)]
    [InlineData(Code, Message, ErrorType.Conflict)]
    [Trait("AC", "AC-3.4")]
    public void Errors_differing_in_code_message_or_type_are_not_equal(string code, string message, ErrorType type)
    {
        var reference = Error.NotFound(Code, Message);
        Error other = Create(type, code, message);

        reference.ShouldNotBe(other);
        (reference != other).ShouldBeTrue();
    }

    [Fact]
    [Trait("AC", "AC-3.4")]
    public void An_error_is_not_equal_to_null() => Error.Failure(Code, Message).Equals(null).ShouldBeFalse();

    [Theory]
    [MemberData(nameof(AllTypes))]
    [Trait("AC", "AC-3.5")]
    public void ToString_contains_type_code_and_message(ErrorType type) =>
        Create(type, Code, Message).ToString().ShouldBe($"{type} {Code}: {Message}");
}
