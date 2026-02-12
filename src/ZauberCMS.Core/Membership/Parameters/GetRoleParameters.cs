namespace ZauberCMS.Core.Membership.Parameters;

public class GetRoleParameters
{
    public string? Id { get; set; }
    public bool AsNoTracking { get; set; } = true;
}