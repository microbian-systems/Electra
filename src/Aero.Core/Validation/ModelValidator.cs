using System.Linq.Expressions;

namespace Aero.Core.Validation;

/// <summary>Non-generic factory so callers don't have to specify the type explicitly.</summary>
public static class ModelValidator
{
    public static ModelValidator<T> For<T>(T model) => ModelValidator<T>.For(model);
}

/// <summary>
/// Fluent validator for a model object. Each call to <see cref="RuleFor"/>
/// registers a <see cref="FieldRuleBuilder{T}"/> for a specific string property.
/// <example>
/// <code>
/// var result = ModelValidator.For(registrationDto)
///     .RuleFor(x => x.Email).NotEmpty().MustBeEmail()
///     .RuleFor(x => x.Password).NotEmpty().MustBeValidPassword()
///     .RuleFor(x => x.Username).NotEmpty().MustBeValidUsername()
///     .RuleFor(x => x.Phone).MustBePhone()
///     .Validate();
///
/// if (!result.IsValid)
///     return BadRequest(result.Errors);
/// </code>
/// </example>
/// </summary>
public sealed class ModelValidator<T>
{
    private readonly T _model;
    private readonly List<IFieldValidator> _fieldValidators = [];

    private ModelValidator(T model) => _model = model;

    public static ModelValidator<T> For(T model) => new(model);

    /// <summary>
    /// Begins a rule chain for the specified string property.
    /// The member name is extracted from the expression and used in error messages.
    /// </summary>
    public FieldRuleBuilder<T> RuleFor(Expression<Func<T, string?>> selector)
    {
        var fieldName = ExtractMemberName(selector);
        var value = selector.Compile()(_model);
        var builder = new FieldRuleBuilder<T>(this, fieldName, value);
        _fieldValidators.Add(builder);
        return builder;
    }

    /// <summary>Executes all registered field rules and returns the aggregated result.</summary>
    public ValidationResult Validate()
        => new(_fieldValidators.SelectMany(v => v.GetErrors()));

    private static string ExtractMemberName(Expression<Func<T, string?>> selector)
        => selector.Body is MemberExpression member ? member.Member.Name : "Value";
}