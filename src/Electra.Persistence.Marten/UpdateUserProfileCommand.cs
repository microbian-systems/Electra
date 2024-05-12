using System.Threading.Tasks;
using Electra.Common.Commands;
using Electra.Common.Extensions;
using Electra.Models;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.Marten
{
    public class UpdateUserProfileCommand : IAsyncCommand<ElectraUserProfile, ElectraUserProfile>
    {
        private readonly ILogger<UpdateUserProfileCommand> log;
        private readonly IGenericRepository<ElectraUserProfile, string> db;

        public UpdateUserProfileCommand(IGenericRepository<ElectraUserProfile, string> db, ILogger<UpdateUserProfileCommand> log)
        {
            this.db = db;
            this.log = log;
        }

        public async Task<ElectraUserProfile> ExecuteAsync(ElectraUserProfile parameter)
        {
            log.LogInformation($"updating user profile: {parameter.ToJson()}");
            var results = await db.UpsertAsync(parameter);
            return results;
        }
    }
}