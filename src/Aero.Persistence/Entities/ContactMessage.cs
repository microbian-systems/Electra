using Aero.Core.Entities;

namespace Aero.Persistence.Entities;

public class ContactMessage : Entity
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}