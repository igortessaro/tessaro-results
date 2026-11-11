using System.ComponentModel;
using System.Reflection;

namespace Tessaro.Tests;

// Spec 004, US-5 — compile-time guards. The CS0619 at call sites is shown by each task's consumer
// check; these tests pin the guards themselves. T01 covers completed receivers; T02 adds pending ones.
[Trait("Spec", "004")]
public class SideEffectGuardTests
{
    private static IEnumerable<MethodInfo> Guards(string name) =>
        typeof(ResultSideEffectExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == name && m.GetCustomAttribute<ObsoleteAttribute>() is not null);

    private static bool OnCompletedReceiver(MethodInfo method) =>
        method.GetParameters()[0].ParameterType is not { IsGenericType: true } receiver
        || receiver.GetGenericTypeDefinition() != typeof(Task<>);

    private static Type ReturnOfAction(MethodInfo method) =>
        method.GetParameters()[1].ParameterType.GetGenericArguments()[^1];

    private static void AssertGuard(MethodInfo guard, string pointsTo)
    {
        ObsoleteAttribute obsolete = guard.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
        obsolete.IsError.ShouldBeTrue();
        obsolete.Message.ShouldNotBeNull().ShouldContain(pointsTo);
        guard.GetCustomAttribute<EditorBrowsableAttribute>().ShouldNotBeNull().State.ShouldBe(EditorBrowsableState.Never);

        // Only reachable through reflection; then it refuses to run.
        Type[] arguments = [.. guard.GetGenericArguments().Select(a => a.Name switch
        {
            "T" => typeof(int),
            "TOut" => typeof(Result),
            _ => typeof(Result<int>),
        })];
        object receiver = OnCompletedReceiver(guard) ? Result.Ok(1) : Task.FromResult(Result.Ok(1));
        TargetInvocationException ex = Should.Throw<TargetInvocationException>(
            () => guard.MakeGenericMethod(arguments).Invoke(null, [receiver, null]));
        ex.InnerException.ShouldBeOfType<NotSupportedException>().Message.ShouldContain(pointsTo);
    }

    [Theory]
    [InlineData("OnSuccess", "OnSuccessAsync", 2)]
    [InlineData("OnFailure", "OnFailureAsync", 1)]
    [Trait("AC", "AC-5.1")]
    public void The_synchronous_steps_refuse_an_action_that_returns_a_Task(string step, string pointsTo, int shapes)
    {
        MethodInfo[] guards = [.. Guards(step).Where(m => ReturnOfAction(m) == typeof(Task))];

        guards.Length.ShouldBe(shapes); // with and without the value (OnSuccess), errors (OnFailure)
        foreach (MethodInfo guard in guards)
        {
            AssertGuard(guard, pointsTo);
        }
    }

    [Theory]
    [InlineData("OnSuccess", "use Bind")]
    [InlineData("OnSuccessAsync", "use BindAsync")]
    [Trait("AC", "AC-5.3")]
    public void Success_steps_on_a_completed_result_refuse_an_action_that_returns_a_result(string step, string pointsTo)
    {
        MethodInfo[] guards = [.. Guards(step).Where(OnCompletedReceiver).Where(m => ReturnOfAction(m) != typeof(Task))];

        guards.Length.ShouldBe(2); // with and without the value
        foreach (MethodInfo guard in guards)
        {
            AssertGuard(guard, pointsTo);
        }
    }

    // --- Pending receivers (T02) --------------------------------------------------------------------

    [Fact]
    [Trait("AC", "AC-5.3")]
    public void OnSuccessAsync_on_a_pending_result_refuses_an_action_that_returns_a_result()
    {
        MethodInfo[] guards = [.. Guards("OnSuccessAsync").Where(m => !OnCompletedReceiver(m))];

        guards.Length.ShouldBe(4); // with and without the value, sync and async action
        foreach (MethodInfo guard in guards)
        {
            AssertGuard(guard, "use BindAsync");
        }
    }

    private static Task Gate(TaskCompletionSource gate, List<string> log, string entry)
    {
        log.Add(entry);
        return gate.Task;
    }

    [Fact]
    [Trait("AC", "AC-5.2")]
    public async Task A_Task_returning_lambda_on_a_pending_result_is_awaited_not_discarded()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var log = new List<string>();

        // Expression lambdas returning a Task: they also convert to Action, which would discard the task.
        Task<Result<int>> onSuccess = Task.FromResult(Result.Ok(1)).OnSuccessAsync(v => Gate(gate, log, $"success {v}"));
        Task<Result> onSuccessNoValue = Task.FromResult(Result.Ok()).OnSuccessAsync(() => Gate(gate, log, "success"));
        Task<Result> onFailure = Task.FromResult(Result.Fail(Error.Conflict("c", "C"))).OnFailureAsync(e => Gate(gate, log, $"failure {e.Count}"));

        log.ShouldBe(["success 1", "success", "failure 1"], ignoreOrder: true);
        onSuccess.IsCompleted.ShouldBeFalse();
        onSuccessNoValue.IsCompleted.ShouldBeFalse();
        onFailure.IsCompleted.ShouldBeFalse();

        gate.SetResult();
        await Task.WhenAll(onSuccess, onSuccessNoValue, onFailure);
    }
}
