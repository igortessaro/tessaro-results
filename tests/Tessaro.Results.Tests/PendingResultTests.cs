namespace Tessaro.Tests;

// Spec 002, US-1 — ordinary (synchronous) steps on a pending result.
[Trait("Spec", "002")]
public class PendingResultTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");
    private static readonly Error _downstream = Error.Conflict("quote.locked", "Quote is locked");

    // A result that is still pending when the member is called.
    private static async Task<TResult> Pending<TResult>(TResult result)
    {
        await Task.Yield();
        return result;
    }

    private static Task<Result<int>> PendingFailure() => Pending(Result.Fail<int>([_first, _second]));

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
    [Trait("AC", "AC-1.1")]
    public async Task MapAsync_on_a_pending_success_applies_a_synchronous_mapper()
    {
        (await Pending(Result.Ok(21)).MapAsync(v => v * 2)).Value.ShouldBe(42);
        (await Pending(Result.Ok("quote")).MapAsync(Describe)).Value.ShouldBe("quote!");
    }

    [Fact]
    [Trait("AC", "AC-1.2")]
    public async Task BindAsync_on_a_pending_success_returns_the_synchronous_step_result()
    {
        Result next = Result.Fail(_downstream);

        (await Pending(Result.Ok(7)).BindAsync(v => Result.Ok($"quote {v}"))).Value.ShouldBe("quote 7");
        (await Pending(Result.Ok(7)).BindAsync(_ => Result.Fail<string>(_downstream))).Errors.ShouldBe([_downstream]);
        (await Pending(Result.Ok(7)).BindAsync(_ => next)).ShouldBeSameAs(next);
    }

    [Fact]
    [Trait("AC", "AC-1.3")]
    public async Task MatchAsync_on_a_pending_success_runs_only_onSuccess()
    {
        var calls = new Calls();

        (await Pending(Result.Ok(42)).MatchAsync(v => $"value {v}", _ => calls.Track("failure"))).ShouldBe("value 42");
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-1.4")]
    public async Task Synchronous_steps_on_a_pending_failure_keep_every_error_and_are_not_called()
    {
        var calls = new Calls();

        (await PendingFailure().MapAsync(v => calls.Track(v + 1))).Errors.ShouldBe([_first, _second]);
        (await PendingFailure().BindAsync(v => calls.Track(Result.Ok("x")))).Errors.ShouldBe([_first, _second]);
        (await PendingFailure().BindAsync(v => calls.Track(Result.Ok()))).Errors.ShouldBe([_first, _second]);
        (await PendingFailure().MatchAsync(v => calls.Track(-1), errors => errors.Count)).ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-1.5")]
    public async Task Synchronous_steps_on_a_pending_success_without_value()
    {
        Result next = Result.Fail(_downstream);

        (await Pending(Result.Ok()).MapAsync(() => 42)).Value.ShouldBe(42);
        (await Pending(Result.Ok()).BindAsync(() => next)).ShouldBeSameAs(next);
        (await Pending(Result.Ok()).BindAsync(() => Result.Ok(5))).Value.ShouldBe(5);
        (await Pending(Result.Ok()).MatchAsync(() => "ok", _ => "ko")).ShouldBe("ok");
    }

    [Fact]
    [Trait("AC", "AC-1.5")]
    public async Task Synchronous_steps_on_a_pending_failure_without_value_keep_every_error()
    {
        var calls = new Calls();
        Result failed = Result.Fail([_first, _second]);

        (await Pending(failed).MapAsync(() => calls.Track(42))).Errors.ShouldBe([_first, _second]);
        (await Pending(failed).BindAsync(() => calls.Track(Result.Ok()))).ShouldBeSameAs(failed);
        (await Pending(failed).BindAsync(() => calls.Track(Result.Ok(5)))).Errors.ShouldBe([_first, _second]);
        (await Pending(failed).MatchAsync(() => calls.Track(-1), errors => errors.Count)).ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    private static string Describe(string value) => value + "!";
}
