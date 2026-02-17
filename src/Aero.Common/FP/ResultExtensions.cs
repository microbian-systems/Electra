using System;
using System.Threading.Tasks;

namespace Aero.Common.FP;

public static class ResultExtensions
{
    public static Result<TError, TOut> Map<TError, TValue, TOut>(
        this Result<TError, TValue> r, Func<TValue, TOut> f) =>
        r switch
        {
            Result<TError, TValue>.Ok ok => new Result<TError, TOut>.Ok(f(ok.Value)),
            Result<TError, TValue>.Failure e => new Result<TError, TOut>.Failure(e.Error),
            _ => throw new InvalidOperationException()
        };

    public static async Task<Result<TError, TOut>> BindAsync<TError, TValue, TOut>(
        this Result<TError, TValue> r,
        Func<TValue, Task<Result<TError, TOut>>> f) =>
        r switch
        {
            Result<TError, TValue>.Ok ok => await f(ok.Value),
            Result<TError, TValue>.Failure e => new Result<TError, TOut>.Failure(e.Error),
            _ => throw new InvalidOperationException()
        };
}