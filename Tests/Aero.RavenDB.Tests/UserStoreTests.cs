using System.Security.Claims;
using Aero.Core;
using Aero.Core.Identity;
using Aero.Models.Entities;
using Aero.RavenDB.Identity;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client;

namespace Aero.RavenDB.Tests;

public class UserStoreTests : RavenDbTestBase
{
    private readonly UserStore<AeroUser, AeroRole> _userStore;
    private readonly ILogger<UserStore<AeroUser, AeroRole>> _logger;
    private readonly IOptions<RavenDbIdentityOptions> _options;

    public UserStoreTests()
    {
        _logger = A.Fake<ILogger<UserStore<AeroUser, AeroRole>>>();
        _options = Microsoft.Extensions.Options.Options.Create(new RavenDbIdentityOptions
        {
            AutoSaveChanges = true,
            UseStaticIndexes = false
        });

        _userStore = new UserStore<AeroUser, AeroRole>(DocumentStore.OpenAsyncSession(), _logger, _options);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_User_And_Email_Reservation()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            CreatedBy = "System"
        };

        // Act
        var result = await _userStore.CreateAsync(user, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        using var session = DocumentStore.OpenAsyncSession();
        var savedUser = await session.LoadAsync<AeroUser>(user.Id);
        savedUser.Should().NotBeNull();
        savedUser.Email.Should().Be("test@example.com");

        var compareExchangeKey = Conventions.CompareExchangeKeyFor("test@example.com");
        var reservation = await DocumentStore.Operations.SendAsync(new Raven.Client.Documents.Operations.CompareExchange.GetCompareExchangeValueOperation<string>(compareExchangeKey));
        reservation.Should().NotBeNull();
        reservation.Value.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateAsync_Should_Fail_If_Email_Already_Reserved()
    {
        // Arrange
        var user1 = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "user1",
            Email = "duplicate@example.com",
            FirstName = "User",
            LastName = "One",
            CreatedBy = "System"
        };
        await _userStore.CreateAsync(user1, CancellationToken.None);

        var user2 = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "user2",
            Email = "duplicate@example.com",
            FirstName = "User",
            LastName = "Two",
            CreatedBy = "System"
        };

        // Act
        var result = await _userStore.CreateAsync(user2, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task FindByIdAsync_Should_Return_User()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "findme",
            Email = "findme@example.com",
            FirstName = "Find",
            LastName = "Me",
            CreatedBy = "System"
        };
        await _userStore.CreateAsync(user, CancellationToken.None);

        // Act
        var foundUser = await _userStore.FindByIdAsync(user.Id, CancellationToken.None);

