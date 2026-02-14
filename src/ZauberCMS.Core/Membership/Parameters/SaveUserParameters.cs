using Electra.Models.Entities;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Parameters;

public class SaveUserParameters
{
    public ElectraUser? User { get; set; }
    public string? Password { get; set; }
    public List<string>? Roles { get; set; }
}