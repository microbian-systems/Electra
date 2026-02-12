using System.Threading.Tasks;
using Electra.Common.Commands;
using Electra.Common.Extensions;
using Electra.Core.Extensions;
using Electra.Models.Entities;
using Electra.Persistence.Core;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.Marten;

public class UpdateUserProfileCommand(
    IGenericRepository<ElectraUserProfile, string> db,
    ILogger<UpdateUserProfileCommand> log)
    : IAsyncCommand<ElectraUserProfile, ElectraUserProfile>
{
    public async Task<ElectraUserProfile> ExecuteAsync(ElectraUserProfile profile)
    {
        log.LogInformation($"updating user profile: {profile.ToJson()}");
        var results = await db.UpsertAsync(profile);
        return results;
    }
}