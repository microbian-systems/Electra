namespace Aero.CMS.Core.Shared.Interfaces;

public interface IEntity<TId>
{
    TId Id { get; set; }
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
