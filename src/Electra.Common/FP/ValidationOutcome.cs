namespace Electra.Common.FP;

public abstract record ValidationOutcome<T>
{
    public sealed record Valid(T Value) : ValidationOutcome<T>;

    public sealed record Invalid(string Error) : ValidationOutcome<T>;
}