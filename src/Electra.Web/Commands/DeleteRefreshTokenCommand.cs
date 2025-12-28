using Electra.Persistence;
using Electra.Persistence.Entities;
using Electra.Common.Commands;
using Electra.Persistence.Core.Marten;
using Electra.Persistence.Marten;

namespace Electra.Common.Web.Commands;

public class DeleteRefreshTokenRequest
{
    public string UserId { get; set; }
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

    public async Task<bool> DeleteRefreshToken(string id, string refreshToken)
    {
        var record = await db.FindSingle<RefreshTokens>(x => x.UserId == id);
        if (record != null)
            await db.DeleteAsync(record);
        return true;
    }

    public async Task<bool> ExecuteAsync(DeleteRefreshTokenRequest command) =>
        await DeleteRefreshToken(command.UserId, command.Password);
}