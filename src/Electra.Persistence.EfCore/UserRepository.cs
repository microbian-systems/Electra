using Electra.Models;
using Electra.Models.Entities;
using Electra.Persistence.Core;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.EfCore;



public class UserRepository(ElectraDbContext context, ILogger<GenericEntityFrameworkRepository<ElectraUser>> log)
    : GenericEntityFrameworkRepository<ElectraUser>(context, log), IUserRepository
{
    public async Task<Option<ElectraUser>> GetFullUserById(string userId)
    {
        var user = await db.Where(x => x.Id == userId)
            .Include(x => x.Profile)
            .Include(x => x.UserSettings)
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<Option<ElectraUserProfile>> GetUserProfileAsync(string userId)
    {
        var user = await db.Where(x => x.Id == userId)
            .Include(x => x.Profile)
            .FirstOrDefaultAsync();

        return user?.Profile;
    }

    public async Task<Option<UserSettingsModel>> GetUserSettingsAsync(string userId)
    {
        var user = await db.Where(x => x.Id == userId)
            .Include(x => x.UserSettings)
            .FirstOrDefaultAsync();

        return user?.UserSettings;
    }
}