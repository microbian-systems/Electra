using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Validators;
using IPropertyValidator = FluentValidation.Validators.IPropertyValidator;

namespace Electra.Validators.Extensions
{
    public interface INotNullOrEmpty : IPropertyValidator{}
    public class NotEmptyGuid<T, TProperty> : AsyncPropertyValidator<T, TProperty>, INotEmptyGuidValidator 
    {
        public override async Task<bool> IsValidAsync(ValidationContext<T> context, TProperty value, CancellationToken cancellation)
        {
            var guid = value as Guid?;
            if (guid != null)
            {
                if (guid.Value == Guid.Empty)
                    return false;
            }
            return await Task.FromResult(true);
        }

        public override string Name { get; }
    }
}