using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Tests.Integration.Infrastructure;
using NSubstitute;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aero.CMS.Tests.Integration.Content;

public class ContentTypeRepositoryTests : RavenTestBase
{
    private readonly ISystemClock _clock;
    private readonly ContentTypeRepository _sut;
    private readonly DateTime _now = new(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

    public ContentTypeRepositoryTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(_now);
        _sut = new ContentTypeRepository(Store, _clock, NullLogger<ContentTypeRepository>.Instance);
    }

    [Fact]
    public async Task GetByAliasAsync_Returns_Correct_Document()
    {
        // Arrange
        var doc = new ContentTypeDocument { Name = "Page", Alias = "page" };
        await _sut.SaveAsync(doc);

        // Act
        var retrieved = await _sut.GetByAliasAsync("page");

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Alias.ShouldBe("page");
        retrieved.Name.ShouldBe("Page");
    }

    [Fact]
    public async Task GetByAliasAsync_Returns_Null_For_Unknown_Alias()
    {
        // Act
        var retrieved = await _sut.GetByAliasAsync("unknown");

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Documents()
    {
        // Arrange
        await _sut.SaveAsync(new ContentTypeDocument { Name = "Type 1", Alias = "t1" });
        await _sut.SaveAsync(new ContentTypeDocument { Name = "Type 2", Alias = "t2" });

        // Act
        var all = await _sut.GetAllAsync();

        // Assert
        all.Count.ShouldBeGreaterThanOrEqualTo(2);
        all.Any(x => x.Alias == "t1").ShouldBeTrue();
        all.Any(x => x.Alias == "t2").ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_Then_GetByAlias_Retrieves_With_Properties_Intact()
    {
        // Arrange
        var doc = new ContentTypeDocument 
        { 
            Name = "Page", 
            Alias = "page",
            Properties = new List<ContentTypeProperty>
            {
                new ContentTypeProperty 
                { 
                    Name = "Body", 
                    Alias = "body", 
                    PropertyType = PropertyType.RichText,
                    Required = true,
                    SortOrder = 1
                }
            }
        };

        await _sut.SaveAsync(doc);

        // Act
        var retrieved = await _sut.GetByAliasAsync("page");

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Properties.Count.ShouldBe(1);
        retrieved.Properties[0].Name.ShouldBe("Body");
        retrieved.Properties[0].PropertyType.ShouldBe(PropertyType.RichText);
        retrieved.Properties[0].Required.ShouldBeTrue();
    }
}
