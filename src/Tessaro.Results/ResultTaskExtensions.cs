namespace Tessaro;

/// <summary>
/// <c>MapAsync</c>, <c>BindAsync</c> and <c>MatchAsync</c> on a pending result
/// (<see cref="Task{TResult}"/> of <see cref="Result"/> or <see cref="Result{T}"/>), with synchronous
/// or asynchronous steps, so an awaited call composes as one chain.
/// </summary>
/// <remarks>
/// Every member validates its arguments when called — before the pending result is awaited — then
/// awaits it (without resuming on the caller's synchronization context) and applies the matching
/// member of <see cref="Result"/> / <see cref="Result{T}"/>. A failure passes every error on.
/// Exceptions and cancellation of the pending result or of a step propagate unchanged.
/// </remarks>
public static partial class ResultTaskExtensions
{
    private const string PendingCompletedWithNull = "The pending result completed with null.";

    private static async Task<TResult> Completed<TResult>(Task<TResult> result) where TResult : Result =>
        await result.ConfigureAwait(false) ?? throw new InvalidOperationException(PendingCompletedWithNull);

    /// <summary>Maps the value of a pending success without a value with a synchronous <paramref name="mapper"/>; see <see cref="Result.Map{TOut}(Func{TOut})"/>.</summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="mapper">Produces the value of the new success.</param>
    /// <returns>A task that completes with the mapped success, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="mapper"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="mapper"/> returned <see langword="null"/>.</exception>
    public static Task<Result<TOut>> MapAsync<TOut>(this Task<Result> result, Func<TOut> mapper) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return Core();

        async Task<Result<TOut>> Core() => (await Completed(result).ConfigureAwait(false)).Map(mapper);
    }

    /// <summary>Maps a pending success without a value with an asynchronous <paramref name="mapper"/>; see <see cref="Result.MapAsync{TOut}(Func{Task{TOut}})"/>.</summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="mapper">Produces the value of the new success asynchronously.</param>
    /// <returns>A task that completes with the mapped success, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="mapper"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="mapper"/> returned a <see langword="null"/> task or value.</exception>
    public static Task<Result<TOut>> MapAsync<TOut>(this Task<Result> result, Func<Task<TOut>> mapper) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return Core();

        async Task<Result<TOut>> Core() => await (await Completed(result).ConfigureAwait(false)).MapAsync(mapper).ConfigureAwait(false);
    }

    /// <summary>Continues a pending success without a value with a synchronous step; see <see cref="Result.Bind{TOut}(Func{Result{TOut}})"/>.</summary>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned <see langword="null"/>.</exception>
    public static Task<Result<TOut>> BindAsync<TOut>(this Task<Result> result, Func<Result<TOut>> next) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result<TOut>> Core() => (await Completed(result).ConfigureAwait(false)).Bind(next);
    }

    /// <summary>Continues a pending success without a value with an asynchronous step; see <see cref="Result.BindAsync{TOut}(Func{Task{Result{TOut}}})"/>.</summary>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned a <see langword="null"/> task or result.</exception>
    public static Task<Result<TOut>> BindAsync<TOut>(this Task<Result> result, Func<Task<Result<TOut>>> next) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result<TOut>> Core() => await (await Completed(result).ConfigureAwait(false)).BindAsync(next).ConfigureAwait(false);
    }

    /// <summary>Continues a pending success without a value with a synchronous step; see <see cref="Result.Bind(Func{Result})"/>.</summary>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned <see langword="null"/>.</exception>
    public static Task<Result> BindAsync(this Task<Result> result, Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result> Core() => (await Completed(result).ConfigureAwait(false)).Bind(next);
    }

    /// <summary>Continues a pending success without a value with an asynchronous step; see <see cref="Result.BindAsync(Func{Task{Result}})"/>.</summary>
    /// <param name="result">The pending result.</param>
    /// <param name="next">The asynchronous operation to run after a success.</param>
    /// <returns>A task that completes with the result of <paramref name="next"/>, or with the failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="next"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or <paramref name="next"/> returned a <see langword="null"/> task or result.</exception>
    public static Task<Result> BindAsync(this Task<Result> result, Func<Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Core();

        async Task<Result> Core() => await (await Completed(result).ConfigureAwait(false)).BindAsync(next).ConfigureAwait(false);
    }

    /// <summary>Handles both outcomes of a pending result without a value with synchronous branches; see <see cref="Result.Match{TOut}(Func{TOut}, Func{IReadOnlyList{Error}, TOut})"/>.</summary>
    /// <typeparam name="TOut">The type both branches return.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="onSuccess">Produces the outcome of a success.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors.</param>
    /// <returns>A task that completes with the value returned by the branch that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/>, <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>.</exception>
    public static Task<TOut> MatchAsync<TOut>(this Task<Result> result, Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return Core();

        async Task<TOut> Core() => (await Completed(result).ConfigureAwait(false)).Match(onSuccess, onFailure);
    }

    /// <summary>Handles both outcomes of a pending result without a value with asynchronous branches; see <see cref="Result.MatchAsync{TOut}(Func{Task{TOut}}, Func{IReadOnlyList{Error}, Task{TOut}})"/>.</summary>
    /// <typeparam name="TOut">The type both branches produce.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="onSuccess">Produces the outcome of a success asynchronously.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors, asynchronously.</param>
    /// <returns>A task that completes with the outcome of the branch that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/>, <paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (thrown by the call itself).</exception>
    /// <exception cref="InvalidOperationException">Awaited: the pending result completed with <see langword="null"/>, or the branch that ran returned a <see langword="null"/> task.</exception>
    public static Task<TOut> MatchAsync<TOut>(this Task<Result> result, Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return Core();

        async Task<TOut> Core() => await (await Completed(result).ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);
    }
}
