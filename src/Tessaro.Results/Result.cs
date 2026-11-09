using System.Collections.ObjectModel;

namespace Tessaro;

/// <summary>
/// The outcome of an operation without a value: a success, or a failure with at least one
/// <see cref="Error"/>.
/// </summary>
/// <remarks>
/// <para>Results are immutable. A <see cref="Result{T}"/> is a <see cref="Result"/>, so a result with
/// a value can be returned or passed wherever a result without one is expected.</para>
/// <para>Create results with the static factories on this type (<see cref="Ok()"/>,
/// <see cref="Ok{T}(T)"/>, <see cref="Fail(Error)"/>, <see cref="Fail{T}(Error)"/>, …) or through the
/// implicit conversions from <see cref="Error"/> and from a value. The factories are also reachable as
/// <c>Result&lt;T&gt;.Ok()</c>, but that returns a <see cref="Result"/>; always call them on
/// <see cref="Result"/>.</para>
/// </remarks>
public class Result
{
    private const string GeneralFailureCode = "general.failure";

    private static readonly Result _success = new(ReadOnlyCollection<Error>.Empty);

    private protected Result(ReadOnlyCollection<Error> errors) => Errors = errors;

    /// <summary>Gets a value indicating whether the operation succeeded (it has no errors).</summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>Gets a value indicating whether the operation failed (it has at least one error).</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the errors of a failure, in the order they were given; empty, never <see langword="null"/>,
    /// for a success. The list is read-only.
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>Returns a success without a value.</summary>
    /// <returns>The success. Every call returns the same immutable instance.</returns>
    public static Result Ok() => _success;

    /// <summary>Creates a success holding <paramref name="value"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value. A success never holds <see langword="null"/>.</param>
    /// <returns>The success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Result<T> Ok<T>(T value) where T : notnull => new(value);

    /// <summary>Creates a failure with one error.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result Fail(Error error) => new(SingleError(error));

    /// <summary>Creates a failure with the given errors, kept in order.</summary>
    /// <param name="errors">The errors. They are copied, so later changes to the source don't affect the failure.</param>
    /// <returns>The failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/> or contains <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result Fail(IEnumerable<Error> errors) => new(CopyErrors(errors));

    /// <summary>
    /// Creates a failure with one <see cref="ErrorType.Failure"/> error with code <c>general.failure</c>
    /// and the given message.
    /// </summary>
    /// <param name="message">The human-readable description of the failure.</param>
    /// <returns>The failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or white space.</exception>
    public static Result Fail(string message) => Fail(Error.Failure(GeneralFailureCode, message));

    /// <summary>Creates a failure with one error, for an operation that would have returned a <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value a success would hold.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>The failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<T> Fail<T>(Error error) where T : notnull => new(SingleError(error));

    /// <summary>Creates a failure with the given errors, kept in order, for an operation that would have returned a <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value a success would hold.</typeparam>
    /// <param name="errors">The errors. They are copied, so later changes to the source don't affect the failure.</param>
    /// <returns>The failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/> or contains <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result<T> Fail<T>(IEnumerable<Error> errors) where T : notnull => new(CopyErrors(errors));

    /// <summary>
    /// Creates a failure with one <see cref="ErrorType.Failure"/> error with code <c>general.failure</c>
    /// and the given message, for an operation that would have returned a <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value a success would hold.</typeparam>
    /// <param name="message">The human-readable description of the failure.</param>
    /// <returns>The failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="message"/> is empty or white space.</exception>
    public static Result<T> Fail<T>(string message) where T : notnull => Fail<T>(Error.Failure(GeneralFailureCode, message));

    /// <summary>Converts an error to a failure, so a method returning <see cref="Result"/> can <c>return error;</c>.</summary>
    /// <param name="error">The error.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static implicit operator Result(Error error) => Fail(error);

    /// <summary>Returns <c>Success</c>, or <c>Failure(…)</c> listing every error, for logs.</summary>
    /// <returns>The text form of the result.</returns>
    public override string ToString() => IsSuccess ? "Success" : $"Failure({string.Join("; ", Errors)})";

    private static ReadOnlyCollection<Error> SingleError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ReadOnlyCollection<Error>([error]);
    }

    private static ReadOnlyCollection<Error> CopyErrors(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        Error[] copy = [.. errors];
        if (copy.Length == 0)
        {
            throw new ArgumentException("A failure needs at least one error.", nameof(errors));
        }

        if (Array.Exists(copy, error => error is null))
        {
            throw new ArgumentNullException(nameof(errors), "A failure cannot contain a null error.");
        }

        return new ReadOnlyCollection<Error>(copy);
    }
}
