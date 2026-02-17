using System;
using Aero.Models;
using Aero.Models.Entities;
using Aero.Validators.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aero.Validators;

public class UserProfileUpdateRequestValidator : BaseModelValidator<UserProfileUpdateRequest>
{
    public UserProfileUpdateRequestValidator(IMemoryCache cache,
        ILogger<BaseModelValidator<UserProfileUpdateRequest>> log)
        : base(cache, log)
    {
        RuleFor(x => x.Id).NotNullOrEmpty();
    }

    protected bool NotBeEmptyGuid(Guid? guid) => 
        guid == null || (guid.HasValue && guid.Value != Guid.Empty ? true : false);
}
    
public class UserProfileValidator : BaseModelValidator<AeroUserProfile>
{
    public UserProfileValidator(IMemoryCache cache,
        ILogger<UserProfileValidator> log)
        : base(cache, log)
    {
        RuleFor(x => x.Id).NotNullOrEmpty();
    }
}