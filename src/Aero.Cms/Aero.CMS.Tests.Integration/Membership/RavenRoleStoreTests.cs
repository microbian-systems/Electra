using System.Threading;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;
using Aero.CMS.Core.Membership.Stores;
using Aero.CMS.Tests.Integration.Infrastructure;
using Raven.Client.Documents;
using Xunit;

namespace Aero.CMS.Tests.Integration.Membership;

public class RavenRoleStoreTests : RavenTestBase
{
    private readonly RavenRoleStore _store;
    private readonly IDocumentStore _documentStore;

    public RavenRoleStoreTests()
    {
        _documentStore = Store;
        _store = new RavenRoleStore(_documentStore);
    }

    [Fact]
    public async Task CreateAsync_SavesRole()
    {
        var role = new CmsRole { Name = "Admin", NormalizedName = "ADMIN" };
        var result = await _store.CreateAsync(role, CancellationToken.None);

        Assert.True(result.Succeeded);
        
        using var session = _documentStore.OpenAsyncSession();
        var savedRole = await session.LoadAsync<CmsRole>(role.Id);
        Assert.NotNull(savedRole);
        Assert.Equal("Admin", savedRole.Name);
    }

    [Fact]
    public async Task FindByNameAsync_RetrievesByNormalizedName()
    {
        var role = new CmsRole { Name = "Editor", NormalizedName = "EDITOR" };
        using (var session = _documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(role);
            await session.SaveChangesAsync();
        }

        WaitForIndexing(_documentStore);

        var found = await _store.FindByNameAsync("EDITOR", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("Editor", found.Name);
    }
}
