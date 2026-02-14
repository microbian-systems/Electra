using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Mapping;

/// <summary>
/// Extension methods for mapping User entities
/// Replaces AutoMapper UserMapper profile
/// </summary>
public static class UserMappingExtensions
{
    /// <summary>
    /// Maps properties from source User to target User, excluding navigation properties
    /// </summary>
    /// <param name="source">Source User entity</param>
    /// <param name="target">Target User entity to update</param>
    /// <returns>Updated target User entity</returns>
    public static CmsUser MapTo(this CmsUser source, CmsUser target)
    {
        // Map all simple properties from IdentityUser
        target.Id = source.Id;
        target.UserName = source.UserName;
        target.NormalizedUserName = source.NormalizedUserName;
        target.Email = source.Email;
        target.NormalizedEmail = source.NormalizedEmail;
        target.EmailConfirmed = source.EmailConfirmed;
        target.PasswordHash = source.PasswordHash;
        target.SecurityStamp = source.SecurityStamp;
        target.ConcurrencyStamp = source.ConcurrencyStamp;
        target.PhoneNumber = source.PhoneNumber;
        target.PhoneNumberConfirmed = source.PhoneNumberConfirmed;
        target.TwoFactorEnabled = source.TwoFactorEnabled;
        target.LockoutEnd = source.LockoutEnd;
        target.LockoutEnabled = source.LockoutEnabled;
        target.AccessFailedCount = source.AccessFailedCount;

        // Map custom audit properties
        target.CreatedOn = source.CreatedOn;
        target.ModifiedOn = source.ModifiedOn;
        target.CreatedBy = source.CreatedBy;
        target.ModifiedBy = source.ModifiedBy;

        // Note: PropertyData, ExtendedData are now in CmsUserProfile
        // Use CmsUserProfileMappingExtensions for those properties

        // Excluded properties (navigation properties):
        // - UserRoles (navigation property)

        return target;
    }

    /// <summary>
    /// Creates a new User entity with properties mapped from source
    /// </summary>
    /// <param name="source">Source User entity</param>
    /// <returns>New User entity with mapped properties</returns>
    public static CmsUser MapToNew(this CmsUser source)
    {
        var target = new CmsUser();
        return source.MapTo(target);
    }
}

/// <summary>
/// Extension methods for mapping CmsUserProfile entities
/// </summary>
public static class CmsUserProfileMappingExtensions
{
    /// <summary>
    /// Maps properties from source Profile to target Profile
    /// </summary>
    public static CmsUserProfile MapTo(this CmsUserProfile source, CmsUserProfile target)
    {
        target.PropertyData = source.PropertyData;
        target.ExtendedData = source.ExtendedData;
        target.ProfileImageId = source.ProfileImageId;

        return target;
    }

    /// <summary>
    /// Creates a new Profile entity with properties mapped from source
    /// </summary>
    public static CmsUserProfile MapToNew(this CmsUserProfile source)
    {
        var target = new CmsUserProfile();
        return source.MapTo(target);
    }
}
