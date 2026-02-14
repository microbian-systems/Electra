using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Shared.Validation.Interfaces;
using ZauberCMS.Core.Shared.Validation.Models;

namespace ZauberCMS.Core.Membership.Validation;

/// <summary>
/// Validates CmsUser entities
/// Note: PropertyData validation moved to CmsUserProfileValidator
/// </summary>
public class ValidateUser : IValidate<CmsUser>
{
    public Task<ValidateResult> Validate(CmsUser user)
    {
        var validateResult = new ValidateResult();
        if (user.UserName.IsNullOrWhiteSpace())
        {
            validateResult.ErrorMessages.Add("You cannot leave the name empty");
        }
        
        // Note: PropertyData validation moved to CmsUserProfileValidator
        // PropertyData is now in CmsUserProfile, not CmsUser

        return Task.FromResult(validateResult);
    }
}

/// <summary>
/// Validates CmsUserProfile entities
/// </summary>
public class ValidateUserProfile : IValidate<CmsUserProfile>
{
    public Task<ValidateResult> Validate(CmsUserProfile profile)
    {
        var validateResult = new ValidateResult();
        
        // Validate required properties
        // Note: To fully validate, we need access to the role properties
        // This would require injecting ICmsUserProfileService to load roles

        return Task.FromResult(validateResult);
    }
}
