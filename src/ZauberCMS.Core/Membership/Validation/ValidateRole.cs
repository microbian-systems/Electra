using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Shared.Validation.Interfaces;
using ZauberCMS.Core.Shared.Validation.Models;

namespace ZauberCMS.Core.Membership.Validation;

/// <summary>
/// Validates CmsRole entities
/// Note: Properties, Tabs, Description, Icon validation moved to CmsRoleUIValidator
/// </summary>
public class ValidateRole : IValidate<CmsRole>
{
    public Task<ValidateResult> Validate(CmsRole item)
    {
        var validateResult = new ValidateResult();
        
        if (item.Name.IsNullOrWhiteSpace())
        {
            validateResult.ErrorMessages.Add("You cannot leave the name empty");
        }
        
        // Note: Properties validation moved to CmsRoleUIValidator
        // Properties are now in CmsRoleUI, not CmsRole

        return Task.FromResult(validateResult);
    }
}

/// <summary>
/// Validates CmsRoleUI entities
/// </summary>
public class ValidateRoleUI : IValidate<CmsRoleUI>
{
    public Task<ValidateResult> Validate(CmsRoleUI item)
    {
        var validateResult = new ValidateResult();
        
        if (item.Properties.Any(x => x.Name.IsNullOrWhiteSpace()))
        {
            validateResult.ErrorMessages.Add("Some properties are missing a name (and alias)");
        }

        return Task.FromResult(validateResult);
    }
}
