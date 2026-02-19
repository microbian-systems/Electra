using System;

namespace Aero.CMS.Core.Membership.Models;

public class PasskeyCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public byte[] CredentialId { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public int SignCount { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
