using System;

namespace Aero.Common.FP;

public abstract record Result<TError, TValue>
{
    public sealed record Ok(TValue Value) : Result<TError, TValue>;

    public sealed record Failure(TError Error) : Result<TError, TValue>;
    
    public static implicit operator Result<TError, TValue>(TValue value) => new Ok(value);
    public static implicit operator Result<TError, TValue>(TError error) => new Failure(error);

    public static explicit operator TValue(Result<TError, TValue> result) =>
        result switch
        {
            Ok(var value) => value,
            Failure(var error) => throw new InvalidCastException($"Result was Failure: {error}"),
        };

    public static explicit operator TError(Result<TError, TValue> result) =>
        result switch
        {
            Failure(var error) => error,
            Ok(var value) => throw new InvalidCastException($"Result was Ok: {value}"),
        };
}

public abstract record Option<T>
{
    public sealed record Some(T Value) : Option<T>;
    public sealed record None : Option<T>;
    
    public static implicit operator Option<T>(T value) =>
        value is not null ? new Some(value) : new None();

    // explicit — unwrap back to T, throws if None
    public static explicit operator T(Option<T> option) =>
        option switch
        {
            Some(var value) => value,
            None => throw new InvalidCastException("Cannot cast None to value"),
        };
}

public readonly struct None { }

