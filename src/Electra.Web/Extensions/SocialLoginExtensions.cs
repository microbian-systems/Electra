using Microsoft.AspNetCore.Authentication;

namespace Electra.Common.Web.Extensions;

public static class SocialLoginExtensions
{
    public static AuthenticationBuilder AddSocialLogins(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<IConfiguration>();

        var authBuidler = services.AddAuthentication();
        authBuidler
            .AddFacebook(opts =>
            {
                opts.AppId = config["Authentication:Facebook:AppId"]
                             ?? throw new ArgumentNullException(opts.AppId, "facebook appid cannot be null");
                opts.AppSecret = config["Authentication:Facebook:AppSecret"]
                                 ?? throw new ArgumentNullException(opts.AppSecret, "facebook appsecret cannot be null");
                opts.AccessDeniedPath = "/AccessDeniedPathInfo";
            })
            .AddGoogle(opts =>
            {
                var googleAuthNSection =
                    config.GetSection("Authentication:Google");

                opts.ClientId = googleAuthNSection["ClientId"]
                                ?? throw new ArgumentNullException(opts.ClientId, "google clientid cannot be null");
                opts.ClientSecret = googleAuthNSection["ClientSecret"]
                                    ?? throw new ArgumentNullException(opts.ClientSecret, "google clientsecret cannot be null");
            });

        return authBuidler;
    }
}