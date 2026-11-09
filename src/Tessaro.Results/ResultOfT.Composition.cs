namespace Tessaro;

public sealed partial class Result<T>
{
    /// <summary>
    /// On a success, creates a success holding <paramref name="mapper"/> applied to the value; on a
    /// failure, returns a failure with the same errors without calling <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="mapper">Transforms the value of the success.</param>
    /// <returns>The mapped success, or the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/> (on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="mapper"/> returned <see langword="null"/>.</exception>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsSuccess ? new Result<TOut>(NotNull(mapper(_value), MapperReturnedNull)) : ToFailure<TOut>();
    }

    /// <summary>
    /// On a success, runs the next operation with the value and returns its result; on a failure,
    /// returns this failure without calling <paramref name="next"/>.
    /// </summary>
    /// <param name="next">The operation to run with the value of a success.</param>
    /// <returns>The result of <paramref name="next"/>, or this failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="next"/> returned <see langword="null"/>.</exception>
    public Result Bind(Func<T, Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? NotNull(next(_value), NextReturnedNull) : this;
    }

    /// <summary>
    /// On a success, runs the next operation with the value and returns its result; on a failure,
    /// returns a failure with the same errors without calling <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value the next operation produces.</typeparam>
    /// <param name="next">The operation to run with the value of a success.</param>
    /// <returns>The result of <paramref name="next"/>, or the failure carried over.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/> (on a success or a failure).</exception>
    /// <exception cref="InvalidOperationException"><paramref name="next"/> returned <see langword="null"/>.</exception>
    /// <remarks>When <paramref name="next"/> returns a plain value, <typeparamref name="TOut"/> is not inferred; use <see cref="Map{TOut}(Func{T, TOut})"/> or write <c>Bind&lt;TOut&gt;(…)</c>.</remarks>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next) where TOut : notnull
    {
        ArgumentNullException.ThrowIfNull(next);

        return IsSuccess ? NotNull(next(_value), NextReturnedNull) : ToFailure<TOut>();
    }

    /// <summary>
    /// Handles both outcomes in one expression: returns <paramref name="onSuccess"/> applied to the
    /// value on a success, or <paramref name="onFailure"/> applied to every error on a failure. Only
    /// one delegate runs.
    /// </summary>
    /// <typeparam name="TOut">The type both branches return.</typeparam>
    /// <param name="onSuccess">Produces the outcome of a success from its value.</param>
    /// <param name="onFailure">Produces the outcome of a failure from all its errors, in order.</param>
    /// <returns>The value returned by the delegate that ran.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailure"/> is <see langword="null"/> (on a success or a failure).</exception>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value) : onFailure(Errors);
    }
}
