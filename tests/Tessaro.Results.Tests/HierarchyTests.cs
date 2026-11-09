using System.Reflection;

namespace Tessaro.Tests;

public class HierarchyTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    public static TheoryData<bool> SuccessOrFailure => [true, false];

    private static Result<string> Typed(bool success) => success ? Result.Ok("quote") : Result.Fail<string>([_first, _second]);

    // A method declared to return Result that returns a Result<string> (a delegate, so CA1859 does not
    // suggest narrowing the return type: widening it is the point of AC-5.1).
    private static readonly Func<Result<string>, Result> _returnAsResult = typed => typed;

    private static Result PassThrough(Result result) => result;

    [Theory]
    [MemberData(nameof(SuccessOrFailure))]
    [Trait("AC", "AC-5.1")]
    public void A_Result_of_T_returned_as_Result_is_the_same_instance(bool success)
    {
        Result<string> typed = Typed(success);

        _returnAsResult(typed).ShouldBeSameAs(typed);
    }

    [Theory]
    [MemberData(nameof(SuccessOrFailure))]
    [Trait("AC", "AC-5.1")]
    public void A_Result_of_T_passed_as_Result_is_the_same_instance(bool success)
    {
        Result<string> typed = Typed(success);

        PassThrough(typed).ShouldBeSameAs(typed);
    }

    [Fact]
    [Trait("AC", "AC-5.1")]
    public void Results_of_different_shapes_live_in_one_collection()
    {
        Result<string> typed = Typed(success: true);
        Result untyped = Result.Ok();

        List<Result> all = [typed, untyped];

        all[0].ShouldBeSameAs(typed);
        all[1].ShouldBeSameAs(untyped);
    }

    [Fact]
    [Trait("AC", "AC-5.2")]
    public void A_failed_Result_of_T_seen_as_Result_keeps_state_and_errors_in_order()
    {
        Result asResult = Typed(success: false);

        asResult.IsFailure.ShouldBeTrue();
        asResult.IsSuccess.ShouldBeFalse();
        asResult.Errors.ShouldBe([_first, _second]);
    }

    [Fact]
    [Trait("AC", "AC-5.3")]
    public void ToFailure_turns_a_failed_Result_into_a_failed_Result_of_any_type()
    {
        Result failure = Result.Fail([_first, _second]);

        Result<int> asInt = failure.ToFailure<int>();
        Result<string> asString = failure.ToFailure<string>();

        asInt.IsFailure.ShouldBeTrue();
        asInt.Errors.ShouldBe([_first, _second]);
        asString.IsFailure.ShouldBeTrue();
        asString.Errors.ShouldBe([_first, _second]);
    }

    [Fact]
    [Trait("AC", "AC-5.3")]
    public void ToFailure_turns_a_failed_Result_of_T_into_a_failed_Result_of_another_type()
    {
        Result<int> asInt = Typed(success: false).ToFailure<int>();

        asInt.IsFailure.ShouldBeTrue();
        asInt.Errors.ShouldBe([_first, _second]);
    }

    [Fact]
    [Trait("AC", "AC-5.4")]
    public void ToFailure_on_a_success_throws_InvalidOperationException() =>
        Should.Throw<InvalidOperationException>(() => Result.Ok().ToFailure<int>())
            .Message.ShouldBe("A successful result has no errors to carry over.");

    [Fact]
    [Trait("AC", "AC-5.4")]
    public void ToFailure_on_a_success_with_a_value_throws_InvalidOperationException() =>
        Should.Throw<InvalidOperationException>(() => Typed(success: true).ToFailure<int>());

    [Fact]
    [Trait("AC", "AC-5.5")]
    public void Result_cannot_be_subclassed_outside_the_library() =>
        typeof(Result).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ShouldAllBe(c => !c.IsPublic && !c.IsFamily && !c.IsFamilyOrAssembly);

    [Fact]
    [Trait("AC", "AC-5.5")]
    public void Result_of_T_is_sealed_and_has_no_public_constructor()
    {
        typeof(Result<>).IsSealed.ShouldBeTrue();
        typeof(Result<>).GetConstructors(BindingFlags.Instance | BindingFlags.Public).ShouldBeEmpty();
    }
}
