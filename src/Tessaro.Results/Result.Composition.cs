namespace Tessaro;

public partial class Result
{
    private protected const string MapperReturnedNull =
        "The mapper returned null; a success never holds null. Return a failure from Bind instead.";

    private protected const string NextReturnedNull = "The next operation returned a null result.";

    /// <summary>
    /// On a success, creates a success holding the value <paramref name="mapper"/> produces; on a
    /// failure, returns a failure with the same errors without calling <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="mapper">Produces the value of the new success.</param>
    /// <returns>The mapped success, or the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/> (on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="mapper"/> returned <see langword="null"/>.</exception>
    public Result<TOut> Map<TOut>(Func<TOut> mapper) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsSuccess ? new Result<TOut>(NotNull(mapper(), MapperReturnedNull)) : ToFailure<TOut>();
    }

    /// <summary>
    /// On a success, runs the next operation and returns its result; on a failure, returns this
    /// failure without calling <paramref name="next"/>.
    /// </summary>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>The result of <paramref name="next"/>, or this failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="next"/> returned <see langword="null"/>.</exception>
    public Result Bind(Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? NotNull(next(), NextReturnedNull) : this;
    }

    /// <summary>
    /// On a success, runs the next operation and returns its result; on a failure, returns a failure
    /// with the same errors without calling <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="next">The operation to run after a success.</param>
    /// <returns>The result of <paramref name="next"/>, or the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="next"/> returned <see langword="null"/>.</exception>
    public Result<TOut> Bind<TOut>(Func<Result<TOut>> next) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? NotNull(next(), NextReturnedNull) : ToFailure<TOut>();
    }

    /// <summary>
    /// Handles both outcomes in one expression: returns <paramref name="onSuccess"/>() on a success,
    /// or <paramref name="onFailure"/> applied to every error on a failure. Only one delegate runs.
    /// </summary>
    /// <typeparam name="TOut">The type both branches return.</typeparam>
    /// <param name="onSuccess">Produces the outcome of a success.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors, in order.</param>
    /// <returns>The value returned by the delegate that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (on a success or a failure).</exception>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess() : onFailure(_errors);
    }

    private protected static TValue NotNull<TValue>(TValue value, string message) =>
        value is null ? throw new InvalidOperationException(message) : value;
}
