namespace Tessaro.Tests;

// Spec 004, US-3 — asynchronous side effects. T01 covers completed receivers; T02 adds pending ones.
[Trait("Spec", "004")]
public class AsyncSideEffectTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");
    private static readonly Error _second = Error.Validation("quote.status", "Status is invalid");

    private sealed class Calls
    {
        public List<object> Seen { get; } = [];

        public async Task Track(object argument)
        {
            Seen.Add(argument);
            await Task.Yield();
        }
    }

    // --- Completed receivers ------------------------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-3.1")]
    public async Task OnSuccessAsync_on_a_completed_success_calls_the_async_action_with_the_value()
    {
        var calls = new Calls();
        Result<int> success = Result.Ok(42);

        Result<int> returned = await success.OnSuccessAsync(v => calls.Track(v));

        calls.Seen.ShouldBe([42]);
        returned.ShouldBeSameAs(success);
    }

    [Fact]
    [Trait("AC", "AC-3.3")]
    public async Task OnFailureAsync_on_a_completed_failure_calls_the_async_action_with_every_error()
    {
        var calls = new Calls();
        Result<int> typed = Result.Fail<int>([_first, _second]);
        Result untyped = Result.Fail([_first, _second]);

        Result<int> typedReturned = await typed.OnFailureAsync(errors => calls.Track(errors));
        Result untypedReturned = await untyped.OnFailureAsync(errors => calls.Track(errors));

        calls.Seen.Count.ShouldBe(2);
        calls.Seen.ShouldAllBe(seen => ((IReadOnlyList<Error>)seen).SequenceEqual(new[] { _first, _second }));
        typedReturned.ShouldBeSameAs(typed);
        untypedReturned.ShouldBeSameAs(untyped);
    }

    [Fact]
    [Trait("AC", "AC-3.4")]
    public async Task Async_actions_of_the_other_branch_are_not_called_on_completed_results()
    {
        var calls = new Calls();
        Result<int> failure = Result.Fail<int>(_first);
        Result<int> success = Result.Ok(1);
        Result untypedFailure = Result.Fail(_first);
        Result untypedSuccess = Result.Ok();

        (await failure.OnSuccessAsync(v => calls.Track(v))).ShouldBeSameAs(failure);
        (await failure.OnSuccessAsync(() => calls.Track("none"))).ShouldBeSameAs(failure);
        (await untypedFailure.OnSuccessAsync(() => calls.Track("none"))).ShouldBeSameAs(untypedFailure);
        (await success.OnFailureAsync(errors => calls.Track(errors))).ShouldBeSameAs(success);
        (await untypedSuccess.OnFailureAsync(errors => calls.Track(errors))).ShouldBeSameAs(untypedSuccess);

        calls.Seen.ShouldBeEmpty();
    }

    [Fact]
    [Trait("AC", "AC-3.5")]
    public async Task OnSuccessAsync_without_a_value_on_a_completed_success()
    {
        var calls = new Calls();
        Result untyped = Result.Ok();
        Result<int> typed = Result.Ok(1);

        Result untypedReturned = await untyped.OnSuccessAsync(() => calls.Track("untyped"));
        Result<int> typedReturned = await typed.OnSuccessAsync(() => calls.Track("typed"));

        calls.Seen.ShouldBe(["untyped", "typed"]);
        untypedReturned.ShouldBeSameAs(untyped);
        typedReturned.ShouldBeSameAs(typed);
    }

    [Fact]
    [Trait("AC", "AC-3.6")]
    public async Task Async_actions_on_completed_results_are_awaited()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<Result<int>> onSuccess = Result.Ok(1).OnSuccessAsync(_ => gate.Task);
        Task<Result> onSuccessNoValue = Result.Ok().OnSuccessAsync(() => gate.Task);
        Task<Result> onFailure = Result.Fail(_first).OnFailureAsync(_ => gate.Task);

        onSuccess.IsCompleted.ShouldBeFalse();
        onSuccessNoValue.IsCompleted.ShouldBeFalse();
        onFailure.IsCompleted.ShouldBeFalse();

        gate.SetResult();

        (await onSuccess).Value.ShouldBe(1);
        (await onSuccessNoValue).IsSuccess.ShouldBeTrue();
        (await onFailure).Errors.ShouldBe([_first]);
    }
}
