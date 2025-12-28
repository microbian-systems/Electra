using System.ComponentModel.DataAnnotations.Schema;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

[Table("UserPasskeys")]
public class UserPasskeys : Entity
{
    public long UserId { get; set; }    
    public byte[] CredentialId { get; set; }    
    public byte[] PublicKey { get; set; }    
    public uint SignatureCounter { get; set; }
}
