using Electra.Persistence;
using Electra.Persistence.Entities;
using Electra.Common.Commands;
using Electra.Core.Entities;

namespace Electra.Common.Web.Commands
{
    // todo - move this to the marten cqs project
    public class SaveRefreshTokenCommand : IAsyncCommand<SaveRefreshTokenRequest, bool>
    {
        private readonly IDynamicMartenRepository db;
        private readonly IAsyncCommand<DeleteRefreshTokenRequest, bool> command;

        public SaveRefreshTokenCommand(IDynamicMartenRepository db, IAsyncCommand<DeleteRefreshTokenRequest, bool> command)
        {
            this.db = db;
            this.command = command;
        }
        
        public async Task<bool> SaveRefreshToken(SaveRefreshTokenRequest request)
        {
            var success = await command.ExecuteAsync(new DeleteRefreshTokenRequest()
            {
                Username = request.Username,
                Password = request.Password
            });
            var entity = new RefreshTokens
            {
                Token = request.Token, 
                UserId = request.Username,
                CreatedOn = DateTime.UtcNow,
                ModifiedOn =  DateTime.UtcNow
            };
            
            await db.SaveAsync(entity);
            return true;
        }

        public async Task<bool> ExecuteAsync(SaveRefreshTokenRequest parameter) => await SaveRefreshToken(parameter);
    }

    public class SaveRefreshTokenRequest : Entity<string>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }
}