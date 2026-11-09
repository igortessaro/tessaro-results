namespace Tessaro;

// Spec 002: async steps on a completed result with a value (same rules as Result.Async.cs).
public sealed partial class Result<T>
{
    /// <summary>
    /// On a success, creates a success holding the value the asynchronous <paramref name="mapper"/>
    /// completes with for this value; on a failure, returns a failure with the same errors without calling it.
    /// </summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="mapper">Transforms the value of the success asynchronously.</param>
    /// <returns>A task that completes with the mapped success, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="mapper"/> returned a <see langword="null"/> task, or a task that completed with <see langword="null"/>.</exception>
    public Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsSuccess ? MapCore() : Task.FromResult(ToFailure<TOut>());

        async Task<Result<TOut>> MapCore() =>
            new(NotNull(await NotNull(mapper(_value), MapperReturnedNullTask).ConfigureAwait(false), MapperReturnedNull));
    }

    /// <summary>
    /// On a success, runs the asynchronous next operation with the value and returns the result it
    /// completes with; on a failure, returns this failure without calling <paramref name="next"/>.
    /// </summary>
    /// <param name="next">The asynchronous operation to run with the value of a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with this failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="next"/> returned a <see langword="null"/> task, or a task that completed with <see langword="null"/>.</exception>
    public Task<Result> BindAsync(Func<T, Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? BindCore() : Task.FromResult<Result>(this);

        async Task<Result> BindCore() =>
            NotNull(await NotNull(next(_value), NextReturnedNullTask).ConfigureAwait(false), NextReturnedNull);
    }

    /// <summary>
    /// On a success, runs the asynchronous next operation with the value and returns the result it
    /// completes with; on a failure, returns a failure with the same errors without calling <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="next">The asynchronous operation to run with the value of a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="next"/> returned a <see langword="null"/> task, or a task that completed with <see langword="null"/>.</exception>
    public Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> next) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? BindCore() : Task.FromResult(ToFailure<TOut>());

        async Task<Result<TOut>> BindCore() =>
            NotNull(await NotNull(next(_value), NextReturnedNullTask).ConfigureAwait(false), NextReturnedNull);
    }

    /// <summary>
    /// Handles both outcomes asynchronously: completes with what <paramref name="onSuccess"/> completes
    /// with for the value on a success, or with what <paramref name="onFailure"/> completes with for
    /// every error on a failure. Only one delegate runs.
    /// </summary>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="onSuccess">Produces the outcome of a success from its value, asynchronously.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors, in order, asynchronously.</param>
    /// <returns>A task that completes with the outcome of the branch that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (thrown by the call itself, on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the branch that ran returned a <see langword="null"/> task.</exception>
    public Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return MatchCore();

        async Task<TOut> MatchCore() =>
            await NotNull(IsSuccess ? onSuccess(_value) : onFailure(Errors), BranchReturnedNullTask).ConfigureAwait(false);
    }
}
