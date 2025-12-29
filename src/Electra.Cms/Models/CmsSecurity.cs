namespace Electra.Cms.Models
{
    public static class CmsRoles
    {
        public const string Admin = "CmsAdmin";
        public const string Editor = "CmsEditor";
        public const string Author = "CmsAuthor";
    }

    public static class CmsPermissions
    {
        public const string ManageSites = "cms:sites:manage";
        public const string ManagePages = "cms:pages:manage";
        public const string PublishPages = "cms:pages:publish";
        public const string EditPages = "cms:pages:edit";
    }
}
