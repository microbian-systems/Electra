using Electra.Auth.Context;
using Electra.Auth.Models;
using Electra.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;

namespace Electra.Auth.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config)
    {
        // Configure database context
        services.AddDbContext<ElectraAuthDbContext>(opts =>
        {
            //opts.UseNpgsql(config.GetConnectionString("DefaultConnection"));
            opts.UseSqlite(config.GetConnectionString("DefaultConnection"));

            // Register the entity sets needed by OpenIddict
            // opts.UseOpenIddict();
        });

        // Configure ASP.NET Core Identity
        services.AddIdentity<ElectraUser, IdentityRole>(opts =>
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

        // Configure authentication
        services.AddAuthentication(options => {
                // Default scheme for web pages is Cookies
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // API requests use JWT
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(options => {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/auth/access-denied";
            })
            .AddJwtBearer(options => {
                var jwtSettings = config.GetSection("JwtSettings");
                var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);
    
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            })
// Add social logins
            .AddGoogle(options => {
                options.ClientId = config["Authentication:Google:ClientId"];
                options.ClientSecret = config["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/auth/signin-google";
            })
            .AddTwitter(options => {
                options.ConsumerKey = config["Authentication:Twitter:ConsumerKey"];
                options.ConsumerSecret = config["Authentication:Twitter:ConsumerSecret"];
                options.CallbackPath = "/auth/signin-twitter";
            })
// .AddApple(options => {
//     options.ClientId = config["Authentication:Apple:ClientId"];
//     options.KeyId = config["Authentication:Apple:KeyId"];
//     options.TeamId = config["Authentication:Apple:TeamId"];
//     options.PrivateKey = config["Authentication:Apple:PrivateKey"];
//     options.CallbackPath = "/auth/signin-apple";
// })
            ;

            // .AddMicrosoftAccount(opts =>
            // {
            //     opts.ClientId = config["Authentication:Microsoft:ClientId"];
            //     opts.ClientSecret = config["Authentication:Microsoft:ClientSecret"];
            // })
            ;

        return services;
    }
}