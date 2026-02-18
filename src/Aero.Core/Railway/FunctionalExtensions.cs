namespace Aero.Core.Railway;

using System;
using System.Threading.Tasks;

/// <summary>
/// Provides general functional programming extensions for any type.
/// These methods complement the Result and Option specific extensions with
/// general-purpose patterns like Pipe, Tap, and function composition.
/// </summary>
public static class FunctionalExtensions
{
    // =========================
    // PIPE
    // =========================

    /// <summary>
    /// Pipes a value through a function, enabling fluent left-to-right composition.
    /// </summary>
    public static TResult Pipe<T, TResult>(
        this T source,
        Func<T, TResult> func)
        => func(source);

    /// <summary>
    /// Pipes a task result through a function asynchronously.
    /// </summary>
    public static async Task<TResult> PipeAsync<T, TResult>(
        this Task<T> task,
        Func<T, TResult> func)
    {
        var result = await task.ConfigureAwait(false);
        return func(result);
    }

    /// <summary>
    /// Pipes a task result through an async function.
    /// </summary>
    public static async Task<TResult> PipeAsync<T, TResult>(
        this Task<T> task,
        Func<T, Task<TResult>> func)
    {
        var result = await task.ConfigureAwait(false);
        return await func(result).ConfigureAwait(false);
    }

    // =========================
    // TAP (Side Effects)
    // =========================

    /// <summary>
    /// Executes a side-effect action on a value and returns the original value.
    /// Useful for logging, debugging, or triggering side effects in a fluent chain.
    /// </summary>
    public static T Tap<T>(
        this T source,
        Action<T> action)
    {
        action(source);
        return source;
    }

    /// <summary>
    /// Executes a side-effect action on a task result asynchronously.
    /// </summary>
    public static async Task<T> TapAsync<T>(
        this Task<T> task,
        Action<T> action)
    {
        var result = await task.ConfigureAwait(false);
        action(result);
        return result;
    }

    /// <summary>
    /// Executes an async side-effect action on a task result.
    /// </summary>
    public static async Task<T> TapAsync<T>(
        this Task<T> task,
        Func<T, Task> action)
    {
        var result = await task.ConfigureAwait(false);
        await action(result).ConfigureAwait(false);
        return result;
    }

    // =========================
    // FUNCTION COMPOSITION
    // =========================

    /// <summary>
    /// Composes two functions in left-to-right order (forward composition).
    /// </summary>
    /// <remarks>
    /// This enables fluent function composition: f.Then(g) is equivalent to x => g(f(x)).
    /// </remarks>
    public static Func<T, TResult> Then<T, TIntermediate, TResult>(
        this Func<T, TIntermediate> first,
        Func<TIntermediate, TResult> second)
    {
        return input => second(first(input));
    }

    /// <summary>
    /// Composes two functions in right-to-left order (standard mathematical composition).
    /// </summary>
    /// <remarks>
    /// This enables standard function composition: g.Compose(f) is equivalent to x => g(f(x)).
    /// </remarks>
    public static Func<T, TResult> Compose<T, TIntermediate, TResult>(
        this Func<TIntermediate, TResult> second,
        Func<T, TIntermediate> first)
    {
        return input => second(first(input));
    }

    /// <summary>
    /// Composes two async functions in left-to-right order.
    /// </summary>
    public static Func<T, Task<TResult>> ThenAsync<T, TIntermediate, TResult>(
        this Func<T, Task<TIntermediate>> first,
        Func<TIntermediate, Task<TResult>> second)
    {
        return async input =>
        {
            var intermediate = await first(input).ConfigureAwait(false);
            return await second(intermediate).ConfigureAwait(false);
        };
    }

    // =========================
    // OPTION <-> RESULT CONVERSIONS
    // =========================

    /// <summary>
    /// Converts a Result to an Option, discarding the error.
    /// </summary>
    public static Option<TValue> ToOption<TError, TValue>(
        this Result<TError, TValue> result)
    {
        return result switch
        {
            Result<TError, TValue>.Ok(var value) => new Option<TValue>.Some(value),
            Result<TError, TValue>.Failure => new Option<TValue>.None(),
            _ => new Option<TValue>.None()
        };
    }

    /// <summary>
    /// Converts an Option to a Result with a specified error if None.
    /// </summary>
    public static Result<TError, TValue> ToResult<TValue, TError>(
        this Option<TValue> option,
        TError error)
    {
        return option switch
        {
            Option<TValue>.Some(var value) => new Result<TError, TValue>.Ok(value),
            Option<TValue>.None => new Result<TError, TValue>.Failure(error),
            _ => new Result<TError, TValue>.Failure(error)
        };
    }

    // =========================
    // ASYNC RESULT OPERATIONS
    // =========================

    /// <summary>
    /// Maps over a Result wrapped in a Task.
    /// </summary>
    public static async Task<Result<TError, TResult>> MapAsync<TError, TValue, TResult>(
        this Task<Result<TError, TValue>> task,
        Func<TValue, TResult> map)
    {
        var result = await task.ConfigureAwait(false);
        return result.Map(map);
    }

    /// <summary>
    /// Binds over a Result wrapped in a Task.
    /// </summary>
    public static async Task<Result<TError, TResult>> BindAsync<TError, TValue, TResult>(
        this Task<Result<TError, TValue>> task,
        Func<TValue, Task<Result<TError, TResult>>> bind)
    {
        var result = await task.ConfigureAwait(false);

        return result switch
        {
            Result<TError, TValue>.Ok(var value) => await bind(value).ConfigureAwait(false),
            Result<TError, TValue>.Failure(var error) => new Result<TError, TResult>.Failure(error),
            _ => throw new InvalidOperationException("Invalid Result state")
        };
    }

    // =========================
    // ASYNC OPTION OPERATIONS
    // =========================

    /// <summary>
    /// Maps over an Option wrapped in a Task.
    /// </summary>
    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Task<Option<T>> task,
        Func<T, TResult> map)
    {
        var option = await task.ConfigureAwait(false);
        return option.Map(map);
    }

    /// <summary>
    /// Binds over an Option wrapped in a Task.
    /// </summary>
    public static async Task<Option<TResult>> BindAsync<T, TResult>(
        this Task<Option<T>> task,
        Func<T, Task<Option<TResult>>> bind)
    {
        var option = await task.ConfigureAwait(false);

        return option switch
        {
            Option<T>.Some(var value) => await bind(value).ConfigureAwait(false),
            Option<T>.None => new Option<TResult>.None(),
            _ => new Option<TResult>.None()
        };
    }

    /// <summary>
    /// Matches on an Option wrapped in a Task.
    /// </summary>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Option<T>> task,
        Func<T, TResult> some,
        Func<TResult> none)
    {
        var option = await task.ConfigureAwait(false);
        return option switch
        {
            Option<T>.Some(var value) => some(value),
            Option<T>.None => none(),
            _ => none()
        };
    }
}
