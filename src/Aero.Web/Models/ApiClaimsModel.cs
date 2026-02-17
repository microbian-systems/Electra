using System.ComponentModel.DataAnnotations;

namespace Aero.Common.Web.Models;

public record ApiClaimsModel
{
    [Key]
    public int Id { get; set; }
    public string ClaimKey { get; set; }
    public string ClaimValue { get; set; }
    
    public int AccountId { get; set; }
}