using Electra.Models.Entities;

namespace Electra.Persistence.Extensions;

public static class UserManagerExtensions
{
    public static async Task<ElectraUser?> FindByIdAsync(this UserManager<ElectraUser> um, long id)
    {
        return await um.FindByIdAsync(id.ToString());
    }
}