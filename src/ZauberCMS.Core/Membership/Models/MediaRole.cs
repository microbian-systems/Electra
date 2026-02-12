namespace ZauberCMS.Core.Membership.Models;

public class MediaRole
{
    public Media.Models.Media Media { get; set; } = null!;
    public string MediaId { get; set; }
    public Role Role { get; set; } = null!;
    public string RoleId { get; set; }
}