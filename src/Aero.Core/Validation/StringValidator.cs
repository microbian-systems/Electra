namespace Aero.Core.Validation;

/// <summary>
/// Standalone fluent validator for a single string value.
/// <example>
/// <code>
/// var result = StringValidator.For(email, "Email")
///     .NotEmpty()
///     .MustBeEmail()
///     .Validate();
/// </code>
/// </example>
/// </summary>
public sealed class StringValidator : StringRuleBuilder<StringValidator>
{
    private StringValidator(string fieldName, string? value)
        : base(fieldName, value) { }

    /// <param name="value">The value to validate.</param>
    /// <param name="fieldName">Used in error messages. Defaults to "Value".</param>
    public static StringValidator For(string? value, string fieldName = "Value")
        => new(fieldName, value);

    public ValidationResult Validate()
        => new(GetErrors());
}