using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Electra.Auth;

public class Seeder
{
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElectraDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ElectraRole>>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Create database and apply migrations
        await context.Database.MigrateAsync();

        // Seed roles
        string[] roles = ["Admin", "User", "Editor"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ElectraRole(role));
            }
        }

        // Seed admin user
        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];

        if (adminEmail != null && adminPassword != null)
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ElectraUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Profile = new ElectraUserProfile
                    {
                    }
                };

                await userManager.CreateAsync(adminUser, adminPassword);
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Register client applications for OpenIddict
        // Web application client
        if (await applicationManager.FindByClientIdAsync("electra_web_client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "electra_web_client",
                ClientSecret = "web_client_secret",
                DisplayName = "Electra Web Client",
                RedirectUris = { new Uri(configuration["ClientUrls:Web"] ?? "https://localhost:3000") },
                PostLogoutRedirectUris = { new Uri(configuration["ClientUrls:Web"] ?? "https://localhost:3000") },
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Revocation,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Token,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                }
            });
        }

        // Mobile application client
        if (await applicationManager.FindByClientIdAsync("electra_mobile_client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "electra_mobile_client",
                ClientSecret = "mobile_client_secret",
                DisplayName = "Electra Mobile Client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Revocation,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Token,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                }
            });
        }

        // Desktop application client
        if (await applicationManager.FindByClientIdAsync("electra_desktop_client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "electra_desktop_client",
                ClientSecret = "desktop_client_secret",
                DisplayName = "Electra Desktop Client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Revocation,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Token,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                }
            });
        }
    }
}