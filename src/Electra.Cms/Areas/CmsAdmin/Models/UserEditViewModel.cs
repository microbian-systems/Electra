using Electra.Models.Entities;

namespace Electra.Cms.Areas.CmsAdmin.Models
{
    public class UserEditViewModel
    {
        public required ElectraUser User { get; set; }
        public required IEnumerable<string> UserRoles { get; set; }
        public required IEnumerable<string> AllRoles { get; set; }
        public string? Password { get; set; }
    }
}
