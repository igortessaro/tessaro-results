// NFR-3 / plan 001 §5: inside a Tessaro.* namespace the public names resolve with no `using`, next to
// Tessaro.Optional's Optional<T> in the same root namespace (ADR 0004).
namespace Tessaro.Compat;

public static class Parsing
{
    public static Result<int> Parse(string text) =>
        int.TryParse(text, out var value) ? value : Error.Validation("int.parse", $"'{text}' is not a number");

    public static Optional<int> ToOptional(Result<int> result) =>
        result.Match(Optional.Of, _ => Optional.Empty<int>());

    public static Result<int> FromOptional(Optional<int> optional) =>
        optional.Map(Result.Ok).OrElseGet(() => Result.Fail<int>("no value"));
}

public class SiblingNamespaceTests
{
    [Fact]
    [Trait("NFR", "NFR-3")]
    public void Result_and_Optional_interoperate_inside_a_Tessaro_namespace()
    {
        Parsing.ToOptional(Parsing.Parse("7")).ShouldBe(Optional.Of(7));
        Parsing.ToOptional(Parsing.Parse("x")).IsEmpty.ShouldBeTrue();
        Parsing.FromOptional(Optional.Of(3)).Value.ShouldBe(3);
        Parsing.FromOptional(Optional.Empty<int>()).Errors.ShouldHaveSingleItem().Code.ShouldBe("general.failure");
    }
}
