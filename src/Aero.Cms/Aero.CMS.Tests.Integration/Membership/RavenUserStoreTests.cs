using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;
using Aero.CMS.Core.Membership.Stores;
using Aero.CMS.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Xunit;

namespace Aero.CMS.Tests.Integration.Membership;

public class RavenUserStoreTests : RavenTestBase
{
    private readonly RavenUserStore _store;
    private readonly IDocumentStore _documentStore;

    public RavenUserStoreTests()
    {
        _documentStore = Store;
        _store = new RavenUserStore(_documentStore);
    }

    [Fact]
    public async Task CreateAsync_SavesUser()
    {
        var user = new CmsUser { UserName = "test", Email = "test@example.com" };
        var result = await _store.CreateAsync(user, CancellationToken.None);

        Assert.True(result.Succeeded);
        
        using var session = _documentStore.OpenAsyncSession();
        var savedUser = await session.LoadAsync<CmsUser>(user.Id, CancellationToken.None);
        Assert.NotNull(savedUser);
        Assert.Equal("test", savedUser.UserName);
    }

    [Fact]
    public async Task FindByIdAsync_RetrievesCorrectly()
    {
        var user = new CmsUser { UserName = "findme" };
        using (var session = _documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(user);
            await session.SaveChangesAsync();
        }

        var found = await _store.FindByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("findme", found.UserName);
    }

    [Fact]
    public async Task FindByNameAsync_RetrievesByNormalizedUserName()
    {
        var user = new CmsUser { UserName = "FindMe", NormalizedUserName = "FINDME" };
        using (var session = _documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(user);
            await session.SaveChangesAsync();
        }

        WaitForIndexing(_documentStore);

        var found = await _store.FindByNameAsync("FINDME", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("FindMe", found.UserName);
    }

    [Fact]
    public async Task FindByEmailAsync_RetrievesByNormalizedEmail()
    {
        var user = new CmsUser { Email = "test@example.com", NormalizedEmail = "TEST@EXAMPLE.COM" };
        using (var session = _documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(user);
            await session.SaveChangesAsync();
        }

        WaitForIndexing(_documentStore);

        var found = await _store.FindByEmailAsync("TEST@EXAMPLE.COM", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("test@example.com", found.Email);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var user = new CmsUser { UserName = "old" };
        using (var session = _documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(user);
            await session.SaveChangesAsync();
        }

        user.UserName = "new";
        await _store.UpdateAsync(user, CancellationToken.None);

        using (var session = _documentStore.OpenAsyncSession())
        {
            var updated = await session.LoadAsync<CmsUser>(user.Id);
            Assert.Equal("new", updated.UserName);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var user = new CmsUser { UserName = "delete" };
        using (var session = _documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(user);
            await session.SaveChangesAsync();
        }

        await _store.DeleteAsync(user, CancellationToken.None);

        using (var session = _documentStore.OpenAsyncSession())
        {
            var deleted = await session.LoadAsync<CmsUser>(user.Id);
            Assert.Null(deleted);
        }
    }

    [Fact]
    public async Task RoleOperations_WorkCorrectly()
    {
        var user = new CmsUser();
        
        await _store.AddToRoleAsync(user, "Admin", CancellationToken.None);
        Assert.True(await _store.IsInRoleAsync(user, "Admin", CancellationToken.None));
        Assert.Contains("Admin", await _store.GetRolesAsync(user, CancellationToken.None));

        await _store.RemoveFromRoleAsync(user, "Admin", CancellationToken.None);
        Assert.False(await _store.IsInRoleAsync(user, "Admin", CancellationToken.None));
    }

    [Fact]
    public async Task ClaimOperations_WorkCorrectly()
    {
        var user = new CmsUser();
        var claim = new Claim("type", "value");

        await _store.AddClaimsAsync(user, new[] { claim }, CancellationToken.None);
        var claims = await _store.GetClaimsAsync(user, CancellationToken.None);
        Assert.Single(claims);
        Assert.Equal("type", claims[0].Type);

        await _store.RemoveClaimsAsync(user, new[] { claim }, CancellationToken.None);
        claims = await _store.GetClaimsAsync(user, CancellationToken.None);
        Assert.Empty(claims);
    }

    [Fact]
    public async Task LockoutOperations_WorkCorrectly()
    {
        var user = new CmsUser();
        var end = DateTimeOffset.UtcNow.AddDays(1);

        await _store.SetLockoutEnabledAsync(user, true, CancellationToken.None);
        await _store.SetLockoutEndDateAsync(user, end, CancellationToken.None);
        
        Assert.True(await _store.GetLockoutEnabledAsync(user, CancellationToken.None));
        Assert.Equal(end, await _store.GetLockoutEndDateAsync(user, CancellationToken.None));

        await _store.IncrementAccessFailedCountAsync(user, CancellationToken.None);
        Assert.Equal(1, await _store.GetAccessFailedCountAsync(user, CancellationToken.None));

        await _store.ResetAccessFailedCountAsync(user, CancellationToken.None);
        Assert.Equal(0, await _store.GetAccessFailedCountAsync(user, CancellationToken.None));
    }
}
