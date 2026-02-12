namespace ZauberCMS.Core.Membership.Models;

public class ContentRole
{
    public Content.Models.Content Content { get; set; } = null!;
    public string ContentId { get; set; }
    public Role Role { get; set; } = null!;
    public string RoleId { get; set; }
}