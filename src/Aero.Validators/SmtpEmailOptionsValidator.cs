using Aero.Validators.Extensions;
using Aero.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aero.Validators;

public class SmtpEmailOptionsValidator : BaseModelValidator<SmtpEmailOptions>
{
    public SmtpEmailOptionsValidator(IMemoryCache cache, ILogger<SmtpEmailOptionsValidator> log) 
        : base(cache, log)
    {
        RuleFor(x => x.Host).NotNullOrEmpty();
        RuleFor(x => x.SenderEmail).NotNullOrEmpty();
    }
}