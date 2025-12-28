using Electra.Core.Entities;
using Electra.Models.Entities;

namespace Electra.Models;

public class UserSettingsModel : Entity
{
    public string UserId { get; set; } // foreign key
    public ElectraUser User { get; set; } // ef core relation
    public string Stuff { get; set; }
}