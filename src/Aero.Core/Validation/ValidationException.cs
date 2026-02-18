namespace Aero.Core.Validation;

public sealed record ValidationError(string Field, string Message);


public sealed class ValidationException(ValidationResult result)
    : Exception(result.ToString())
{
    public ValidationResult Result { get; } = result;
}