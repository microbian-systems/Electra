namespace Aero.Cms.Models
{
    public static class CmsRoles
    {
        public const string Admin = "cms.admin";
        public const string Creator = "cms.creator";
        public const string Contributor = "cms.contributor";
        public const string Viewer = "cms.viewer";
        
        // Legacy/Existing - mapping to new ones or keeping if needed
        public const string Editor = "cms.editor";
        public const string Author = "cms.author";
    }

    public static class CmsPermissions
    {
        public const string ManageSites = "cms:sites:manage";
        public const string ManagePages = "cms:pages:manage";
        public const string PublishPages = "cms:pages:publish";
        public const string EditPages = "cms:pages:edit";
    }
}