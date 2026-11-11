namespace Tessaro;

// Plan 004 §3.2: side-effect steps on a pending result. Each member validates its arguments when
// called, awaits the receiver (null-checked by ResultTaskExtensions.Completed) and delegates to the
// completed-result member — one implementation per step.
public static partial class ResultSideEffectExtensions
{
    /// <summary>Runs <paramref name="action"/> once a pending result completes as a success; see <see cref="OnSuccess{TResult}(TResult, Action)"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">The side effect to run after a success.</param>
    /// <returns>A task that completes with the result the pending result completed with.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>.</exception>
    public static Task<TResult> OnSuccessAsync<TResult>(this Task<TResult> result, Action action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return Core();

        async Task<TResult> Core() => (await ResultTaskExtensions.Completed(result).ConfigureAwait(false)).OnSuccess(action);
    }

    /// <summary>Runs and awaits <paramref name="action"/> once a pending result completes as a success; see <see cref="OnSuccessAsync{TResult}(TResult, Func{Task})"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">The asynchronous side effect to run after a success.</param>
    /// <returns>A task that completes with the result the pending result completed with, after <paramref name="action"/> completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="action"/> returned a <see langword="null"/> task.</exception>
    public static Task<TResult> OnSuccessAsync<TResult>(this Task<TResult> result, Func<Task> action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return Core();

        async Task<TResult> Core() => await (await ResultTaskExtensions.Completed(result).ConfigureAwait(false)).OnSuccessAsync(action).ConfigureAwait(false);
    }

    /// <summary>Runs <paramref name="action"/> with the value once a pending result completes as a success; see <see cref="OnSuccess{T}(Result{T}, Action{T})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">The side effect to run with the value after a success.</param>
    /// <returns>A task that completes with the result the pending result completed with.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>.</exception>
    public static Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> result, Action<T> action) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return Core();

        async Task<Result<T>> Core() => (await ResultTaskExtensions.Completed(result).ConfigureAwait(false)).OnSuccess(action);
    }

    /// <summary>Runs and awaits <paramref name="action"/> with the value once a pending result completes as a success; see <see cref="OnSuccessAsync{T}(Result{T}, Func{T, Task})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">The asynchronous side effect to run with the value after a success.</param>
    /// <returns>A task that completes with the result the pending result completed with, after <paramref name="action"/> completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="action"/> returned a <see langword="null"/> task.</exception>
    public static Task<Result<T>> OnSuccessAsync<T>(this Task<Result<T>> result, Func<T, Task> action) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return Core();

        async Task<Result<T>> Core() => await (await ResultTaskExtensions.Completed(result).ConfigureAwait(false)).OnSuccessAsync(action).ConfigureAwait(false);
    }

    /// <summary>Runs <paramref name="action"/> with every error once a pending result completes as a failure; see <see cref="OnFailure{TResult}(TResult, Action{IReadOnlyList{Error}})"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">The side effect to run with all the errors, in order, after a failure.</param>
    /// <returns>A task that completes with the result the pending result completed with.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>.</exception>
    public static Task<TResult> OnFailureAsync<TResult>(this Task<TResult> result, Action<IReadOnlyList<Error>> action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return Core();

        async Task<TResult> Core() => (await ResultTaskExtensions.Completed(result).ConfigureAwait(false)).OnFailure(action);
    }

    /// <summary>Runs and awaits <paramref name="action"/> with every error once a pending result completes as a failure; see <see cref="OnFailureAsync{TResult}(TResult, Func{IReadOnlyList{Error}, Task})"/>.</summary>
    /// <typeparam name="TResult">The type of the result, kept in the return type.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">The asynchronous side effect to run with all the errors, in order, after a failure.</param>
    /// <returns>A task that completes with the result the pending result completed with, after <paramref name="action"/> completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="action"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="action"/> returned a <see langword="null"/> task.</exception>
    public static Task<TResult> OnFailureAsync<TResult>(this Task<TResult> result, Func<IReadOnlyList<Error>, Task> action) where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        return Core();

        async Task<TResult> Core() => await (await ResultTaskExtensions.Completed(result).ConfigureAwait(false)).OnFailureAsync(action).ConfigureAwait(false);
    }
}
