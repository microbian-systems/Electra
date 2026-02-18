using System.Linq.Expressions;

namespace Aero.Core.Validation;

/// <summary>
/// Fluent rule builder for a single field on a model.
/// Returned by <see cref="ModelValidator{T}.RuleFor"/>.
/// Delegates <c>RuleFor</c> and <c>Validate</c> back to the parent
/// so chains can span multiple fields without breaking fluency.
/// <example>
/// <code>
/// var result = ModelValidator.For(dto)
///     .RuleFor(x => x.Email).NotEmpty().MustBeEmail()
///     .RuleFor(x => x.Phone).MustBePhone()
///     .Validate();
/// </code>
/// </example>
/// </summary>
public sealed class FieldRuleBuilder<T> : StringRuleBuilder<FieldRuleBuilder<T>>
{
    private readonly ModelValidator<T> _parent;

    internal FieldRuleBuilder(ModelValidator<T> parent, string fieldName, string? value)
        : base(fieldName, value)
    {
        _parent = parent;
    }

    /// <summary>Registers a new field rule on the parent validator.</summary>
    public FieldRuleBuilder<T> RuleFor(Expression<Func<T, string?>> selector)
        => _parent.RuleFor(selector);

    /// <summary>Runs all registered rules across all fields.</summary>
    public ValidationResult Validate()
        => _parent.Validate();
}