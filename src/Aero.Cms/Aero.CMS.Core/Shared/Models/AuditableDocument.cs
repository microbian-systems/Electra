using Aero.CMS.Core.Shared.Interfaces;

namespace Aero.CMS.Core.Shared.Models;

public abstract class AuditableDocument : IEntity<Guid>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    Guid IEntity<Guid>.Id
    {
        get => Guid.TryParse(Id, out var guid) ? guid : Guid.Empty;
        set => Id = value.ToString();
    }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
