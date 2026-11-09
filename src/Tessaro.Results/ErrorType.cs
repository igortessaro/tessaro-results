namespace Tessaro;

/// <summary>
/// The category of an <see cref="Error"/>: what kind of failure happened, independent of any
/// transport. Adapters map a category to a presentation (for example an HTTP status code).
/// </summary>
public enum ErrorType
{
    /// <summary>A general failure that fits no more specific category. The default for message-only failures.</summary>
    Failure,

    /// <summary>The input is invalid.</summary>
    Validation,

    /// <summary>A requested resource does not exist.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state (for example a duplicate).</summary>
    Conflict,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed to perform the operation.</summary>
    Forbidden,

    /// <summary>An unexpected condition the caller cannot fix.</summary>
    Unexpected,
}
