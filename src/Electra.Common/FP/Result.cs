namespace Electra.Common.FP;

public abstract record Result<TError, TValue>
{
    public sealed record Ok(TValue Value) : Result<TError, TValue>;

    public sealed record Failure(TError Error) : Result<TError, TValue>;
}