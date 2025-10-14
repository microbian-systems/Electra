using System.ComponentModel.DataAnnotations;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

public class ApiClaimsModel : Entity // todo - inherit from Entity<long>
{
    public string ClaimKey { get; set; }
    public string ClaimValue { get; set; }
    
    public long AccountId { get; set; }
}