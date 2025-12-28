using System.ComponentModel.DataAnnotations;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

public class ApiClaimsModel : Entity // todo - inherit from Entity<long>
{
    [MaxLength(128)]
    public string ClaimKey { get; set; }
    [MaxLength(1024)]
    public string ClaimValue { get; set; }
    
    public long AccountId { get; set; }
}