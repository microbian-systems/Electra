namespace Aero.Core.Validation;

public sealed class ValidationResult
{
    public static readonly ValidationResult Valid = new([]);

    public IReadOnlyList<ValidationError> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    internal ValidationResult(IEnumerable<ValidationError> errors)
        => Errors = errors.ToList().AsReadOnly();

    /// <summary>Throws a <see cref="ValidationException"/> if the result is invalid.</summary>
    public ValidationResult ThrowIfInvalid()
    {
        if (!IsValid) throw new ValidationException(this);
        return this;
    }

    public override string ToString()
        => IsValid ? "Valid" : string.Join("; ", Errors.Select(e => $"{e.Field}: {e.Message}"));
}