namespace Tessaro;

// Spec 002: async steps on a completed result. Each member validates its arguments synchronously
// (fail fast, before anything is awaited) and only then runs an async local function; every await
// uses ConfigureAwait(false). A failure returns an already completed task without calling the step.
public partial class Result
{
    private protected const string MapperReturnedNullTask = "The mapper returned a null task.";

    private protected const string NextReturnedNullTask = "The next operation returned a null task.";

    private protected const string BranchReturnedNullTask = "The branch returned a null task.";

    /// <summary>
    /// On a success, creates a success holding the value the asynchronous <paramref name="mapper"/>
    /// completes with; on a failure, returns a failure with the same errors without calling it.
    /// </summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="mapper">Produces the value of the new success asynchronously.</param>
    /// <returns>A task that completes with the mapped success, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="mapper"/> returned a <see langword="null"/> task, or a task that completed with <see langword="null"/>.</exception>
    public Task<Result<TOut>> MapAsync<TOut>(Func<Task<TOut>> mapper) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsSuccess ? MapCore() : Task.FromResult(ToFailure<TOut>());

        async Task<Result<TOut>> MapCore() =>
            new(NotNull(await NotNull(mapper(), MapperReturnedNullTask).ConfigureAwait(false), MapperReturnedNull));
    }

    /// <summary>
    /// On a success, runs the asynchronous next operation and returns the result it completes with;
    /// on a failure, returns this failure without calling <paramref name="next"/>.
    /// </summary>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with this failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="next"/> returned a <see langword="null"/> task, or a task that completed with <see langword="null"/>.</exception>
    public Task<Result> BindAsync(Func<Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? BindCore() : Task.FromResult(this);

        async Task<Result> BindCore() =>
            NotNull(await NotNull(next(), NextReturnedNullTask).ConfigureAwait(false), NextReturnedNull);
    }

    /// <summary>
    /// On a success, runs the asynchronous next operation and returns the result it completes with;
    /// on a failure, returns a failure with the same errors without calling <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="next"/> returned a <see langword="null"/> task, or a task that completed with <see langword="null"/>.</exception>
    public Task<Result<TOut>> BindAsync<TOut>(Func<Task<Result<TOut>>> next) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? BindCore() : Task.FromResult(ToFailure<TOut>());

        async Task<Result<TOut>> BindCore() =>
            NotNull(await NotNull(next(), NextReturnedNullTask).ConfigureAwait(false), NextReturnedNull);
    }

    /// <summary>
    /// Handles both outcomes asynchronously: completes with what <paramref name="onSuccess"/>()
    /// completes with on a success, or with what <paramref name="onFailure"/> completes with for every
    /// error on a failure. Only one delegate runs.
    /// </summary>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Produces the outcome of a success asynchronously.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors, in order, asynchronously.</param>
    /// <returns>A task that completes with the outcome of the branch that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the branch that ran returned a <see langword="null"/> task.</exception>
    public Task<TOut> MatchAsync<TOut>(Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return MatchCore();

        async Task<TOut> MatchCore() =>
            await NotNull(IsSuccess ? onSuccess() : onFailure(_errors), BranchReturnedNullTask).ConfigureAwait(false);
    }
}
