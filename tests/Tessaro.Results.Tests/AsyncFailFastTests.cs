using System.ComponentModel;
using System.Reflection;

namespace Tessaro.Tests;

// Spec 002, US-3 — fail fast and propagate. T01 covers completed receivers and AC-3.7; T02 adds pending ones.
[Trait("Spec", "002")]
public class AsyncFailFastTests
{
    private static readonly Error _first = Error.NotFound("quote.not_found", "Quote 42 was not found");

    public static TheoryData<bool> SuccessOrFailure => [true, false];

    private static Result<int> Typed(bool success) => success ? Result.Ok(1) : Result.Fail<int>(_first);

    private static Result Untyped(bool success) => success ? Result.Ok() : Result.Fail(_first);

    // AC-3.1: the exception comes from the call itself (an Action, not an awaited task).
    [Theory]
    [MemberData(nameof(SuccessOrFailure))]
    [Trait("AC", "AC-3.1")]
    public void Null_async_delegates_throw_at_the_call(bool success)
    {
        Should.Throw<ArgumentNullException>(() => { _ = Typed(success).MapAsync((Func<int, Task<string>>)null!); }).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => { _ = Typed(success).BindAsync((Func<int, Task<Result<string>>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Typed(success).BindAsync((Func<int, Task<Result>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Typed(success).MatchAsync(null!, _ => Task.FromResult(0)); }).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => { _ = Typed(success).MatchAsync(v => Task.FromResult(v), null!); }).ParamName.ShouldBe("onFailure");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).MapAsync((Func<Task<string>>)null!); }).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).BindAsync((Func<Task<Result<string>>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).BindAsync((Func<Task<Result>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).MatchAsync((Func<Task<int>>)null!, _ => Task.FromResult(0)); }).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => { _ = Untyped(success).MatchAsync(() => Task.FromResult(0), null!); }).ParamName.ShouldBe("onFailure");
    }

    [Fact]
    [Trait("AC", "AC-3.4")]
    public async Task A_step_returning_a_null_task_throws_InvalidOperationException()
    {
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).MapAsync<string>(_ => null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).BindAsync<string>(_ => null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).BindAsync(_ => (Task<Result>)null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).MatchAsync<int>(_ => null!, _ => Task.FromResult(0)))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Fail<int>(_first).MatchAsync(Task.FromResult, _ => (Task<int>)null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok().MapAsync<string>(() => null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok().BindAsync(() => (Task<Result>)null!))).Message.ShouldContain("null task");
    }

    [Fact]
    [Trait("AC", "AC-3.4")]
    public async Task A_step_completing_with_null_throws_InvalidOperationException()
    {
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).MapAsync(_ => Task.FromResult<string>(null!)))).Message.ShouldContain("mapper returned null");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).BindAsync(_ => Task.FromResult<Result<string>>(null!)))).Message.ShouldContain("returned a null result");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok(1).BindAsync(_ => Task.FromResult<Result>(null!)))).Message.ShouldContain("returned a null result");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok().MapAsync(() => Task.FromResult<string>(null!)))).Message.ShouldContain("mapper returned null");
        (await Should.ThrowAsync<InvalidOperationException>(() => Result.Ok().BindAsync(() => Task.FromResult<Result<string>>(null!)))).Message.ShouldContain("returned a null result");
    }

    [Fact]
    [Trait("AC", "AC-3.5")]
    public async Task An_exception_from_a_step_propagates_unwrapped()
    {
        (await Should.ThrowAsync<FormatException>(() => Result.Ok(1).MapAsync<string>(async _ =>
        {
            await Task.Yield();
            throw new FormatException("mapper failed");
        }))).Message.ShouldBe("mapper failed");

        (await Should.ThrowAsync<FormatException>(() => Result.Ok().BindAsync(() => throw new FormatException("thrown before any task"))))
            .Message.ShouldBe("thrown before any task");

        (await Should.ThrowAsync<TimeoutException>(() => Result.Ok(1).MatchAsync<int>(_ => Task.FromException<int>(new TimeoutException("branch")), _ => Task.FromResult(0))))
            .Message.ShouldBe("branch");
    }

    [Fact]
    [Trait("AC", "AC-3.6")]
    public async Task A_cancelled_step_stays_cancelled()
    {
        var cancelled = new CancellationToken(canceled: true);

        Exception ex = await Should.ThrowAsync<TaskCanceledException>(() => Result.Ok(1).BindAsync(_ => Task.FromCanceled<Result<string>>(cancelled)));

        ex.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Theory]
    [InlineData(typeof(Result))]
    [InlineData(typeof(Result<int>))]
    [Trait("AC", "AC-3.7")]
    public void The_synchronous_Map_refuses_a_mapper_that_returns_a_Task(Type type)
    {
        MethodInfo guard = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == "Map"
                && m.GetParameters().Single().ParameterType.GetGenericArguments()[^1] is { IsGenericType: true } ret
                && ret.GetGenericTypeDefinition() == typeof(Task<>));

        ObsoleteAttribute obsolete = guard.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
        obsolete.IsError.ShouldBeTrue();
        obsolete.Message.ShouldNotBeNull().ShouldContain("MapAsync");
        guard.GetCustomAttribute<EditorBrowsableAttribute>().ShouldNotBeNull().State.ShouldBe(EditorBrowsableState.Never);

        // Only reachable through reflection; then it refuses to run.
        object receiver = type == typeof(Result) ? Result.Ok() : Result.Ok(1);
        TargetInvocationException ex = Should.Throw<TargetInvocationException>(
            () => guard.MakeGenericMethod(typeof(string)).Invoke(receiver, [null]));
        ex.InnerException.ShouldBeOfType<NotSupportedException>().Message.ShouldContain("MapAsync");
    }

    // --- Pending receivers (T02) --------------------------------------------------------------------

    private static Task<Result<int>> Never() => new TaskCompletionSource<Result<int>>().Task;

    private static Task<Result> NeverUntyped() => new TaskCompletionSource<Result>().Task;

    [Fact]
    [Trait("AC", "AC-3.1")]
    public void Null_delegates_throw_at_the_call_on_a_pending_result_without_awaiting_it()
    {
        // The receivers never complete: a member that awaited before validating would hang, not throw.
        Should.Throw<ArgumentNullException>(() => { _ = Never().MapAsync((Func<int, string>)null!); }).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => { _ = Never().MapAsync((Func<int, Task<string>>)null!); }).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => { _ = Never().BindAsync((Func<int, Result<string>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Never().BindAsync((Func<int, Task<Result<string>>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Never().BindAsync((Func<int, Result>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Never().BindAsync((Func<int, Task<Result>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = Never().MatchAsync((Func<int, int>)null!, _ => 0); }).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => { _ = Never().MatchAsync(v => v, (Func<IReadOnlyList<Error>, int>)null!); }).ParamName.ShouldBe("onFailure");
        Should.Throw<ArgumentNullException>(() => { _ = Never().MatchAsync((Func<int, Task<int>>)null!, _ => Task.FromResult(0)); }).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => { _ = Never().MatchAsync(Task.FromResult, (Func<IReadOnlyList<Error>, Task<int>>)null!); }).ParamName.ShouldBe("onFailure");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().MapAsync((Func<string>)null!); }).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().MapAsync((Func<Task<string>>)null!); }).ParamName.ShouldBe("mapper");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().BindAsync((Func<Result<string>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().BindAsync((Func<Task<Result<string>>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().BindAsync((Func<Result>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().BindAsync((Func<Task<Result>>)null!); }).ParamName.ShouldBe("next");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().MatchAsync((Func<int>)null!, _ => 0); }).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().MatchAsync(() => 0, (Func<IReadOnlyList<Error>, int>)null!); }).ParamName.ShouldBe("onFailure");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().MatchAsync((Func<Task<int>>)null!, _ => Task.FromResult(0)); }).ParamName.ShouldBe("onSuccess");
        Should.Throw<ArgumentNullException>(() => { _ = NeverUntyped().MatchAsync(() => Task.FromResult(0), (Func<IReadOnlyList<Error>, Task<int>>)null!); }).ParamName.ShouldBe("onFailure");
    }

    [Fact]
    [Trait("AC", "AC-3.2")]
    public void A_null_pending_result_throws_at_the_call()
    {
        Task<Result<int>> typed = null!;
        Task<Result> untyped = null!;

        Should.Throw<ArgumentNullException>(() => { _ = typed.MapAsync(v => v); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.MapAsync(Task.FromResult); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.BindAsync(v => Result.Ok(v)); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.BindAsync(v => Task.FromResult(Result.Ok(v))); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.BindAsync(_ => Result.Ok()); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.BindAsync(_ => Task.FromResult(Result.Ok())); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.MatchAsync(v => v, _ => 0); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = typed.MatchAsync(Task.FromResult, _ => Task.FromResult(0)); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.MapAsync(() => 1); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.MapAsync(() => Task.FromResult(1)); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.BindAsync(() => Result.Ok(1)); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.BindAsync(() => Task.FromResult(Result.Ok(1))); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.BindAsync(() => Result.Ok()); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.BindAsync(() => Task.FromResult(Result.Ok())); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.MatchAsync(() => 0, _ => 0); }).ParamName.ShouldBe("result");
        Should.Throw<ArgumentNullException>(() => { _ = untyped.MatchAsync(() => Task.FromResult(0), _ => Task.FromResult(0)); }).ParamName.ShouldBe("result");
    }

    [Fact]
    [Trait("AC", "AC-3.3")]
    public async Task A_pending_result_completing_with_null_throws_InvalidOperationException()
    {
        const string Expected = "The pending result completed with null.";

        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult<Result<int>>(null!).MapAsync(v => v))).Message.ShouldBe(Expected);
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult<Result<int>>(null!).BindAsync(v => Task.FromResult(Result.Ok(v))))).Message.ShouldBe(Expected);
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult<Result<int>>(null!).MatchAsync(v => v, _ => 0))).Message.ShouldBe(Expected);
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult<Result>(null!).BindAsync(() => Result.Ok()))).Message.ShouldBe(Expected);
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult<Result>(null!).MatchAsync(() => Task.FromResult(0), _ => Task.FromResult(0)))).Message.ShouldBe(Expected);
    }

    [Fact]
    [Trait("AC", "AC-3.4")]
    public async Task An_async_step_returning_a_null_task_on_a_pending_success_throws_InvalidOperationException()
    {
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult(Result.Ok(1)).MapAsync<int, string>(_ => (Task<string>)null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult(Result.Ok(1)).BindAsync(_ => (Task<Result>)null!))).Message.ShouldContain("null task");
        (await Should.ThrowAsync<InvalidOperationException>(() => Task.FromResult(Result.Ok()).MapAsync(() => Task.FromResult<string>(null!)))).Message.ShouldContain("mapper returned null");
    }

    [Fact]
    [Trait("AC", "AC-3.5")]
    public async Task A_faulted_pending_result_or_step_propagates_its_exception_unwrapped()
    {
        (await Should.ThrowAsync<TimeoutException>(() => Task.FromException<Result<int>>(new TimeoutException("db")).MapAsync(v => v)))
            .Message.ShouldBe("db");
        (await Should.ThrowAsync<FormatException>(() => Task.FromResult(Result.Ok(1)).BindAsync<int, int>(async _ =>
        {
            await Task.Yield();
            throw new FormatException("step");
        }))).Message.ShouldBe("step");
        (await Should.ThrowAsync<TimeoutException>(() => Task.FromException<Result>(new TimeoutException("db")).MatchAsync(() => 1, _ => 0)))
            .Message.ShouldBe("db");
    }

    [Fact]
    [Trait("AC", "AC-3.6")]
    public async Task A_cancelled_pending_result_stays_cancelled()
    {
        var cancelled = new CancellationToken(canceled: true);

        (await Should.ThrowAsync<TaskCanceledException>(() => Task.FromCanceled<Result<int>>(cancelled).BindAsync(v => Result.Ok(v))))
            .ShouldBeAssignableTo<OperationCanceledException>();
        (await Should.ThrowAsync<TaskCanceledException>(() => Task.FromCanceled<Result>(cancelled).MapAsync(() => 1)))
            .ShouldBeAssignableTo<OperationCanceledException>();
    }
}
