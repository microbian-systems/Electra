using Electra.Models.Entities;

namespace Electra.Cms.Areas.CmsAdmin.Models
{
    public class UserListViewModel
    {
        public required IEnumerable<ElectraUser> Users { get; set; }
    }
}
