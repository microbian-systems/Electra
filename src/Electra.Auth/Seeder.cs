using Electra.Core.Identity;
using Electra.Persistence.RavenDB.Indexes;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;

namespace Electra.Auth;

public class Seeder
{
    // todo - update method signature to have only WebApplication param and get servicprovider and config from that
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILogger<Seeder>>();
        var userManager = sp.GetRequiredService<UserManager<ElectraUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<ElectraRole>>();
        var store = sp.GetRequiredService<IDocumentStore>();
        var db = sp.GetRequiredService<IAsyncDocumentSession>();
        
        var existing = roleManager.Roles.ToList();
        if (!existing.Any())
        {
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
        }

        await IndexCreation.CreateIndexesAsync(typeof(Users_ByRoleName).Assembly, store);

        // Seed admin user
        var admins = await db
            .Query<Users_ByRoleName.Result, Users_ByRoleName>()
            .Where(x => x.RoleNames.Contains("admin"))
            .OfType<IElectraUser>()
            .ToListAsync();


        
        if (!admins.Any())
        {
            var adminEmail = configuration["AdminUser:Email"] ?? "admin@aero.admin";
            var adminPassword = configuration["AdminUser:Password"] ?? "!Password123";

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
}