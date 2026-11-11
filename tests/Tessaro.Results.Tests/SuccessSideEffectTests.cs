namespace Tessaro.Tests;

// Spec 004, US-1 — a side effect after a success.
[Trait("Spec", "004")]
public class SuccessSideEffectTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    [Fact]
    [Trait("AC", "AC-1.1")]
    public void OnSuccess_on_a_success_with_a_value_calls_the_action_once_with_it_and_returns_the_same_instance()
    {
        Result<int> success = Result.Ok(42);
        var seen = new List<int>();

        Result<int> returned = success.OnSuccess(v => seen.Add(v));

        seen.ShouldBe([42]);
        returned.ShouldBeSameAs(success);
    }

    [Fact]
    [Trait("AC", "AC-1.2")]
    public void OnSuccess_on_a_success_without_a_value_calls_the_action_once_and_returns_the_same_instance()
    {
        Result success = Result.Ok();
        int calls = 0;

        Result returned = success.OnSuccess(() => calls++);

        calls.ShouldBe(1);
        returned.ShouldBeSameAs(success);
    }

    [Fact]
    [Trait("AC", "AC-1.2")]
    public void OnSuccess_without_a_value_on_a_Result_of_T_keeps_its_type()
    {
        Result<int> success = Result.Ok(1);
        int calls = 0;

        Result<int> returned = success.OnSuccess(() => calls++);

        calls.ShouldBe(1);
        returned.ShouldBeSameAs(success);
    }

    [Fact]
    [Trait("AC", "AC-1.3")]
    public void OnSuccess_on_a_failure_does_not_call_the_action_and_returns_the_same_instance()
    {
        Result<int> typed = Result.Fail<int>([_first, _second]);
        Result untyped = Result.Fail(_first);
        int calls = 0;

        Result<int> typedReturned = typed.OnSuccess(_ => calls++);
        Result<int> typedNoValue = typed.OnSuccess(() => calls++);
        Result untypedReturned = untyped.OnSuccess(() => calls++);

        calls.ShouldBe(0);
        typedReturned.ShouldBeSameAs(typed);
        typedReturned.Errors.ShouldBe([_first, _second]);
        typedNoValue.ShouldBeSameAs(typed);
        untypedReturned.ShouldBeSameAs(untyped);
    }

    [Fact]
    [Trait("AC", "AC-1.4")]
    public void OnSuccess_keeps_the_value_type_for_the_next_step()
    {
        int seenByMap = 0;

        Result<string> mapped = Result.Ok(7)
            .OnSuccess(_ => { })
            .Map(v =>
            {
                seenByMap = v;
                return $"quote {v}";
            });

        seenByMap.ShouldBe(7);
        mapped.Value.ShouldBe("quote 7");
    }

    [Fact]
    [Trait("AC", "AC-1.5")]
    public void Steps_run_at_most_once_in_the_order_written()
    {
        var log = new List<string>();

        Result<string> outcome = Result.Ok(1)
            .OnSuccess(v => log.Add($"success {v}"))
            .OnFailure(_ => log.Add("failure"))
            .Map(v =>
            {
                log.Add("map");
                return v + 1;
            })
            .OnSuccess(v => log.Add($"success {v}"))
            .Bind(v =>
            {
                log.Add("bind");
                return Result.Fail<string>(_first);
            })
            .OnSuccess(_ => log.Add("not reached"))
            .OnFailure(errors => log.Add($"failure {errors.Count}"));

        log.ShouldBe(["success 1", "map", "success 2", "bind", "failure 1"]);
        outcome.Errors.ShouldBe([_first]);
    }
}
