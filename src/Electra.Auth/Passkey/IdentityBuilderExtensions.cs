namespace Electra.Auth.Passkey;

public static class IdentityBuilderExtensions
{
    public static IdentityBuilder AddPasswordlessLoginProvider(this IdentityBuilder builder)
    {
        var userType = builder.UserType;
        var totpProvider = typeof(PasswordlessLoginProvider<>).MakeGenericType(userType);

        return builder.AddDefaultTokenProviders()
            .AddTokenProvider("PasswordlessLoginProvider", totpProvider);
    }
}