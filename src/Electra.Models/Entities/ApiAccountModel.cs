using System.ComponentModel.DataAnnotations;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

public class ApiAccountModel : Entity
{
    [MaxLength(128)]
    public string? ApiKey { get; set; }
    [MaxLength(256)]
    public string Email { get; set; }
    public bool Enabled { get; set; }
    [MaxLength(1024)]
    public string RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiry { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset ModifiedDate { get; set; }
    public virtual List<ApiClaimsModel> Claims { get; set; } = new();
}

// public record ApiAccountModel : IEntity<int>
// {
//     [Key]
//     public int Id { get; set; }
//     public DateTimeOffset CreatedOn { get; set; }
//     public DateTimeOffset? ModifiedOn { get; set; }
//     public string CreatedBy { get; set; }
//     public string ModifiedBy { get; set; }
//     public string? ApiKey { get; set; }
//     public string Email { get; set; }
//     public bool Enabled { get; set; }
//     public string RefreshToken { get; set; }
//     public DateTimeOffset RefreshTokenExpiry { get; set; }
//     public DateTimeOffset CreateDate { get; set; }
//     public DateTimeOffset ModifiedDate { get; set; }
//     public virtual List<ApiClaimsModel> Claims { get; set; } = new();
// }