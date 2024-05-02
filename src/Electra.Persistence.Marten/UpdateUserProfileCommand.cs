using System.Threading.Tasks;
using Electra.Common.Commands;
using Electra.Common.Extensions;
using Electra.Models;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.Marten
{
    public class UpdateUserProfileCommand : IAsyncCommand<AppXUserProfile, AppXUserProfile>
    {
        private readonly ILogger<UpdateUserProfileCommand> log;
        private readonly IGenericRepository<AppXUserProfile, string> db;

        public UpdateUserProfileCommand(IGenericRepository<AppXUserProfile, string> db, ILogger<UpdateUserProfileCommand> log)
        {
            this.db = db;
            this.log = log;
        }

        public async Task<AppXUserProfile> ExecuteAsync(AppXUserProfile parameter)
        {
            log.LogInformation($"updating user profile: {parameter.ToJson()}");
            var results = await db.UpsertAsync(parameter);
            return results;
        }
    }
}