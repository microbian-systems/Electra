using System.ComponentModel.DataAnnotations;
using Aero.Core.Entities;

namespace Aero.Models.Entities;

public class ApiClaimsModel : Entity
{
    [MaxLength(128)]
    public string ClaimKey { get; set; }
    [MaxLength(1024)]
    public string ClaimValue { get; set; }
    
    public string AccountId { get; set; }
}