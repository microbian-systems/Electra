using Electra.Models;
using Electra.Validators.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Electra.Validators
{
    public class RefreshTokenRequestValidator : BaseModelValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator(IMemoryCache cache, ILogger<BaseModelValidator<RefreshTokenRequest>> log) 
            : base(cache, log)
        {
            RuleFor(x => x.AccessToken).NotNullOrEmpty();
            RuleFor(x => x.RefreshToken).NotNullOrEmpty();
        }
    }
}