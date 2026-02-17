using FluentValidation;

namespace Aero.Validators.Extensions;

// todo - fix empty guid validator


    
public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, TProperty> GuidNotEmpty<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder)
    {
        return ruleBuilder.SetAsyncValidator(new NotEmptyGuid<T, TProperty>());
    }
    
    public static IRuleBuilderOptions<T, TProperty> NotNullOrEmpty<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder)
    {
        return ruleBuilder.SetAsyncValidator(new NotNullOrEmpty<T, TProperty>());
    }
}