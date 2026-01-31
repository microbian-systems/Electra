using Electra.Core.Identity;
using Electra.Persistence;

namespace Electra.Auth;

public class Seeder
{
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Seeder>>();
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
            if (await roleManager.RoleExistsAsync(role)) continue;
            var res = await roleManager.CreateAsync(new ElectraRole(role));
            if(res.Succeeded)
                log.LogInformation("Created role: {o}", role);
            else
                log.LogError("Error creating role: {o}: {a}", role, res.Errors.Select(e => e.Description).ToArray());
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