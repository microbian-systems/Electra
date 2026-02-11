using Bogus;
using Electra.Core;
using Electra.Models;
using Electra.Models.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents.Session;

namespace Electra.Auth.Tests;

public class IdentityTests : IClassFixture<TestWebAppFactory>, IDisposable
{
    private readonly HttpClient client;
    private readonly UserManager<ElectraUser> userManager;
    private readonly IServiceScope scope;
    readonly Faker faker = new();
    private readonly IAsyncDocumentSession db;

    public IdentityTests(TestWebAppFactory factory)
    {
        scope = factory.Services.CreateScope();
        client = factory.CreateClient(); 
        userManager = scope.ServiceProvider.GetRequiredService<UserManager<ElectraUser>>();
        db = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
    }

    [Fact]
    public async Task CanCreateUser()
    {
        var user = new ElectraUser()
        {
            Id = Snowflake.NewId().ToString(),
            FirstName = faker.Person.FirstName,
            LastName = faker.Person.LastName,
            UserName = faker.Person.UserName,
            Email = faker.Internet.Email(),
            CreatedBy = "system",
            ModifiedBy = "system",
            CreatedOn = DateTimeOffset.UtcNow,
            ModifiedOn = DateTimeOffset.UtcNow,
            ProfilePictureDataUrl = "",
            MiddleName = "",
            UserHandle = [],
            RefreshToken = "",
            Profile = new ElectraUserProfile
            {
                Username = "testuser",
                Headline = "💩 My Headline 💩",
                Location = "Los Angeles, CA",
                Bio = "This is my bio",
                Website = "https://example.com",
                CreatedBy = "system",
                ModifiedBy = "system",
                CreatedOn = DateTimeOffset.UtcNow,
                ModifiedOn = DateTimeOffset.UtcNow,
            },
            UserSettings = new UserSettingsModel()
            {
                Stuff = "{}",
                CreatedBy = "system",
                ModifiedBy = "system",
                CreatedOn = DateTimeOffset.UtcNow,
                ModifiedOn = DateTimeOffset.UtcNow,
            }
        };
        var res = await userManager.CreateAsync(user);
        res.Succeeded.Should().BeTrue();

        var saved = await userManager.FindByEmailAsync(user.Email);
        var saved2 = await userManager.FindByIdAsync(user.Id.ToString());
        var efuser = await db.LoadAsync<ElectraUser>(saved?.Id);
        var efuseremail = await db.Query<ElectraUser>()
            .Where(x => x.Email == user.Email).FirstOrDefaultAsync();
        
    }

    public void Dispose()
    {
        scope.Dispose();
    }
}