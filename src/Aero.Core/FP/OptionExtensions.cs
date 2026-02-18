using System;
using System.Threading.Tasks;

namespace Aero.Common.FP;

public static class OptionExtensions
{
    public static Option<TOut> Map<TIn, TOut>(
        this Option<TIn> option, Func<TIn, TOut> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => new Option<TOut>.Some(f(value)),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    public static Option<TOut> Bind<TIn, TOut>(
        this Option<TIn> option, Func<TIn, Option<TOut>> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => f(value),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    public static async Task<Option<TOut>> MapAsync<TIn, TOut>(
        this Option<TIn> option, Func<TIn, Task<TOut>> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => new Option<TOut>.Some(await f(value)),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    public static async Task<Option<TOut>> BindAsync<TIn, TOut>(
        this Option<TIn> option, Func<TIn, Task<Option<TOut>>> f) =>
        option switch
        {
            Option<TIn>.Some(var value) => await f(value),
            Option<TIn>.None => new Option<TOut>.None(),
        };

    // bridge to Result
    public static Result<TError, TValue> OkOrFailure<TError, TValue>(
        this Option<TValue> option, TError error) =>
        option switch
        {
            Option<TValue>.Some(var value) => new Result<TError, TValue>.Ok(value),
            Option<TValue>.None => new Result<TError, TValue>.Failure(error),
        };

    // filter — turns Some into None if predicate fails
    public static Option<T> Filter<T>(
        this Option<T> option, Func<T, bool> predicate) =>
        option switch
        {
            Option<T>.Some(var value) when predicate(value) => option,
            Option<T>.Some => new Option<T>.None(),
            Option<T>.None => option,
        };

    public static T GetOrElse<T>(this Option<T> option, T fallback) =>
        option switch
        {
            Option<T>.Some(var value) => value,
            Option<T>.None => fallback,
        };

    public static T GetOrElse<T>(this Option<T> option, Func<T> fallback) =>
        option switch
        {
            Option<T>.Some(var value) => value,
            Option<T>.None => fallback(),
        };
}