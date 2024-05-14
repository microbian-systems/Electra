namespace Electra.Auth.Passkey;

// https://community.abp.io/posts/implementing-passwordless-authentication-with-asp.net-core-identity-c25l8koj
public class PasswordlessLoginProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
    where TUser : class
{
    public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
    {
        return Task.FromResult(false);
    }

    //We need to override this method as well.
    public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        var userId = await manager.GetUserIdAsync(user);

        return "PasswordlessLogin:" + purpose + ":" + userId;
    }
}