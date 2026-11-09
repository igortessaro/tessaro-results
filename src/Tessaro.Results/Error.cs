using System.Diagnostics.CodeAnalysis;

namespace Tessaro;

/// <summary>
/// Describes why an operation failed: a stable machine-readable <see cref="Code"/>, a
/// human-readable <see cref="Message"/> and a <see cref="Type"/> category.
/// </summary>
/// <remarks>
/// Errors are immutable values: two errors with the same code, message and type are equal.
/// Create them with the factory of their category, e.g. <see cref="NotFound(string, string)"/>.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Error is the standard name for this concept in .NET result libraries (ADR 0002); VB.NET callers can write [Error].")]
public sealed record Error
{
    private Error(string code, string message, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Type = type;
    }

    /// <summary>Gets the stable, machine-readable identifier of the error, e.g. <c>order.not_found</c>.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable description of the error.</summary>
    public string Message { get; }

    /// <summary>Gets the category of the error.</summary>
    public ErrorType Type { get; }

    /// <summary>Creates a general failure error (<see cref="ErrorType.Failure"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>Creates a validation error (<see cref="ErrorType.Validation"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a not-found error (<see cref="ErrorType.NotFound"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a conflict error (<see cref="ErrorType.Conflict"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates an unauthorized error (<see cref="ErrorType.Unauthorized"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a forbidden error (<see cref="ErrorType.Forbidden"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates an unexpected error (<see cref="ErrorType.Unexpected"/>).</summary>
    /// <param name="code">The stable, machine-readable identifier of the error.</param>
    /// <param name="message">The human-readable description of the error.</param>
    /// <returns>The new error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty or white space.</exception>
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);

    /// <summary>Returns the error as <c>"{Type} {Code}: {Message}"</c>, e.g. <c>NotFound order.not_found: Order 42 was not found</c>.</summary>
    /// <returns>The text form of the error, for logs.</returns>
    public override string ToString() => $"{Type} {Code}: {Message}";
}
