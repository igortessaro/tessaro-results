using System.ComponentModel;

namespace Tessaro;

// Spec 002 AC-3.7 / ADR 0006: a mapper that returns a Task is more specific than Func<…, TOut>, so
// these overloads capture it and turn `result.Map(async …)` — which would otherwise compile to a
// result holding an unawaited task — into compile error CS0619 pointing to MapAsync.
public partial class Result
{
    private protected const string MapReceivedAsyncMapper =
        "The mapper returns a Task: use MapAsync, or the result would hold an unawaited task.";

    /// <summary>Not supported: the mapper returns a <see cref="Task{TResult}"/>; use <see cref="MapAsync{TOut}(Func{Task{TOut}})"/>.</summary>
    /// <typeparam name="TOut">The type the task would complete with.</typeparam>
    /// <param name="mapper">An asynchronous mapper.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(MapReceivedAsyncMapper, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result<Task<TOut>> Map<TOut>(Func<Task<TOut>> mapper) => throw new NotSupportedException(MapReceivedAsyncMapper);
}

public sealed partial class Result<T>
{
    /// <summary>Not supported: the mapper returns a <see cref="Task{TResult}"/>; use <see cref="MapAsync{TOut}(Func{T, Task{TOut}})"/>.</summary>
    /// <typeparam name="TOut">The type the task would complete with.</typeparam>
    /// <param name="mapper">An asynchronous mapper.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(MapReceivedAsyncMapper, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Result<Task<TOut>> Map<TOut>(Func<T, Task<TOut>> mapper) => throw new NotSupportedException(MapReceivedAsyncMapper);
}
