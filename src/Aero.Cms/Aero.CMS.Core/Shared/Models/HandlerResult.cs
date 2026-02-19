namespace Aero.CMS.Core.Shared.Models;

public class HandlerResult
{
    public bool Success { get; protected init; }
    public List<string> Errors { get; protected init; } = [];

    public static HandlerResult Ok() => new() { Success = true };
    public static HandlerResult Fail(string error)
        => new() { Success = false, Errors = [error] };
    public static HandlerResult Fail(IEnumerable<string> errors)
        => new() { Success = false, Errors = [..errors] };
}

public class HandlerResult<T> : HandlerResult
{
    public T? Value { get; private init; }

    public static HandlerResult<T> Ok(T value)
        => new() { Success = true, Value = value };
    public new static HandlerResult<T> Fail(string error)
        => new() { Success = false, Errors = [error] };
    public new static HandlerResult<T> Fail(IEnumerable<string> errors)
        => new() { Success = false, Errors = [..errors] };
}
