using System;
using System.Threading.Tasks;

namespace Aero.Common.FP;

public static class ValidationPipeline
{
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