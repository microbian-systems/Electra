using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.Tests;

public class GenericEfRepositoryTests
{
    private readonly DbContextOptions<ElectraDbContext> options;
    private readonly ElectraDbContext context;
    private readonly GenericEntityFrameworkRepository<SampleEntity> repo;

    public GenericEfRepositoryTests()
    {
        options = new DbContextOptionsBuilder<ElectraDbContext>()
            .UseSqlite("Data Source=:memory:;Mode=Memory;Cache=Shared")
            .Options;

        context = new ElectraDbContext(options);
        var log = A.Fake<ILogger<GenericEntityFrameworkRepository<SampleEntity>>>();
        repo = new GenericEntityFrameworkRepository<SampleEntity>(context, log);
    }

    [Fact]
    public async Task GetPaged_ShouldReturnPagedData()
    {
        // Arrange
        var entities = new List<SampleEntity>
        {
            new() { Id = Guid.NewGuid(), Name = "Entity 1" },
            new() { Id = Guid.NewGuid(), Name = "Entity 2" },
            new() { Id = Guid.NewGuid(), Name = "Entity 3" },
            new() { Id = Guid.NewGuid(), Name = "Entity 4" },
            new() { Id = Guid.NewGuid(), Name = "Entity 5" },
            new() { Id = Guid.NewGuid(), Name = "Entity 6" },
            new() { Id = Guid.NewGuid(), Name = "Entity 7" },
            new() { Id = Guid.NewGuid(), Name = "Entity 8" },
            new() { Id = Guid.NewGuid(), Name = "Entity 9" },
            new() { Id = Guid.NewGuid(), Name = "Entity 10" },
            new() { Id = Guid.NewGuid(), Name = "Entity 11" },
            new() { Id = Guid.NewGuid(), Name = "Entity 12" },
            new() { Id = Guid.NewGuid(), Name = "Entity 13" },
            new() { Id = Guid.NewGuid(), Name = "Entity 14" },
            new() { Id = Guid.NewGuid(), Name = "Entity 15" },
            new() { Id = Guid.NewGuid(), Name = "Entity 16" },
            new() { Id = Guid.NewGuid(), Name = "Entity 17" },
            new() { Id = Guid.NewGuid(), Name = "Entity 18" },
            new() { Id = Guid.NewGuid(), Name = "Entity 19" },
            new() { Id = Guid.NewGuid(), Name = "Entity 20" }
        };

        await context.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetPaged(2, 5);
        result.Should().NotBeEmpty();
        result.Should().HaveCount(5);
        result[0].Name.Should().Be("Entity 15");
    }
}

public record SampleEntity : Entity
{
    public string Name { get; init; } = string.Empty;
}