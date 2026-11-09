namespace Tessaro;

// Spec 002: the same eight members as ResultTaskExtensions.cs, on a pending result with a value.
public static partial class ResultTaskExtensions
{
    /// <summary>Maps the value of a pending success with a synchronous <paramref name="mapper"/>; see <see cref="Result{T}.Map{TOut}(Func{T, TOut})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="mapper">Produces the value of the new success.</param>
    /// <returns>A task that completes with the mapped success, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="mapper"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="mapper"/> returned <see langword="null"/>.</exception>
    public static Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> result, Func<T, TOut> mapper) where T : notnull where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return Core();

        async Task<Result<TOut>> Core() => (await Completed(result).ConfigureAwait(false)).Map(mapper);
    }

    /// <summary>Maps a pending success with an asynchronous <paramref name="mapper"/>; see <see cref="Result{T}.MapAsync{TOut}(Func{T, Task{TOut}})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="mapper">Produces the value of the new success asynchronously.</param>
    /// <returns>A task that completes with the mapped success, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="mapper"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="mapper"/> returned a <see langword="null"/> task or value.</exception>
    public static Task<Result<TOut>> MapAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<TOut>> mapper) where T : notnull where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return Core();

        async Task<Result<TOut>> Core() => await (await Completed(result).ConfigureAwait(false)).MapAsync(mapper).ConfigureAwait(false);
    }

    /// <summary>Continues a pending success with a synchronous step; see <see cref="Result{T}.Bind{TOut}(Func{T, Result{TOut}})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned <see langword="null"/>.</exception>
    public static Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> result, Func<T, Result<TOut>> next) where T : notnull where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result<TOut>> Core() => (await Completed(result).ConfigureAwait(false)).Bind(next);
    }

    /// <summary>Continues a pending success with an asynchronous step; see <see cref="Result{T}.BindAsync{TOut}(Func{T, Task{Result{TOut}}})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned a <see langword="null"/> task or result.</exception>
    public static Task<Result<TOut>> BindAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<Result<TOut>>> next) where T : notnull where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result<TOut>> Core() => await (await Completed(result).ConfigureAwait(false)).BindAsync(next).ConfigureAwait(false);
    }

    /// <summary>Continues a pending success with a synchronous step; see <see cref="Result{T}.Bind(Func{T, Result})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned <see langword="null"/>.</exception>
    public static Task<Result> BindAsync<T>(this Task<Result<T>> result, Func<T, Result> next) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result> Core() => (await Completed(result).ConfigureAwait(false)).Bind(next);
    }

    /// <summary>Continues a pending success with an asynchronous step; see <see cref="Result{T}.BindAsync(Func{T, Task{Result}})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned a <see langword="null"/> task or result.</exception>
    public static Task<Result> BindAsync<T>(this Task<Result<T>> result, Func<T, Task<Result>> next) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result> Core() => await (await Completed(result).ConfigureAwait(false)).BindAsync(next).ConfigureAwait(false);
    }

    /// <summary>Handles both outcomes of a pending result with synchronous branches; see <see cref="Result{T}.Match{TOut}(Func{T, TOut}, Func{IReadOnlyList{Error}, TOut})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <typeparam name="TOut">The type both branches return.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="onSuccess">Produces the outcome of a success.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors.</param>
    /// <returns>A task that completes with the value returned by the branch that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/>, <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>.</exception>
    public static Task<TOut> MatchAsync<T, TOut>(this Task<Result<T>> result, Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return Core();

        async Task<TOut> Core() => (await Completed(result).ConfigureAwait(false)).Match(onSuccess, onFailure);
    }

    /// <summary>Handles both outcomes of a pending result with asynchronous branches; see <see cref="Result{T}.MatchAsync{TOut}(Func{T, Task{TOut}}, Func{IReadOnlyList{Error}, Task{TOut}})"/>.</summary>
    /// <typeparam name="T">The type of the value of the pending result.</typeparam>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="onSuccess">Produces the outcome of a success asynchronously.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors, asynchronously.</param>
    /// <returns>A task that completes with the outcome of the branch that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/>, <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or the branch that ran returned a <see langword="null"/> task.</exception>
    public static Task<TOut> MatchAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return Core();

        async Task<TOut> Core() => await (await Completed(result).ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }
}
