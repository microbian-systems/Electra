namespace Electra.Common.Tests;

using Electra.Common;

public class ProgressTrackerTests
{
    private readonly ILogger<ProgressTracker> logger;
    private readonly ProgressTracker progressTracker;
    private readonly Faker faker;

    public ProgressTrackerTests()
    {
        logger = A.Fake<ILogger<ProgressTracker>>();
        progressTracker = new ProgressTracker(@event, logger);
        faker = new Faker();
        return;

        void @event(double d) => logger.LogInformation("{d}", d);
    }

    [Fact]
    public async void ProcessShouldYieldItemsWhenItemsAreNotNullOrEmpty()
    {
        // Arrange
        var items = faker.Random.WordsArray(5).ToList();
        var processedItems = new List<string>();

        // Act
        await foreach (var item in progressTracker.Process(items, processedItems.Add))
        {
            // Assert
            processedItems.Should().Contain(item);
        }
    }

    [Fact]
    public async void ProcessShouldNotYieldItemsWhenItemsAreNullOrEmpty()
    {
        // Arrange
        List<string> items = [];
        var processedItems = new List<string>();

        // Act
        await foreach (var item in progressTracker.Process(items, processedItems.Add))
        {
            // Assert
            processedItems.Should().BeEmpty();
        }

        processedItems.Should().BeEmpty();
        items.Should().BeEmpty();
    }

    [Fact]
    public async void Process_Should_Invoke_ProgressUpdated_When_Item_Processed()
    {
        // Arrange
        var items = faker.Random.WordsArray(5).ToList();
        var lastPercentage = 0D;
        var progressUpdated = false;
        void @event(double d)
        {
            d.Should().BePositive();
            d.Should().BeGreaterThan(lastPercentage);
            progressUpdated = true;
            lastPercentage = d;
        }
        progressTracker.progressUpdated += @event;

        // Act
        await foreach (var item in progressTracker.Process(items, word =>
                       {
                           word.Should().NotBeEmpty();
                           word.Should().NotBeNull();
                       }))
        {
            // Assert
            item.Should().NotBeNull();
            progressUpdated.Should().BeTrue();
        }
        progressTracker.progressUpdated -= @event; // properly dispose of event
    }
}