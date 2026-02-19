namespace Aero.CMS.Core.Membership.Models;

public static class Permissions
{
    public const string ContentCreate = "content.create";
    public const string ContentEdit = "content.edit";
    public const string ContentApprove = "content.approve";
    public const string ContentPublish = "content.publish";
    public const string ContentDelete = "content.delete";

    public const string MediaManage = "media.manage";
    public const string UsersManage = "users.manage";
    public const string SettingsManage = "settings.manage";
    public const string PluginsManage = "plugins.manage";

    public static readonly string[] All = 
    {
        ContentCreate,
        ContentEdit,
        ContentApprove,
        ContentPublish,
        ContentDelete,
        MediaManage,
        UsersManage,
        SettingsManage,
        PluginsManage
    };
}
