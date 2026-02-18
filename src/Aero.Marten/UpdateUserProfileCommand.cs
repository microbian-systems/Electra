using Aero.Models.Entities;
using Aero.Common.Commands;
using Aero.Core.Data;
using Aero.Core.Extensions;
using Microsoft.Extensions.Logging;


namespace Aero.Marten;

public class UpdateUserProfileCommand(
    IGenericRepository<AeroUserProfile, string> db,
    ILogger<UpdateUserProfileCommand> log)
    : IAsyncCommand<AeroUserProfile, AeroUserProfile>
{
    public async Task<AeroUserProfile> ExecuteAsync(AeroUserProfile profile)
    {
        log.LogInformation($"updating user profile: {profile.ToJson()}");
        var results = await db.UpsertAsync(profile);
        return results;
    }
}