using Aero.Core.Entities;
using Aero.Models.Entities;

namespace Aero.Models;

public class UserSettingsModel : Entity
{
    public string UserId { get; set; } // foreign key
    public AeroUser User { get; set; } // ef core relation
    public string Stuff { get; set; }
}