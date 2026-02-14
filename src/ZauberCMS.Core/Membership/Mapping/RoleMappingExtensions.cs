using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Mapping;

/// <summary>
/// Extension methods for mapping Role entities
/// Replaces AutoMapper RoleMapper profile
/// </summary>
public static class RoleMappingExtensions
{
    /// <summary>
    /// Maps properties from source Role to target Role, excluding navigation properties
    /// </summary>
    /// <param name="source">Source Role entity</param>
    /// <param name="target">Target Role entity to update</param>
    /// <returns>Updated target Role entity</returns>
    public static CmsRole MapTo(this CmsRole source, CmsRole target)
    {
        // Map all simple properties from IdentityRole
        target.Id = source.Id;
        target.Name = source.Name;
        target.NormalizedName = source.NormalizedName;
        target.ConcurrencyStamp = source.ConcurrencyStamp;

        // Map custom audit properties
        target.CreatedOn = source.CreatedOn;
        target.ModifiedOn = source.ModifiedOn;

        // Note: Description, Icon, Properties, Tabs, ExtendedData are now in CmsRoleUI
        // Use CmsRoleUIMappingExtensions for those properties

        // Excluded properties (navigation properties):
        // - UserRoles (navigation property)
        // - ContentRoles (navigation property)
        // - MediaRoles (navigation property)

        return target;
    }

    /// <summary>
    /// Creates a new Role entity with properties mapped from source
    /// </summary>
    /// <param name="source">Source Role entity</param>
    /// <returns>New Role entity with mapped properties</returns>
    public static CmsRole MapToNew(this CmsRole source)
    {
        var target = new CmsRole();
        return source.MapTo(target);
    }
}

/// <summary>
/// Extension methods for mapping CmsRoleUI entities
/// </summary>
public static class CmsRoleUIMappingExtensions
{
    /// <summary>
    /// Maps properties from source RoleUI to target RoleUI
    /// </summary>
    public static CmsRoleUI MapTo(this CmsRoleUI source, CmsRoleUI target)
    {
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.ExtendedData = source.ExtendedData;
        target.Properties = source.Properties;
        target.Tabs = source.Tabs;

        return target;
    }

    /// <summary>
    /// Creates a new RoleUI entity with properties mapped from source
    /// </summary>
    public static CmsRoleUI MapToNew(this CmsRoleUI source)
    {
        var target = new CmsRoleUI();
        return source.MapTo(target);
    }
}
