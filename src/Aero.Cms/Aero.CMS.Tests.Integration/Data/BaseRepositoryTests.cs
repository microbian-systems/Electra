using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using Aero.CMS.Tests.Integration.Infrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Integration.Data;

public class TestDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
}

public class TestRepository : BaseRepository<TestDocument>
{
    public TestRepository(Raven.Client.Documents.IDocumentStore store, ISystemClock clock) : base(store, clock)
    {
    }
}

public class BaseRepositoryTests : RavenTestBase
{
    private readonly ISystemClock _clock;
    private readonly TestRepository _sut;
    private readonly DateTime _now = new(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

    public BaseRepositoryTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(_now);
        _sut = new TestRepository(Store, _clock);
    }

    [Fact]
    public async Task SaveAsync_Then_GetByIdAsync_Retrieves_Document()
    {
        // Arrange
        var doc = new TestDocument { Name = "Test" };
        var id = ((IEntity<Guid>)doc).Id;

        // Act
        var result = await _sut.SaveAsync(doc);
        result.Success.ShouldBeTrue(string.Join(", ", result.Errors));
        
        var retrieved = await _sut.GetByIdAsync(id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Test");
        retrieved.Id.ShouldBe(doc.Id);
    }

    [Fact]
    public async Task SaveAsync_Sets_AuditFields()
    {
        // Arrange
        var doc = new TestDocument { Name = "Test" };
        var id = ((IEntity<Guid>)doc).Id;

        // Act
        await _sut.SaveAsync(doc);
        var retrieved = await _sut.GetByIdAsync(id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.CreatedAt.ShouldBe(_now);
        retrieved.UpdatedAt.ShouldBe(_now);
    }

    [Fact]
    public async Task SaveAsync_Updates_UpdatedAt_On_Existing_Entity()
    {
        // Arrange
        var doc = new TestDocument { Name = "Test" };
        var id = ((IEntity<Guid>)doc).Id;
        await _sut.SaveAsync(doc);
        
        var later = _now.AddHours(1);
        _clock.UtcNow.Returns(later);

        // Act
        doc.Name = "Updated";
        await _sut.SaveAsync(doc);
        var retrieved = await _sut.GetByIdAsync(id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Updated");
        retrieved.CreatedAt.ShouldBe(_now);
        retrieved.UpdatedAt.ShouldBe(later);
    }

    [Fact]
    public async Task DeleteAsync_Removes_Document()
    {
        // Arrange
        var doc = new TestDocument { Name = "DeleteMe" };
        var id = ((IEntity<Guid>)doc).Id;
        await _sut.SaveAsync(doc);

        // Act
        var deleteResult = await _sut.DeleteAsync(id);
        var retrieved = await _sut.GetByIdAsync(id);

        // Assert
        deleteResult.Success.ShouldBeTrue();
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_For_NonExistent_Guid()
    {
        // Act
        var retrieved = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        retrieved.ShouldBeNull();
    }
}
