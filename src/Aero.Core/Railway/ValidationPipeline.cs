namespace Aero.Core.Railway;

/// <summary>
/// Provides extension methods for working with <see cref="ValidationOutcome{T}"/> types,
/// enabling railway-oriented validation pipelines.
/// </summary>
/// <remarks>
/// <para>
/// ValidationPipeline extends the railway pattern specifically for validation scenarios,
/// where the error type is always a string (the validation error message) and operations
/// chain through Valid/Invalid outcomes.
/// </para>
/// <para>
/// <b>Railway-Oriented Validation:</b>
/// Validation is a perfect fit for railway-oriented programming because:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Each validation rule can either pass (Valid) or fail (Invalid)</description>
/// </item>
/// <item>
/// <description>Validation rules naturally compose - if one fails, subsequent rules can be skipped</description>
/// </item>
/// <item>
/// <description>The "failure rail" carries the specific validation error message</description>
/// </item>
/// </list>
/// <para>
/// <b>Monadic Lifting:</b>
/// The BindAsync method "lifts" validation functions into the ValidationOutcome context,
/// allowing you to compose validators declaratively while maintaining the short-circuit
/// behavior on the first validation failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Composing validators
/// var outcome = await ValidateNotEmpty(request.Name)
///     .BindAsync(name => ValidateLength(name, 2, 50))
///     .BindAsync(name => ValidateFormat(name, @"^[a-zA-Z]+$"));
/// 
/// var result = outcome.Match(
///     valid => $"Valid name: {valid}",
///     error => $"Validation failed: {error}"
/// );
/// </code>
/// </example>
public static class ValidationPipeline
{
    /// <summary>
    /// Chains an async validation function to a ValidationOutcome, propagating Invalid results automatically.
    /// </summary>
    /// <typeparam name="T">The type of the input value being validated.</typeparam>
    /// <typeparam name="TResult">The type of the output after validation transformation.</typeparam>
    /// <param name="validation">The current validation outcome.</param>
    /// <param name="next">The async validation function to chain.</param>
    /// <returns>
    /// A task that resolves to the result of <paramref name="next"/> if the input was Valid;
    /// otherwise, an Invalid outcome containing the original error.
    /// </returns>
    /// <remarks>
    /// <para>
    /// BindAsync implements the monadic bind for ValidationOutcome, "lifting" a validation function
    /// that returns a ValidationOutcome into the context of an existing ValidationOutcome.
    /// </para>
    /// <para>
    /// This enables railway-oriented composition of validators:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>If the current outcome is Valid, the next validator is executed</description>
    /// </item>
    /// <item>
    /// <description>If the current outcome is Invalid, the next validator is skipped and the error propagates</description>
    /// </item>
    /// </list>
    /// <para>
    /// This is the validation-specific equivalent of <see cref="ResultExtensions.BindAsync{TError, TValue, TOut}"/>,
    /// specialized for string errors (validation messages).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Individual validators
    /// async Task&lt;ValidationOutcome&lt;string&gt;&gt; ValidateNotEmpty(string value)
    /// {
    ///     return string.IsNullOrWhiteSpace(value)
    ///         ? new ValidationOutcome&lt;string&gt;.Invalid("Value cannot be empty")
    ///         : new ValidationOutcome&lt;string&gt;.Valid(value);
    /// }
    /// 
    /// async Task&lt;ValidationOutcome&lt;string&gt;&gt; ValidateEmailFormat(string email)
    /// {
    ///     var regex = new Regex(@"^[^@]+@[^@]+$");
    ///     return regex.IsMatch(email)
    ///         ? new ValidationOutcome&lt;string&gt;.Valid(email)
    ///         : new ValidationOutcome&lt;string&gt;.Invalid("Invalid email format");
    /// }
    /// 
    /// // Composing validators in a railway
    /// var emailOutcome = await ValidateNotEmpty(userInput)
    ///     .BindAsync(ValidateEmailFormat)
    ///     .BindAsync(async email => 
    ///     {
    ///         // Check if email is already taken
    ///         var exists = await userService.ExistsAsync(email);
    ///         return exists 
    ///             ? new ValidationOutcome&lt;string&gt;.Invalid("Email already registered")
    ///             : new ValidationOutcome&lt;string&gt;.Valid(email);
    ///     });
    /// 
    /// // At any point, if a validator returns Invalid, subsequent validators are skipped
    /// </code>
    /// </example>
    public static async Task<ValidationOutcome<TResult>> BindAsync<T, TResult>(
        this ValidationOutcome<T> validation,
        Func<T, Task<ValidationOutcome<TResult>>> next)
    {
        return validation switch
        {
            ValidationOutcome<T>.Valid v => await next(v.Value),

            ValidationOutcome<T>.Invalid i =>
                new ValidationOutcome<TResult>.Invalid(i.Error),

            _ => throw new InvalidOperationException()
        };
    }
}
