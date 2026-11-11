namespace Tessaro;

/// <summary>
/// <c>OnSuccess</c> and <c>OnFailure</c>: run a side effect on one outcome and pass the same result
/// on, unchanged, so "publish after a success" or "log after a failure" stays in the chain.
/// </summary>
/// <remarks>
/// The steps never transform: they return the very instance they were called on (or that the pending
/// result completed with), keeping its static type — a <see cref="Result{T}"/> stays a
/// <see cref="Result{T}"/>. The action of the other outcome is never called. Every member validates its
/// arguments when called, before any action runs or any task is awaited; exceptions and cancellation of
/// an action propagate unchanged. Asynchronous members end with <c>Async</c> and await the action
/// without resuming on the caller's synchronization context.
/// </remarks>
public static partial class ResultSideEffectExtensions
{
    private const string ActionReturnedNullTask = "The action returned a null task.";

    /// <summary>Runs <paramref name="action"/> if <paramref name="result"/> is a success, then returns <paramref name="result"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The side effect to run after a success.</param>
    /// <returns><paramref name="result"/> itself.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static TResult OnSuccess<TResult>(this TResult result, Action action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>Runs <paramref name="action"/> with the value if <paramref name="result"/> is a success, then returns <paramref name="result"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The side effect to run with the value after a success.</param>
    /// <returns><paramref name="result"/> itself.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>Runs <paramref name="action"/> with every error if <paramref name="result"/> is a failure, then returns <paramref name="result"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The side effect to run with all the errors, in order, after a failure.</param>
    /// <returns><paramref name="result"/> itself.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static TResult OnFailure<TResult>(this TResult result, Action<IReadOnlyList<Error>> action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsFailure)
        {
            action(result.Errors);
        }

        return result;
    }

    /// <summary>Runs and awaits <paramref name="action"/> if <paramref name="result"/> is a success; see <see cref="OnSuccess{TResult}(TResult, Action)"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The asynchronous side effect to run after a success.</param>
    /// <returns>A task that completes with <paramref name="result"/> itself, after <paramref name="action"/> completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="action"/> returned a <see langword="null"/> task.</exception>
    public static Task<TResult> OnSuccessAsync<TResult>(this TResult result, Func<Task> action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return result.IsSuccess ? AfterAction(result, action) : Task.FromResult(result);
    }

    /// <summary>Runs and awaits <paramref name="action"/> with the value if <paramref name="result"/> is a success; see <see cref="OnSuccess{T}(Result{T}, Action{T})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The asynchronous side effect to run with the value after a success.</param>
    /// <returns>A task that completes with <paramref name="result"/> itself, after <paramref name="action"/> completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="action"/> returned a <see langword="null"/> task.</exception>
    public static Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return result.IsSuccess ? AfterAction(result, () => action(result.Value)) : Task.FromResult(result);
    }

    /// <summary>Runs and awaits <paramref name="action"/> with every error if <paramref name="result"/> is a failure; see <see cref="OnFailure{TResult}(TResult, Action{IReadOnlyList{Error}})"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">The asynchronous side effect to run with all the errors, in order, after a failure.</param>
    /// <returns>A task that completes with <paramref name="result"/> itself, after <paramref name="action"/> completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: <paramref name="action"/> returned a <see langword="null"/> task.</exception>
    public static Task<TResult> OnFailureAsync<TResult>(this TResult result, Func<IReadOnlyList<Error>, Task> action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return result.IsFailure ? AfterAction(result, () => action(result.Errors)) : Task.FromResult(result);
    }

    private static async Task<TResult> AfterAction<TResult>(TResult result, Func<Task> action) where TResult : Result
    {
        await (action() ?? throw new InvalidOperationException(ActionReturnedNullTask)).ConfigureAwait(false);
        return result;
    }
}
