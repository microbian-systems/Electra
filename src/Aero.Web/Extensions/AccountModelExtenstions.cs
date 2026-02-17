using Aero.Models.Entities;

namespace Aero.Common.Web.Extensions;

public static class AccountModelExtensions
{
    public static bool IsRefreshTokenValid(this ApiAccountModel model, string refreshToken)
    {
        if (model.RefreshToken != refreshToken || model.RefreshTokenExpiry <= DateTime.Now)
        {
            return false;
        }

        return true;
    }

    public static bool IsRefreshDateValid(this ApiAccountModel model)
        => model.RefreshTokenExpiry >= DateTimeOffset.UtcNow;
}