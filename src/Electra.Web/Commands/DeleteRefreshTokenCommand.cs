using Electra.Persistence;
using Electra.Persistence.Entities;
using Electra.Common.Commands;

namespace Electra.Common.Web.Commands
{
    public class DeleteRefreshTokenRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string RefreshToken { get; set; }
    }
    
    public class DeleteRefreshTokenCommand : IAsyncCommand<DeleteRefreshTokenRequest, bool>
    {
        private readonly IDynamicMartenRepository db;

        public DeleteRefreshTokenCommand(IDynamicMartenRepository db)
        {
            this.db = db;
        }

        public async Task<bool> DeleteRefreshToken(string username, string refreshToken)
        {
            var record = await db.FindSingle<RefreshTokens>(x => x.UserId == username);
            if (record != null)
                await db.DeleteAsync(record);
            return true;
        }

        public async Task<bool> ExecuteAsync(DeleteRefreshTokenRequest command) =>
            await DeleteRefreshToken(command.Username, command.Password);
    }
}