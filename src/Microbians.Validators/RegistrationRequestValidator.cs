using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microbians.Validators
{
    public class RegistrationRequestValidator : BaseModelValidator<RegistrationRequestValidator>
    {
        public RegistrationRequestValidator(IMemoryCache cache, ILogger<RegistrationRequestValidator> log) 
            : base(cache, log)
        {
            
        }
    }
}