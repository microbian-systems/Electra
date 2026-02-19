using Aero.CMS.Core.Shared.Interfaces;

namespace Aero.CMS.Core.Shared.Models;

public abstract class AuditableDocument : IEntity<Guid>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
