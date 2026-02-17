using System.ComponentModel.DataAnnotations.Schema;
using Aero.Core.Entities;

namespace Aero.Models.Entities;

[Table("UserPasskeys")]
public class UserPasskeys : Entity
{
    public string UserId { get; set; } = string.Empty;    
    public byte[] CredentialId { get; set; }    
    public byte[] PublicKey { get; set; }    
    public uint SignatureCounter { get; set; }
}
