namespace Tessaro.Tests;

// Spec 004, US-2 — a side effect after a failure.
[Trait("Spec", "004")]
public class FailureSideEffectTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    [Fact]
    [Trait("AC", "AC-2.1")]
    public void OnFailure_on_a_failure_calls_the_action_once_with_every_error_and_returns_the_same_instance()
    {
        Result<int> typed = Result.Fail<int>([_first, _second]);
        Result untyped = Result.Fail([_first, _second]);
        var seen = new List<IReadOnlyList<Error>>();

        Result<int> typedReturned = typed.OnFailure(seen.Add);
        Result untypedReturned = untyped.OnFailure(seen.Add);

        seen.Count.ShouldBe(2);
        seen[0].ShouldBe([_first, _second]);
        seen[1].ShouldBe([_first, _second]);
        typedReturned.ShouldBeSameAs(typed);
        untypedReturned.ShouldBeSameAs(untyped);
    }

    [Fact]
    [Trait("AC", "AC-2.2")]
    public void OnFailure_on_a_success_does_not_call_the_action_and_returns_the_same_instance()
    {
        Result<int> typed = Result.Ok(1);
        Result untyped = Result.Ok();
        int calls = 0;

        Result<int> typedReturned = typed.OnFailure(_ => calls++);
        Result untypedReturned = untyped.OnFailure(_ => calls++);

        calls.ShouldBe(0);
        typedReturned.ShouldBeSameAs(typed);
        untypedReturned.ShouldBeSameAs(untyped);
    }

    [Fact]
    [Trait("AC", "AC-2.3")]
    public void OnFailure_keeps_the_value_type_and_the_errors_for_the_next_step()
    {
        int mapCalls = 0;

        Result<string> mapped = Result.Fail<int>([_first, _second])
            .OnFailure(_ => { })
            .Map(v =>
            {
                mapCalls++;
                return $"quote {v}";
            });

        mapCalls.ShouldBe(0);
        mapped.Errors.ShouldBe([_first, _second]);
    }
}
