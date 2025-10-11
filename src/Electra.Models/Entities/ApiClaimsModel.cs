using System.ComponentModel.DataAnnotations;

namespace Electra.Models.Entities;

public class ApiClaimsModel // todo - inherit from Entity<long>
{
    [Key]
    public int Id { get; set; }
    public string ClaimKey { get; set; }
    public string ClaimValue { get; set; }
    
    public int AccountId { get; set; }
}