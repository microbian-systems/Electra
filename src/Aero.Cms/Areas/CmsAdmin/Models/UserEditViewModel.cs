using Aero.Models.Entities;

namespace Aero.Cms.Areas.CmsAdmin.Models
{
    public class UserEditViewModel
    {
        public required AeroUser User { get; set; }
        public required IEnumerable<string> UserRoles { get; set; }
        public required IEnumerable<string> AllRoles { get; set; }
        public string? Password { get; set; }
    }
}
