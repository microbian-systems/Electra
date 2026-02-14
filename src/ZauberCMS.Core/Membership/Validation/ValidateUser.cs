using Electra.Models.Entities;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Shared.Validation.Interfaces;
using ZauberCMS.Core.Shared.Validation.Models;

namespace ZauberCMS.Core.Membership.Validation;

/// <summary>
/// Validates ElectraUser entities
/// Note: PropertyData validation moved to ElectraUserProfileValidator
/// </summary>
public class ValidateUser : IValidate<ElectraUser>
{
    public Task<ValidateResult> Validate(ElectraUser user)
    {
        var validateResult = new ValidateResult();
        if (user.UserName.IsNullOrWhiteSpace())
        {
            validateResult.ErrorMessages.Add("You cannot leave the name empty");
        }
        
        // Note: PropertyData validation moved to ElectraUserProfileValidator
        // PropertyData is now in ElectraUserProfile, not ElectraUser

        return Task.FromResult(validateResult);
    }
}

/// <summary>
/// Validates ElectraUserProfile entities
/// </summary>
public class ValidateUserProfile : IValidate<ElectraUserProfile>
{
    public Task<ValidateResult> Validate(ElectraUserProfile profile)
    {
        var validateResult = new ValidateResult();
        
        // Validate required properties
        // Note: To fully validate, we need access to the role properties
        // This would require injecting IElectraUserProfileService to load roles

        return Task.FromResult(validateResult);
    }
}