        // Assert
        foundUser.Should().NotBeNull();
        foundUser.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_User_And_Sync_Username_If_Match()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "old@example.com",
            Email = "old@example.com",
            FirstName = "Old",
            LastName = "Name",
            CreatedBy = "System"
        };
        await _userStore.CreateAsync(user, CancellationToken.None);

        // Update properties
        user.Email = "new@example.com";
        user.FirstName = "New";

        // Act
        var result = await _userStore.UpdateAsync(user, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        
        using var session = DocumentStore.OpenAsyncSession();
        var updatedUser = await session.LoadAsync<AeroUser>(user.Id);
        updatedUser.Email.Should().Be("new@example.com");
        updatedUser.UserName.Should().Be("new@example.com");
        updatedUser.FirstName.Should().Be("New");

        // Verify old reservation is gone and new exists
        var oldKey = Conventions.CompareExchangeKeyFor("old@example.com");
        var newKey = Conventions.CompareExchangeKeyFor("new@example.com");
        
        var oldReservation = await DocumentStore.Operations.SendAsync(new Raven.Client.Documents.Operations.CompareExchange.GetCompareExchangeValueOperation<string>(oldKey));
        oldReservation.Should().BeNull();

        var newReservation = await DocumentStore.Operations.SendAsync(new Raven.Client.Documents.Operations.CompareExchange.GetCompareExchangeValueOperation<string>(newKey));
        newReservation.Should().NotBeNull();
        newReservation.Value.Should().Be(user.Id);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_User_And_Reservation()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "deleteme",
            Email = "delete@example.com",
            FirstName = "Delete",
            LastName = "Me",
            CreatedBy = "System"
        };
        await _userStore.CreateAsync(user, CancellationToken.None);

        // Act
        var result = await _userStore.DeleteAsync(user, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();

        using var session = DocumentStore.OpenAsyncSession();
        var deletedUser = await session.LoadAsync<AeroUser>(user.Id);
        deletedUser.Should().BeNull();

        var compareExchangeKey = Conventions.CompareExchangeKeyFor("delete@example.com");
        var reservation = await DocumentStore.Operations.SendAsync(new Raven.Client.Documents.Operations.CompareExchange.GetCompareExchangeValueOperation<string>(compareExchangeKey));
        reservation.Should().BeNull();
    }

    [Fact]
    public async Task AddToRoleAsync_Should_Add_Role_To_User_And_User_To_Role()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = $"users/{Snowflake.NewId()}",
            UserName = "roleuser",
            Email = "role@example.com",
            FirstName = "Role",
            LastName = "User",
            CreatedBy = "System"
        };
        await _userStore.CreateAsync(user, CancellationToken.None);

        // Act
        await _userStore.AddToRoleAsync(user, "Admin", CancellationToken.None);

        // Assert
        user.GetRolesList().Should().Contain("admin"); // Store implementation lowers it if role doesn't exist

        using var session = DocumentStore.OpenAsyncSession();
        var roleId = Conventions.RoleIdFor<AeroRole>("Admin", DocumentStore);
        var role = await session.LoadAsync<AeroRole>(roleId);
        role.Should().NotBeNull();
        role.Users.Should().Contain(user.Id);
    }

    [Fact]
    public async Task Claims_Operations_Should_Work()
    {
        // Arrange
        var user = new AeroUser { Id = "users/claims", UserName = "c", Email = "c@e.com", FirstName = "f", LastName = "l", CreatedBy = "s" };
        await _userStore.CreateAsync(user, default);
        var claim = new Claim("type1", "value1");

        // Act - Add
        await _userStore.AddClaimsAsync(user, new[] { claim }, default);

        // Assert - Add
        user.Claims.Should().HaveCount(1);
        using (var session = DocumentStore.OpenAsyncSession())
        {
            var refreshedUser = await session.LoadAsync<AeroUser>(user.Id);
            refreshedUser.Claims.Should().HaveCount(1);
            refreshedUser.Claims.First().ClaimType.Should().Be("type1");
        }

        // Act - Replace
        var newClaim = new Claim("type1", "value2");
        await _userStore.ReplaceClaimAsync(user, claim, newClaim, default);

        // Assert - Replace
        user.Claims.Should().HaveCount(1);
        user.Claims.First().ClaimValue.Should().Be("value2");

        // Act - Remove
        await _userStore.RemoveClaimsAsync(user, new[] { newClaim }, default);

        // Assert - Remove
        user.Claims.Should().BeEmpty();
    }

    [Fact]
    public async Task Logins_Operations_Should_Work()
    {
        // Arrange
        var user = new AeroUser { Id = "users/logins", UserName = "l", Email = "l@e.com", FirstName = "f", LastName = "l", CreatedBy = "s" };
        await _userStore.CreateAsync(user, default);
        var login = new UserLoginInfo("Google", "g123", "Google Login");

        // Act - Add
        await _userStore.AddLoginAsync(user, login, default);

        // Assert - Add
        user.Logins.Should().HaveCount(1);
        using (var session = DocumentStore.OpenAsyncSession())
        {
            var refreshedUser = await session.LoadAsync<AeroUser>(user.Id);
            refreshedUser.Logins.Should().HaveCount(1);
            refreshedUser.Logins.First().LoginProvider.Should().Be("Google");
        }

        // Act - Remove
        await _userStore.RemoveLoginAsync(user, "Google", "g123", default);

        // Assert - Remove
        user.Logins.Should().BeEmpty();
    }

    [Fact]
    public async Task Tokens_Operations_Should_Work()
    {
        // Arrange
        var user = new AeroUser { Id = "users/tokens", UserName = "t", Email = "t@e.com", FirstName = "f", LastName = "l", CreatedBy = "s" };
        await _userStore.CreateAsync(user, default);

        // Act - Set
        await _userStore.SetTokenAsync(user, "p1", "n1", "v1", default);

        // Assert - Set
        user.Tokens.Should().HaveCount(1);
        using (var session = DocumentStore.OpenAsyncSession())
        {
            var refreshedUser = await session.LoadAsync<AeroUser>(user.Id);
            refreshedUser.Tokens.Should().HaveCount(1);
            refreshedUser.Tokens.First().Value.Should().Be("v1");
        }

        // Act - Remove
        await _userStore.RemoveTokenAsync(user, "p1", "n1", default);

        // Assert - Remove
        user.Tokens.Should().BeEmpty();
    }
}
