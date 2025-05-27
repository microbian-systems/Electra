using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Validators;

namespace Electra.Validators.Extensions;

public class NotNullOrEmpty<T, TProperty> : AsyncPropertyValidator<T, TProperty>, INotNullOrEmpty
{
    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return $"{nameof(NotNullOrEmpty<T, TProperty>)}: {errorCode}";
    }

    public override string Name { get; }

    public override async Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellation)
    {
            
        if (value is null || value is string)
            return !string.IsNullOrEmpty(value as string);

        return await Task.FromResult(true);
    }
}