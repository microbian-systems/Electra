using Electra.Auth.Context;
using Electra.Auth.Models;

namespace Electra.Auth.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Configure database context
        services.AddDbContext<ElectraAuthDbContext>(opts =>
        {
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection"));

            // Register the entity sets needed by OpenIddict
            opts.UseOpenIddict();
        });

        // Configure ASP.NET Core Identity
        services.AddIdentity<ElectraApplicationUser, IdentityRole>(opts =>
            {
                opts.Password.RequireDigit = true;
                opts.Password.RequireLowercase = true;
                opts.Password.RequireNonAlphanumeric = true;
                opts.Password.RequireUppercase = true;
                opts.Password.RequiredLength = 8;

                opts.User.RequireUniqueEmail = true;
                opts.SignIn.RequireConfirmedEmail = false; // Set to true if email confirmation is implemented
            })
            .AddEntityFrameworkStores<ElectraAuthDbContext>()
            .AddDefaultTokenProviders();

        // Add JWT Authentication with OpenIddict
        services.AddJwtAuthentication(config);

        // Configure external authentication providers
        services.AddAuthentication()
            .AddGoogle(opts =>
            {
                opts.ClientId = config["Authentication:Google:ClientId"];
                opts.ClientSecret = config["Authentication:Google:ClientSecret"];
            })
            .AddFacebook(opts =>
            {
                opts.AppId = config["Authentication:Facebook:AppId"];
                opts.AppSecret = config["Authentication:Facebook:AppSecret"];
            })
            .AddTwitter(opts =>
            {
                opts.ConsumerKey = config["Authentication:Twitter:ConsumerKey"];
                opts.ConsumerSecret = config["Authentication:Twitter:ConsumerSecret"];
            })
            .AddMicrosoftAccount(opts =>
            {
                opts.ClientId = config["Authentication:Microsoft:ClientId"];
                opts.ClientSecret = config["Authentication:Microsoft:ClientSecret"];
            })
            // .AddApple(opts =>
            // {
            //     opts.ClientId = config["Authentication:Apple:ClientId"];
            //     opts.KeyId = config["Authentication:Apple:KeyId"];
            //     opts.TeamId = config["Authentication:Apple:TeamId"];
            //     opts.PrivateKey = config["Authentication:Apple:PrivateKey"];
            // })
            ;

        return services;
    }
}