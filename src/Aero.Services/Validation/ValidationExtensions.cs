using Aero.Core.Railway;
using FluentValidation;

namespace Aero.Services.Validation;

public static class ValidationExtensions
{
    public static async Task<ValidationOutcome<T>> ValidateCommandAsync<T>(this IValidator<T> validator, T command)
    {
        var result = await validator.ValidateAsync(command);

        if (result.IsValid)
            return new ValidationOutcome<T>.Valid(command);

        return new ValidationOutcome<T>.Invalid(
            result.Errors.First().ErrorMessage);
    }
}