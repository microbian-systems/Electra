namespace Aero.Common.FP;

public static class Prelude
{
    public static Option<T> Some<T>(T value) => new Option<T>.Some(value);
    public static None None => default;
    
    public static Result<TError, TValue> Ok<TError, TValue>(TValue value) => 
        new Result<TError, TValue>.Ok(value);
        
    public static Result<TError, TValue> Fail<TError, TValue>(TError error) => 
        new Result<TError, TValue>.Failure(error);
}