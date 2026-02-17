using Aero.Core.Entities;

namespace Aero.Persistence.Entities;

public class RefreshTokens : Entity
{
    public string UserId { get; set; }
    public string Token { get; set; }
}