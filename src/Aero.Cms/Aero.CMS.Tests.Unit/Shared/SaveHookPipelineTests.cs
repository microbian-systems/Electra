using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Shared;

public class SaveHookPipelineTests
{
    public class TestEntity { public string Name { get; set; } = string.Empty; }

    [Fact]
    public async Task RunBeforeAsync_ShouldExecuteHooksInPriorityOrder()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        var executionOrder = new List<int>();

        var hook1 = Substitute.For<IBeforeSaveHook<TestEntity>>();
        hook1.Priority.Returns(20);
        hook1.When(x => x.ExecuteAsync(entity)).Do(_ => executionOrder.Add(20));

        var hook2 = Substitute.For<IBeforeSaveHook<TestEntity>>();
        hook2.Priority.Returns(10);
        hook2.When(x => x.ExecuteAsync(entity)).Do(_ => executionOrder.Add(10));

        var pipeline = new SaveHookPipeline<TestEntity>(new[] { hook1, hook2 }, Array.Empty<IAfterSaveHook<TestEntity>>());

        // Act
        await pipeline.RunBeforeAsync(entity);

        // Assert
        executionOrder.ShouldBe(new[] { 10, 20 });
        await hook1.Received(1).ExecuteAsync(entity);
        await hook2.Received(1).ExecuteAsync(entity);
    }

    [Fact]
    public async Task RunAfterAsync_ShouldExecuteHooksInPriorityOrder()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        var executionOrder = new List<int>();

        var hook1 = Substitute.For<IAfterSaveHook<TestEntity>>();
        hook1.Priority.Returns(20);
        hook1.When(x => x.ExecuteAsync(entity)).Do(_ => executionOrder.Add(20));

        var hook2 = Substitute.For<IAfterSaveHook<TestEntity>>();
        hook2.Priority.Returns(10);
        hook2.When(x => x.ExecuteAsync(entity)).Do(_ => executionOrder.Add(10));

        var pipeline = new SaveHookPipeline<TestEntity>(Array.Empty<IBeforeSaveHook<TestEntity>>(), new[] { hook1, hook2 });

        // Act
        await pipeline.RunAfterAsync(entity);

        // Assert
        executionOrder.ShouldBe(new[] { 10, 20 });
        await hook1.Received(1).ExecuteAsync(entity);
        await hook2.Received(1).ExecuteAsync(entity);
    }

    [Fact]
    public async Task RunMethods_ShouldNotThrow_WhenNoHooksRegistered()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        var pipeline = new SaveHookPipeline<TestEntity>(Array.Empty<IBeforeSaveHook<TestEntity>>(), Array.Empty<IAfterSaveHook<TestEntity>>());

        // Act & Assert
        await Should.NotThrowAsync(async () => await pipeline.RunBeforeAsync(entity));
        await Should.NotThrowAsync(async () => await pipeline.RunAfterAsync(entity));
    }
}
