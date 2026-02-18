using System;
using System.Threading.Tasks;

namespace Aero.Common.FP;

public static class ResultExtensions
{
    public static Result<TError, TOut> Map<TError, TValue, TOut>(
        this Result<TError, TValue> r, Func<TValue, TOut> f) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => new Result<TError, TOut>.Ok(f(value)),
            Result<TError, TValue>.Failure(var error) => new Result<TError, TOut>.Failure(error),
        };

    public static async Task<Result<TError, TOut>> MapAsync<TError, TValue, TOut>(
        this Result<TError, TValue> r, Func<TValue, Task<TOut>> f) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => new Result<TError, TOut>.Ok(await f(value)),
            Result<TError, TValue>.Failure(var error) => new Result<TError, TOut>.Failure(error),
        };

    // maps the error channel — equivalent of Option's Map but for TError
    public static Result<TErrorOut, TValue> MapError<TError, TValue, TErrorOut>(
        this Result<TError, TValue> r, Func<TError, TErrorOut> f) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => new Result<TErrorOut, TValue>.Ok(value),
            Result<TError, TValue>.Failure(var error) => new Result<TErrorOut, TValue>.Failure(f(error)),
        };

    public static Result<TError, TOut> Bind<TError, TValue, TOut>(
        this Result<TError, TValue> r, Func<TValue, Result<TError, TOut>> f) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => f(value),
            Result<TError, TValue>.Failure(var error) => new Result<TError, TOut>.Failure(error),
        };

    public static async Task<Result<TError, TOut>> BindAsync<TError, TValue, TOut>(
        this Result<TError, TValue> r, Func<TValue, Task<Result<TError, TOut>>> f) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => await f(value),
            Result<TError, TValue>.Failure(var error) => new Result<TError, TOut>.Failure(error),
        };

    public static T Match<TError, TValue, T>(
        this Result<TError, TValue> r,
        Func<TValue, T> onOk,
        Func<TError, T> onFailure) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => onOk(value),
            Result<TError, TValue>.Failure(var error) => onFailure(error),
        };

    // equivalent of Option's Filter — Ok becomes Failure if predicate fails
    public static Result<TError, TValue> Filter<TError, TValue>(
        this Result<TError, TValue> r,
        Func<TValue, bool> predicate,
        TError errorIfFalse) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) when predicate(value) => r,
            Result<TError, TValue>.Ok => new Result<TError, TValue>.Failure(errorIfFalse),
            Result<TError, TValue>.Failure => r,
        };

    // equivalent of Option's GetOrElse
    public static TValue GetOrElse<TError, TValue>(
        this Result<TError, TValue> r, TValue fallback) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => value,
            Result<TError, TValue>.Failure => fallback,
        };

    public static TValue GetOrElse<TError, TValue>(
        this Result<TError, TValue> r, Func<TError, TValue> fallback) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => value,
            Result<TError, TValue>.Failure(var error) => fallback(error),
        };

    // equivalent of Option's GetOrThrow
    public static TValue GetOrThrow<TError, TValue>(
        this Result<TError, TValue> r, string? message = null) =>
        r switch
        {
            Result<TError, TValue>.Ok(var value) => value,
            Result<TError, TValue>.Failure(var error) =>
                throw new InvalidOperationException(message ?? $"Result was Failure: {error}"),
        };

    // side effect on success — for logging mid-pipeline without breaking the chain
    public static Result<TError, TValue> Tap<TError, TValue>(
        this Result<TError, TValue> r, Action<TValue> action)
    {
        if (r is Result<TError, TValue>.Ok(var value)) action(value);
        return r;
    }

    // side effect on failure
    public static Result<TError, TValue> TapError<TError, TValue>(
        this Result<TError, TValue> r, Action<TError> action)
    {
        if (r is Result<TError, TValue>.Failure(var error)) action(error);
        return r;
    }
}