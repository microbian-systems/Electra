using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aero.Validators;

public abstract class BaseModelValidator<T> : AbstractValidator<T>
{
    protected readonly IMemoryCache cache;
    protected readonly ILogger<BaseModelValidator<T>> log;

    protected BaseModelValidator(IMemoryCache cache, ILogger<BaseModelValidator<T>> log)
    {
        this.cache = cache;
        this.log = log;
    }
}