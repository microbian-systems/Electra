using Electra.Core.Entities;

namespace Electra.Persistence.Entities;

public record ContactMessage : EntityBase<Guid>
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}