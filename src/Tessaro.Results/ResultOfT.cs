using System.Collections.ObjectModel;

namespace Tessaro;

/// <summary>
/// The outcome of an operation that produces a <typeparamref name="T"/>: a success holding a
/// non-null value, or a failure with at least one <see cref="Error"/>.
/// </summary>
/// <typeparam name="T">The type of the value. It must not itself be <see cref="Error"/>: the implicit
/// conversions from a value and from an error would be ambiguous.</typeparam>
/// <remarks>
/// <para>A <see cref="Result{T}"/> is a <see cref="Result"/>, so it can be returned or passed wherever
/// a result without a value is expected, with the same state and errors.</para>
/// <para>A method returning <see cref="Result{T}"/> can <c>return value;</c> or <c>return error;</c>.
/// The conversion from a value does not apply when the value's static type is an interface (a C#
/// rule); use <see cref="Result.Ok{T}(T)"/> there.</para>
/// </remarks>
public sealed class Result<T> : Result where T : notnull
{
    private readonly T _value;

    internal Result(T value) : base(ReadOnlyCollection<Error>.Empty)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    internal Result(ReadOnlyCollection<Error> errors) : base(errors) => _value = default!;

    /// <summary>Gets the value of a success.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure; the message lists its errors.</exception>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"Cannot read the value of a failed result: {string.Join("; ", Errors)}");

    /// <summary>Converts a value to a success, so a method returning <see cref="Result{T}"/> can <c>return value;</c>.</summary>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Converts an error to a failure, so a method returning <see cref="Result{T}"/> can <c>return error;</c>.</summary>
    /// <param name="error">The error.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static implicit operator Result<T>(Error error) => Fail<T>(error);

    /// <summary>Returns <c>Success(value)</c>, or <c>Failure(…)</c> listing every error, for logs.</summary>
    /// <returns>The text form of the result.</returns>
    public override string ToString() => IsSuccess ? $"Success({_value})" : base.ToString();
}
