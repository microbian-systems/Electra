using System.Security.Claims;
using Aero.Core.Identity;
using Aero.RavenDB.Identity;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Aero.RavenDB.Tests;

public class RoleStoreTests : RavenDbTestBase
{
    private readonly RoleStore<AeroRole> _roleStore;
    private readonly IOptions<RavenDbIdentityOptions> _options;

    public RoleStoreTests()
    {
        _options = Microsoft.Extensions.Options.Options.Create(new RavenDbIdentityOptions
        {
            AutoSaveChanges = true
        });

        _roleStore = new RoleStore<AeroRole>(DocumentStore.OpenAsyncSession(), _options);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Role()
    {
        // Arrange
        var role = new AeroRole("Admin");

        // Act
        var result = await _roleStore.CreateAsync(role, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        using var session = DocumentStore.OpenAsyncSession();
        var savedRole = await session.LoadAsync<AeroRole>(role.Id);
        savedRole.Should().NotBeNull();
        savedRole.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task FindByNameAsync_Should_Return_Role()
    {
        // Arrange
        var role = new AeroRole("Manager");
        await _roleStore.CreateAsync(role, CancellationToken.None);

        // Act
        var foundRole = await _roleStore.FindByNameAsync("Manager", CancellationToken.None);

        // Assert
        foundRole.Should().NotBeNull();
        foundRole.Name.Should().Be("Manager");
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Role_Properties()
    {
        // Arrange
        var role = new AeroRole("OldRole");
        await _roleStore.CreateAsync(role, CancellationToken.None);

        // Act
        role.Name = "NewRole";
        var result = await _roleStore.UpdateAsync(role, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        using var session = DocumentStore.OpenAsyncSession();
        var updatedRole = await session.LoadAsync<AeroRole>(role.Id);
        updatedRole.Name.Should().Be("NewRole");
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Role()
    {
        // Arrange
        var role = new AeroRole("DeleteMe");
        await _roleStore.CreateAsync(role, CancellationToken.None);

        // Act
        var result = await _roleStore.DeleteAsync(role, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        using var session = DocumentStore.OpenAsyncSession();
        var deletedRole = await session.LoadAsync<AeroRole>(role.Id);
        deletedRole.Should().BeNull();
    }

    [Fact]
    public async Task AddClaimAsync_Should_Add_Claim_To_Role()
    {
        // Arrange
        var role = new AeroRole("ClaimRole");
        await _roleStore.CreateAsync(role, CancellationToken.None);
        var claim = new Claim("Permission", "ViewReports");

        // Act
        await _roleStore.AddClaimAsync(role, claim, CancellationToken.None);

        // Assert
        using var session = DocumentStore.OpenAsyncSession();
        var updatedRole = await session.LoadAsync<AeroRole>(role.Id);
        updatedRole.Claims.Should().Contain(c => c.ClaimType == "Permission" && c.ClaimValue == "ViewReports");
    }
}
