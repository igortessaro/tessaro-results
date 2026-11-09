namespace Tessaro.Tests;

// Spec 002, US-2 — async steps. T01 covers completed receivers; T02 adds pending ones.
[Trait("Spec", "002")]
public class AsyncStepTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");
    private static readonly Error _downstream = Error.Conflict("quote.locked", "Quote is locked");

    private sealed class Calls
    {
        public int Count { get; private set; }

        public async Task<TOut> Track<TOut>(TOut value)
        {
            Count++;
            await Task.Yield();
            return value;
        }
    }

    private static async Task<TOut> Later<TOut>(TOut value)
    {
        await Task.Yield();
        return value;
    }

    // --- Completed success with a value --------------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-2.1")]
    public async Task MapAsync_on_a_completed_success_holds_what_the_mapper_completes_with() =>
        (await Result.Ok(21).MapAsync(v => Later(v * 2))).Value.ShouldBe(42);

    [Fact]
    [Trait("AC", "AC-2.2")]
    public async Task BindAsync_on_a_completed_success_returns_the_result_the_step_completes_with()
    {
        (await Result.Ok(7).BindAsync(v => Later(Result.Ok($"quote {v}")))).Value.ShouldBe("quote 7");
        (await Result.Ok(7).BindAsync(_ => Later(Result.Fail<string>(_downstream)))).Errors.ShouldBe([_downstream]);

        Result next = Result.Fail(_downstream);
        (await Result.Ok(7).BindAsync(_ => Later(next))).ShouldBeSameAs(next);
    }

    [Fact]
    [Trait("AC", "AC-2.3")]
    public async Task MatchAsync_on_a_completed_success_runs_only_onSuccess()
    {
        var calls = new Calls();

        string text = await Result.Ok(42).MatchAsync(v => Later($"value {v}"), _ => calls.Track("failure"));

        text.ShouldBe("value 42");
        calls.Count.ShouldBe(0);
    }

    // --- Completed failure ---------------------------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-2.4")]
    public async Task Async_steps_on_a_completed_failure_keep_every_error_and_are_not_called()
    {
        var calls = new Calls();
        Result<int> failed = Result.Fail<int>([_first, _second]);

        (await failed.MapAsync(v => calls.Track(v.ToString(System.Globalization.CultureInfo.InvariantCulture)))).Errors.ShouldBe([_first, _second]);
        (await failed.BindAsync(v => calls.Track(Result.Ok("x")))).Errors.ShouldBe([_first, _second]);
        (await failed.BindAsync(v => calls.Track(Result.Ok()))).ShouldBeSameAs(failed);
        (await failed.MatchAsync(v => calls.Track(-1), errors => Later(errors.Count))).ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    // --- Completed results without a value ----------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-2.5")]
    public async Task Async_steps_without_value_on_a_completed_success()
    {
        Result next = Result.Fail(_downstream);

        (await Result.Ok().MapAsync(() => Later(42))).Value.ShouldBe(42);
        (await Result.Ok().BindAsync(() => Later(next))).ShouldBeSameAs(next);
        (await Result.Ok().BindAsync(() => Later(Result.Ok(5)))).Value.ShouldBe(5);
        (await Result.Ok().MatchAsync(() => Later("ok"), _ => Later("ko"))).ShouldBe("ok");
    }

    [Fact]
    [Trait("AC", "AC-2.5")]
    public async Task Async_steps_without_value_on_a_completed_failure_keep_every_error_and_are_not_called()
    {
        var calls = new Calls();
        Result failed = Result.Fail([_first, _second]);

        (await failed.MapAsync(() => calls.Track(42))).Errors.ShouldBe([_first, _second]);
        (await failed.BindAsync(() => calls.Track(Result.Ok()))).ShouldBeSameAs(failed);
        (await failed.BindAsync(() => calls.Track(Result.Ok(5)))).Errors.ShouldBe([_first, _second]);
        (await failed.MatchAsync(() => calls.Track(-1), errors => Later(errors.Count))).ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-2.4")]
    public void A_completed_failure_returns_an_already_completed_task()
    {
        var calls = new Calls();

        Task<Result<string>> mapped = Result.Fail<int>(_first).MapAsync(v => calls.Track("x"));

        mapped.IsCompletedSuccessfully.ShouldBeTrue();
        calls.Count.ShouldBe(0);
    }

    // --- Pending receivers (T02) --------------------------------------------------------------------

    private static async Task<TResult> Pending<TResult>(TResult result)
    {
        await Task.Yield();
        return result;
    }

    [Fact]
    [Trait("AC", "AC-2.1")]
    public async Task MapAsync_on_a_pending_success_holds_what_the_async_mapper_completes_with() =>
        (await Pending(Result.Ok(21)).MapAsync(v => Later(v * 2))).Value.ShouldBe(42);

    [Fact]
    [Trait("AC", "AC-2.2")]
    public async Task BindAsync_on_a_pending_success_returns_the_result_the_async_step_completes_with()
    {
        Result next = Result.Fail(_downstream);

        (await Pending(Result.Ok(7)).BindAsync(v => Later(Result.Ok($"quote {v}")))).Value.ShouldBe("quote 7");
        (await Pending(Result.Ok(7)).BindAsync(_ => Later(next))).ShouldBeSameAs(next);
    }

    [Fact]
    [Trait("AC", "AC-2.3")]
    public async Task MatchAsync_on_a_pending_success_runs_only_the_async_onSuccess()
    {
        var calls = new Calls();

        (await Pending(Result.Ok(42)).MatchAsync(v => Later($"value {v}"), _ => calls.Track("failure"))).ShouldBe("value 42");
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-2.4")]
    public async Task Async_steps_on_a_pending_failure_keep_every_error_and_are_not_called()
    {
        var calls = new Calls();
        Result<int> failed = Result.Fail<int>([_first, _second]);

        (await Pending(failed).MapAsync(v => calls.Track(v + 1))).Errors.ShouldBe([_first, _second]);
        (await Pending(failed).BindAsync(v => calls.Track(Result.Ok("x")))).Errors.ShouldBe([_first, _second]);
        (await Pending(failed).BindAsync(v => calls.Track(Result.Ok()))).ShouldBeSameAs(failed);
        (await Pending(failed).MatchAsync(v => calls.Track(-1), errors => Later(errors.Count))).ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-2.5")]
    public async Task Async_steps_on_a_pending_result_without_value()
    {
        var calls = new Calls();
        Result failed = Result.Fail([_first, _second]);

        (await Pending(Result.Ok()).MapAsync(() => Later(42))).Value.ShouldBe(42);
        (await Pending(Result.Ok()).BindAsync(() => Later(Result.Ok(5)))).Value.ShouldBe(5);
        (await Pending(Result.Ok()).BindAsync(() => Later(failed))).ShouldBeSameAs(failed);
        (await Pending(Result.Ok()).MatchAsync(() => Later("ok"), _ => Later("ko"))).ShouldBe("ok");
        (await Pending(failed).MapAsync(() => calls.Track(1))).Errors.ShouldBe([_first, _second]);
        (await Pending(failed).BindAsync(() => calls.Track(Result.Ok()))).ShouldBeSameAs(failed);
        (await Pending(failed).MatchAsync(() => calls.Track(-1), errors => Later(errors.Count))).ShouldBe(2);
        calls.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-2.6")]
    public async Task A_chain_stops_at_the_first_failing_step()
    {
        var after = new Calls();

        Result<string> chained = await Pending(Result.Ok(1))
            .BindAsync(v => Later(Result.Ok(v + 1)))                 // step 1: async, succeeds
            .BindAsync(v => Result.Fail<int>(_downstream))           // step 2: sync, fails
            .BindAsync(v => after.Track(Result.Ok(v * 10)))          // step 3: must not run
            .MapAsync(v => after.Track($"value {v}"));               // step 4: must not run

        chained.Errors.ShouldBe([_downstream]);
        after.Count.ShouldBe(0);
    }
}
