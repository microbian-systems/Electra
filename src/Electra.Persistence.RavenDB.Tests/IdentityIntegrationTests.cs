using Electra.Core;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence.RavenDB.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using Xunit;

namespace Electra.Persistence.RavenDB.Tests;

public class IdentityIntegrationTests : RavenDbTestBase
{
    private readonly IServiceProvider _serviceProvider;

    public IdentityIntegrationTests()
    {
        var services = new ServiceCollection();

        // 1. Add Logging
        services.AddLogging(builder => builder.AddConsole());

        // 2. Add RavenDB Singleton
        services.AddSingleton(DocumentStore);

        // 3. Add Scoped Session
        services.AddScoped(sp => DocumentStore.OpenAsyncSession());

        // 4. Add Identity
        services.AddIdentity<ElectraUser, ElectraRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            // Disable complex password requirements for easier testing if needed,
            // though standard tests should work.
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 1;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
        })
        .AddRavenDbIdentityStores<ElectraUser, ElectraRole>(options =>
        {
            options.AutoSaveChanges = true;
        })
        .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task UserManager_Should_Add_Update_Search_And_Delete_User()
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();

        // --- 1. ADD ---
        var user = new ElectraUser
        {
            Id = Snowflake.NewId().ToString(),
            UserName = "integration_test_user",
            Email = "integration@example.com",
            FirstName = "Integration",
            LastName = "Test",
            CreatedBy = "System"
        };

        var createResult = await userManager.CreateAsync(user, "Password123!");
        createResult.Succeeded.Should().BeTrue();

        // --- 2. SEARCH ---
        // UserManager.FindByNameAsync might normalize the input name. 
        // Our UserStore normalizes to lowercase in SetNormalizedUserNameAsync.
        var foundUser = await userManager.FindByNameAsync(user.UserName.ToLowerInvariant());
        foundUser.Should().NotBeNull();
        foundUser!.Email.Should().Be(user.Email);

        // --- 3. UPDATE ---
        foundUser.FirstName = "Updated";
        var updateResult = await userManager.UpdateAsync(foundUser);
        updateResult.Succeeded.Should().BeTrue();

        // Verify update persisted
        var updatedUser = await userManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Updated");

        // --- 4. DELETE ---
        var deleteResult = await userManager.DeleteAsync(updatedUser);
        deleteResult.Succeeded.Should().BeTrue();

        var deletedUser = await userManager.FindByIdAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task UserManager_And_RoleManager_Integration_Should_Work()
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ElectraRole>>();

        // Arrange
        var user = new ElectraUser
        {
            Id = Snowflake.NewId().ToString(),
            UserName = "role_test_user",
            Email = "role_test@example.com",
            FirstName = "Role",
            LastName = "Test",
            CreatedBy = "System"
        };
        await userManager.CreateAsync(user);

        var roleName = "TestRole";
        await roleManager.CreateAsync(new ElectraRole { Name = roleName });

        // Act
        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);

        // Assert
        addToRoleResult.Succeeded.Should().BeTrue();

        var roles = await userManager.GetRolesAsync(user);
        roles.Should().Contain(roleName);

        var isInRole = await userManager.IsInRoleAsync(user, roleName);
        isInRole.Should().BeTrue();
    }
}
