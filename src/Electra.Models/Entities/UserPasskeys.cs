using System.ComponentModel.DataAnnotations.Schema;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

[Table("UserPasskeys")]
public class UserPasskeys : Entity
{
    public string UserId { get; set; } = string.Empty;    
    public byte[] CredentialId { get; set; }    
    public byte[] PublicKey { get; set; }    
    public uint SignatureCounter { get; set; }
}
