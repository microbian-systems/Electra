using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using NSubstitute;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class ContentRepositoryWithHooksTests
{
    private readonly IDocumentStore _store;
    private readonly IAsyncDocumentSession _session;
    private readonly ISystemClock _clock;
    private readonly ContentRepository _repo;
    private readonly IBeforeSaveHook<ContentDocument> _beforeHook;
    private readonly IAfterSaveHook<ContentDocument> _afterHook;

    public ContentRepositoryWithHooksTests()
    {
        _store = Substitute.For<IDocumentStore>();
        _session = Substitute.For<IAsyncDocumentSession>();
        _store.OpenAsyncSession().Returns(_session);
        _clock = Substitute.For<ISystemClock>();
        
        _beforeHook = Substitute.For<IBeforeSaveHook<ContentDocument>>();
        _afterHook = Substitute.For<IAfterSaveHook<ContentDocument>>();
        
        var pipeline = new SaveHookPipeline<ContentDocument>(
            new[] { _beforeHook }, 
            new[] { _afterHook });
            
        _repo = new ContentRepository(_store, _clock, pipeline);
    }

    [Fact]
    public async Task SaveAsync_ShouldExecuteHooksInCorrectOrder()
    {
        // Arrange
        var doc = new ContentDocument { Name = "Test" };
        var executionOrder = new List<string>();

        // Set up ordered callbacks
        _beforeHook.When(x => x.ExecuteAsync(doc)).Do(_ => executionOrder.Add("before"));
        
        // We can't easily hook into base.SaveAsync's internal session.SaveChangesAsync() call 
        // without more complex setup, but we can verify before and after hooks are called 
        // around the save operation.
        
        _afterHook.When(x => x.ExecuteAsync(doc)).Do(_ => executionOrder.Add("after"));

        // Act
        var result = await _repo.SaveAsync(doc);

        // Assert
        result.Success.ShouldBeTrue();
        executionOrder.ShouldBe(new[] { "before", "after" });
        
        await _beforeHook.Received(1).ExecuteAsync(doc);
        await _afterHook.Received(1).ExecuteAsync(doc);
        await _session.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task SaveAsync_ShouldNotExecuteAfterHook_IfSaveFails()
    {
        // Arrange
        var doc = new ContentDocument { Name = "Test" };
        _session.When(x => x.SaveChangesAsync()).Do(_ => throw new Exception("Save failed"));

        // Act
        var result = await _repo.SaveAsync(doc);

        // Assert
        result.Success.ShouldBeFalse();
        await _beforeHook.Received(1).ExecuteAsync(doc);
        await _afterHook.DidNotReceive().ExecuteAsync(doc);
    }
}
