using Electra.Core.Entities;

namespace Electra.Persistence.Entities;

public class RefreshTokens : Entity
{
    public string UserId { get; set; }
    public string Token { get; set; }
}