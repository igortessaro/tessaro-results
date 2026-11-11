namespace Tessaro.Tests;

// Spec 004, US-4 — fail fast and propagate. T01 covers completed receivers; T02 adds pending ones.
[Trait("Spec", "004")]
public class SideEffectFailFastTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");

    public static TheoryData<bool> SuccessOrFailure => [true, false];

    private static Result<int> Typed(bool success) => success ? Result.Ok(1) : Result.Fail<int>(_first);

    private static Result Untyped(bool success) => success ? Result.Ok() : Result.Fail(_first);

    // A throwing method, not a throw-only lambda: a lambda without a return type also fits the
    // Task guard and would not compile (plan 004 §3.4).
    private static void Fail(object _) => throw new FormatException("action failed");

    // --- Completed receivers ------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SuccessOrFailure))]
    [Trait("AC", "AC-4.1")]
    public void Null_actions_throw_at_the_call(bool success)
    {
        Should.Throw<ArgumentNullException>(() => Typed(success).OnSuccess((Action<int>)null!)).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => Typed(success).OnSuccess((Action)null!)).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => Untyped(success).OnSuccess((Action)null!)).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => Typed(success).OnFailure((Action<IReadOnlyList<Error>>)null!)).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => Untyped(success).OnFailure((Action<IReadOnlyList<Error>>)null!)).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => { _ = Typed(success).OnSuccessAsync((Func<int, Task>)null!); }).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).OnSuccessAsync((Func<Task>)null!); }).ParamName.ShouldBe("action");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).OnFailureAsync((Func<IReadOnlyList<Error>, Task>)null!); }).ParamName.ShouldBe("action");
    }

    [Fact]
    [Trait("AC", "AC-4.1")]
    public void A_null_completed_receiver_throws_at_the_call()
    {
        Should.Throw<ArgumentNullException>(() => ((Result<int>)null!).OnSuccess(_ => { })).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => ((Result)null!).OnSuccess(() => { })).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => ((Result)null!).OnFailure(_ => { })).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = ((Result<int>)null!).OnSuccessAsync(_ => Task.CompletedTask); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = ((Result)null!).OnSuccessAsync(() => Task.CompletedTask); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = ((Result)null!).OnFailureAsync(_ => Task.CompletedTask); }).ParamName.ShouldBe("result");
    }

    [Fact]
    [Trait("AC", "AC-4.4")]
    public async Task An_async_action_returning_a_null_task_throws_InvalidOperationException()
    {
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).OnSuccessAsync(_ => null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok().OnSuccessAsync(() => null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Fail(_first).OnFailureAsync(_ => null!))).Message.ShouldContain("null task");
    }

    [Fact]
    [Trait("AC", "AC-4.5")]
    public void An_exception_from_a_sync_action_propagates_and_stops_the_chain()
    {
        int later = 0;

        Should.Throw<FormatException>(() => Result.Ok(1).OnSuccess(v => Fail(v)).OnSuccess(_ => later++)).Message.ShouldBe("action failed");
        Should.Throw<FormatException>(() => Result.Ok().OnSuccess(() => Fail(0)).OnSuccess(() => later++)).Message.ShouldBe("action failed");
        Should.Throw<FormatException>(() => Result.Fail(_first).OnFailure(e => Fail(e)).OnFailure(_ => later++)).Message.ShouldBe("action failed");

        later.ShouldBe(0);
    }

    [Fact]
    [Trait("AC", "AC-4.5")]
    public async Task An_exception_from_an_async_action_propagates_unwrapped()
    {
        (await Should.ThrowAsync<FormatException>(() => Result.Ok(1).OnSuccessAsync(async _ =>
        {
            await Task.Yield();
            throw new FormatException("after yield");
        }))).Message.ShouldBe("after yield");

        (await Should.ThrowAsync<TimeoutException>(() => Result.Ok().OnSuccessAsync(() => Task.FromException(new TimeoutException("faulted")))))
            .Message.ShouldBe("faulted");

        (await Should.ThrowAsync<TimeoutException>(() => Result.Fail(_first).OnFailureAsync(_ => Task.FromException(new TimeoutException("faulted")))))
            .Message.ShouldBe("faulted");
    }

    [Fact]
    [Trait("AC", "AC-4.6")]
    public async Task A_cancelled_async_action_stays_cancelled()
    {
        var cancelled = new CancellationToken(canceled: true);

        Exception typed = await Should.ThrowAsync<TaskCanceledException>(() => Result.Ok(1).OnSuccessAsync(_ => Task.FromCanceled(cancelled)));
        Exception untyped = await Should.ThrowAsync<TaskCanceledException>(() => Result.Fail(_first).OnFailureAsync(_ => Task.FromCanceled(cancelled)));

        typed.ShouldBeAssignableTo<OperationCanceledException>();
        untyped.ShouldBeAssignableTo<OperationCanceledException>();
    }
}
