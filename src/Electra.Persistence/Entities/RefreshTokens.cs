using Electra.Core.Entities;

namespace Electra.Persistence.Entities;

public class RefreshTokens : Entity
{
    public long UserId { get; set; }
    public string Token { get; set; }
}