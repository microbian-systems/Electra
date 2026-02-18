using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace Aero.Common.FP;

public static class ValidationExtensions
{
    public static async Task<ValidationOutcome<T>>
        ValidateCommandAsync<T>(
            this IValidator<T> validator,
            T command)
    {
        var result = await validator.ValidateAsync(command);

        if (result.IsValid)
            return new ValidationOutcome<T>.Valid(command);

        return new ValidationOutcome<T>.Invalid(
            result.Errors.First().ErrorMessage);
    }
}