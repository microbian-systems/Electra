using Aero.Models.Entities;

namespace Aero.Cms.Areas.CmsAdmin.Models
{
    public class UserListViewModel
    {
        public required IEnumerable<AeroUser> Users { get; set; }
    }
}
