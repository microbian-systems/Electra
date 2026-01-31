using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence;

namespace Electra.Auth;

public class Seeder
{
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetService<ElectraDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ElectraRole>>();

        // Create database and apply migrations if EF is used
        if (context != null)
        {
            //await context.Database.MigrateAsync();
        }

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
    }
}