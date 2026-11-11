using System.ComponentModel;

namespace Tessaro;

// Spec 004 US-5 / ADR 0006 (plan 004 §3.3). A lambda that returns a Task or a result converts to the
// void-returning Action of a side-effect step too — the task would never be awaited (an async lambda
// would become async void) and the result's errors would be lost. C# prefers a delegate with a return
// type for such a lambda, so these overloads capture it and turn the call into compile error CS0619
// pointing to the right member. They are hidden from IntelliSense and only reachable through reflection.
public static partial class ResultSideEffectExtensions
{
    private const string OnSuccessReceivedAsyncAction =
        "The action returns a Task: use OnSuccessAsync, or the task would not be awaited.";

    private const string OnFailureReceivedAsyncAction =
        "The action returns a Task: use OnFailureAsync, or the task would not be awaited.";

    private const string OnSuccessReceivedResultAction =
        "The action returns a result: use Bind, or its errors would be lost.";

    private const string OnSuccessAsyncReceivedResultAction =
        "The action returns a result: use BindAsync, or its errors would be lost.";

    /// <summary>Not supported: the action returns a <see cref="Task"/>; use <see cref="OnSuccessAsync{TResult}(TResult, Func{Task})"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An asynchronous action.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessReceivedAsyncAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TResult OnSuccess<TResult>(this TResult result, Func<Task> action) where TResult : Result =>
        throw new NotSupportedException(OnSuccessReceivedAsyncAction);

    /// <summary>Not supported: the action returns a <see cref="Task"/>; use <see cref="OnSuccessAsync{T}(Result{T}, Func{T, Task})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An asynchronous action.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessReceivedAsyncAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<T> OnSuccess<T>(this Result<T> result, Func<T, Task> action) where T : notnull =>
        throw new NotSupportedException(OnSuccessReceivedAsyncAction);

    /// <summary>Not supported: the action returns a <see cref="Task"/>; use <see cref="OnFailureAsync{TResult}(TResult, Func{IReadOnlyList{Error}, Task})"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An asynchronous action.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnFailureReceivedAsyncAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TResult OnFailure<TResult>(this TResult result, Func<IReadOnlyList<Error>, Task> action) where TResult : Result =>
        throw new NotSupportedException(OnFailureReceivedAsyncAction);

    /// <summary>Not supported: the action returns a result; use <see cref="Result.Bind(Func{Result})"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <typeparam name="TOut">The type of the result the action returns.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TResult OnSuccess<TResult, TOut>(this TResult result, Func<TOut> action) where TResult : Result where TOut : Result =>
        throw new NotSupportedException(OnSuccessReceivedResultAction);

    /// <summary>Not supported: the action returns a result; use <see cref="Result{T}.Bind(Func{T, Result})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TOut">The type of the result the action returns.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<T> OnSuccess<T, TOut>(this Result<T> result, Func<T, TOut> action) where T : notnull where TOut : Result =>
        throw new NotSupportedException(OnSuccessReceivedResultAction);

    /// <summary>Not supported: the action returns a task of a result; use <see cref="Result.BindAsync(Func{Task{Result}})"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <typeparam name="TOut">The type of the result the action completes with.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An asynchronous operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessAsyncReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TResult> OnSuccessAsync<TResult, TOut>(this TResult result, Func<Task<TOut>> action) where TResult : Result where TOut : Result =>
        throw new NotSupportedException(OnSuccessAsyncReceivedResultAction);

    /// <summary>Not supported: the action returns a task of a result; use <see cref="Result{T}.BindAsync(Func{T, Task{Result}})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TOut">The type of the result the action completes with.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="action">An asynchronous operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessAsyncReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<T>> OnSuccessAsync<T, TOut>(this Result<T> result, Func<T, Task<TOut>> action) where T : notnull where TOut : Result =>
        throw new NotSupportedException(OnSuccessAsyncReceivedResultAction);

    /// <summary>Not supported: the action returns a result; use <see cref="ResultTaskExtensions.BindAsync(Task{Result}, Func{Result})"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <typeparam name="TOut">The type of the result the action returns.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">An operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessAsyncReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TResult> OnSuccessAsync<TResult, TOut>(this Task<TResult> result, Func<TOut> action) where TResult : Result where TOut : Result =>
        throw new NotSupportedException(OnSuccessAsyncReceivedResultAction);

    /// <summary>Not supported: the action returns a task of a result; use <see cref="ResultTaskExtensions.BindAsync(Task{Result}, Func{Task{Result}})"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <typeparam name="TOut">The type of the result the action completes with.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">An asynchronous operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessAsyncReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<TResult> OnSuccessAsync<TResult, TOut>(this Task<TResult> result, Func<Task<TOut>> action) where TResult : Result where TOut : Result =>
        throw new NotSupportedException(OnSuccessAsyncReceivedResultAction);

    /// <summary>Not supported: the action returns a result; use <see cref="ResultTaskExtensions.BindAsync{T}(Task{Result{T}}, Func{T, Result})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TOut">The type of the result the action returns.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">An operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessAsyncReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<T>> OnSuccessAsync<T, TOut>(this Task<Result<T>> result, Func<T, TOut> action) where T : notnull where TOut : Result =>
        throw new NotSupportedException(OnSuccessAsyncReceivedResultAction);

    /// <summary>Not supported: the action returns a task of a result; use <see cref="ResultTaskExtensions.BindAsync{T}(Task{Result{T}}, Func{T, Task{Result}})"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TOut">The type of the result the action completes with.</typeparam>
    /// <param name="result">The pending result.</param>
    /// <param name="action">An asynchronous operation that can fail.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always (only reachable through reflection).</exception>
    [Obsolete(OnSuccessAsyncReceivedResultAction, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<T>> OnSuccessAsync<T, TOut>(this Task<Result<T>> result, Func<T, Task<TOut>> action) where T : notnull where TOut : Result =>
        throw new NotSupportedException(OnSuccessAsyncReceivedResultAction);
}
