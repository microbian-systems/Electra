using Aero.Models;
using Aero.Validators.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aero.Validators;

public class RefreshTokenRequestValidator : BaseModelValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator(IMemoryCache cache, ILogger<BaseModelValidator<RefreshTokenRequest>> log) 
        : base(cache, log)
    {
        RuleFor(x => x.AccessToken).NotNullOrEmpty();
        RuleFor(x => x.RefreshToken).NotNullOrEmpty();
    }
}