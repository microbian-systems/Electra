namespace Aero.Core.Railway;

/// <summary>
/// Represents the outcome of a validation operation, either Valid with a value or Invalid with an error message.
/// </summary>
/// <typeparam name="T">The type of the validated value.</typeparam>
/// <remarks>
/// <para>
/// ValidationOutcome is a specialized variant of the Result type designed specifically for validation scenarios.
/// Unlike the general-purpose <see cref="Result{TError, TValue}"/> which can have any error type,
/// ValidationOutcome always uses <c>string</c> for errors (representing validation error messages).
/// </para>
/// <para>
/// <b>Railway-Oriented Validation:</b>
/// ValidationOutcome implements the same railway pattern as Result:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>The "success rail" carries the Valid value through the validation pipeline</description>
/// </item>
/// <item>
/// <description>The "failure rail" carries the Invalid error message when validation fails</description>
/// </item>
/// <item>
/// <description>Once on the failure rail, subsequent validations are bypassed</description>
/// </item>
/// </list>
/// <para>
/// <b>Monadic Context:</b>
/// Like Result and Option, ValidationOutcome provides a monadic context for validation operations.
/// You can "lift" validation functions into this context using <see cref="ValidationPipeline.BindAsync{T, TResult}"/>,
/// enabling composable validation pipelines where each validator is a pure function that returns a ValidationOutcome.
/// </para>
/// <para>
/// This type is ideal for building validation rules that need to be composed together,
/// such as input validation for APIs, form validation, or business rule checking.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating validation outcomes
/// ValidationOutcome&lt;string&gt; valid = new ValidationOutcome&lt;string&gt;.Valid("alice@example.com");
/// ValidationOutcome&lt;string&gt; invalid = new ValidationOutcome&lt;string&gt;.Invalid("Email is required");
/// 
/// // Pattern matching
/// var message = validationOutcome switch
/// {
///     ValidationOutcome&lt;string&gt;.Valid v => $"Valid: {v.Value}",
///     ValidationOutcome&lt;string&gt;.Invalid i => $"Error: {i.Error}",
/// };
/// </code>
/// </example>
public abstract record ValidationOutcome<T>
{
    /// <summary>
    /// Represents a successful validation outcome containing the validated value.
    /// </summary>
    /// <param name="Value">The value that passed validation.</param>
    /// <remarks>
    /// The Valid case represents successful validation. The value has passed all
    /// validation checks and can be used in subsequent operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// ValidationOutcome&lt;UserRegistration&gt; result = 
    ///     new ValidationOutcome&lt;UserRegistration&gt;.Valid(registration);
    /// </code>
    /// </example>
    public sealed record Valid(T Value) : ValidationOutcome<T>;

    /// <summary>
    /// Represents a failed validation outcome containing an error message.
    /// </summary>
    /// <param name="Error">A descriptive error message explaining why validation failed.</param>
    /// <remarks>
    /// The Invalid case represents validation failure. The error message should be
    /// human-readable and explain specifically what validation rule was violated.
    /// </remarks>
    /// <example>
    /// <code>
    /// ValidationOutcome&lt;string&gt; result = 
    ///     new ValidationOutcome&lt;string&gt;.Invalid("Password must be at least 8 characters");
    /// </code>
    /// </example>
    public sealed record Invalid(string Error) : ValidationOutcome<T>;
}