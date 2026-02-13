using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Shared.Validation.Interfaces;
using ZauberCMS.Core.Shared.Validation.Models;

namespace ZauberCMS.Core.Membership.Validation;

public class ValidateUser : IValidate<CmsUser>
{
    public Task<ValidateResult> Validate(CmsUser user)
    {
        var validateResult = new ValidateResult();
        if (user.UserName.IsNullOrWhiteSpace())
        {
            validateResult.ErrorMessages.Add("You cannot leave the name empty");
        }
        
        var roles = user!.UserRoles.Select(x => x.Role);
        var enumerable = roles as CmsRole[] ?? roles.ToArray();
        var contentProperties = enumerable.SelectMany(x => x.Properties).ToList();
        var valuesInDict = user.PropertyData.ToDictionary(x => x.ContentTypePropertyId, x => x);
        foreach (var p in contentProperties.Where(x => x.IsRequired))
        {
            
            //valuesInDict.TryGetValue(p.Id, out var contentValue);
            if (string.IsNullOrWhiteSpace(p.Id))
            {
                validateResult.ErrorMessages.Add($"{p.Name} is required");
            }
        }

        return Task.FromResult(validateResult);
    }
}