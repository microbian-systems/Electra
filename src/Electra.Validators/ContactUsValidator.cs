using Electra.Models;
using Electra.Validators.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Electra.Validators;

public class ContactUsValidator : BaseModelValidator<ContactUsModel>
{
    public ContactUsValidator(IMemoryCache cache, ILogger<ContactUsValidator> log)
        : base(cache, log)
    {
        // todo - add test for proper email address
        // todo - add test for proper message length
        RuleFor(x => x.Name).NotNullOrEmpty();
        RuleFor(x => x.Email).NotNullOrEmpty();
        RuleFor(x => x.Message).NotNullOrEmpty();
    }
}