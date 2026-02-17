using Aero.Models;
using LanguageExt;

namespace Aero.EfCore;



public class UserRepository(AeroDbContext context, ILogger<GenericEntityFrameworkRepository<AeroUser>> log)
    : GenericEntityFrameworkRepository<AeroUser>(context, log), IUserRepository
{
    public async Task<Option<AeroUser>> GetFullUserById(string userId)
    {
        var user = await db.Where(x => x.Id == userId)
            .Include(x => x.Profile)
            .Include(x => x.UserSettings)
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<Option<AeroUserProfile>> GetUserProfileAsync(string userId)
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