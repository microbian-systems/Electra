using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Content;

public class PublishingWorkflowTests
{
    private readonly IContentRepository _contentRepo;
    private readonly IContentTypeRepository _contentTypeRepo;
    private readonly ISystemClock _clock;
    private readonly PublishingWorkflow _sut;
    private readonly DateTime _now = new(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

    public PublishingWorkflowTests()
    {
        _contentRepo = Substitute.For<IContentRepository>();
        _contentTypeRepo = Substitute.For<IContentTypeRepository>();
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(_now);
        _sut = new PublishingWorkflow(_contentRepo, _contentTypeRepo, _clock);
    }

    [Fact]
    public async Task SubmitForApprovalAsync_Changes_Draft_To_PendingApproval()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Draft };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        var result = await _sut.SubmitForApprovalAsync(contentId);

        // Assert
        result.Success.ShouldBeTrue();
        doc.Status.ShouldBe(PublishingStatus.PendingApproval);
        await _contentRepo.Received(1).SaveAsync(doc);
    }

    [Fact]
    public async Task PublishAsync_Succeeds_For_Draft_When_No_Approval_Required()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Draft, ContentTypeAlias = "page" };
        var contentType = new ContentTypeDocument { Alias = "page", RequiresApproval = false };
        
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentTypeRepo.GetByAliasAsync("page").Returns(contentType);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        var result = await _sut.PublishAsync(contentId);

        // Assert
        result.Success.ShouldBeTrue();
        doc.Status.ShouldBe(PublishingStatus.Published);
        doc.PublishedAt.ShouldBe(_now);
    }

    [Fact]
    public async Task PublishAsync_Fails_For_Draft_When_Approval_Required()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Draft, ContentTypeAlias = "page" };
        var contentType = new ContentTypeDocument { Alias = "page", RequiresApproval = true };
        
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentTypeRepo.GetByAliasAsync("page").Returns(contentType);

        // Act
        var result = await _sut.PublishAsync(contentId);

        // Assert
        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("Requires approval"));
        doc.Status.ShouldBe(PublishingStatus.Draft);
    }

    [Fact]
    public async Task ApproveAsync_Changes_PendingApproval_To_Approved()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.PendingApproval };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        var result = await _sut.ApproveAsync(contentId);

        // Assert
        result.Success.ShouldBeTrue();
        doc.Status.ShouldBe(PublishingStatus.Approved);
    }

    [Fact]
    public async Task RejectAsync_Changes_PendingApproval_To_Draft()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.PendingApproval };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        var result = await _sut.RejectAsync(contentId);

        // Assert
        result.Success.ShouldBeTrue();
        doc.Status.ShouldBe(PublishingStatus.Draft);
    }

    [Fact]
    public async Task PublishAsync_Sets_PublishedAt_On_First_Publish()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Approved };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        await _sut.PublishAsync(contentId);

        // Assert
        doc.PublishedAt.ShouldBe(_now);
    }

    [Fact]
    public async Task UnpublishAsync_Changes_Published_To_Draft_And_Preserves_PublishedAt()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var firstPublished = _now.AddDays(-1);
        var doc = new ContentDocument { Status = PublishingStatus.Published, PublishedAt = firstPublished };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        var result = await _sut.UnpublishAsync(contentId);

        // Assert
        result.Success.ShouldBeTrue();
        doc.Status.ShouldBe(PublishingStatus.Draft);
        doc.PublishedAt.ShouldBe(firstPublished);
    }

    [Fact]
    public async Task ExpireAsync_Changes_Published_To_Expired()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Published };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);
        _contentRepo.SaveAsync(doc).Returns(HandlerResult.Ok());

        // Act
        var result = await _sut.ExpireAsync(contentId);

        // Assert
        result.Success.ShouldBeTrue();
        doc.Status.ShouldBe(PublishingStatus.Expired);
    }

    [Fact]
    public async Task PublishAsync_On_Already_Published_Fails()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Published };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);

        // Act
        var result = await _sut.PublishAsync(contentId);

        // Assert
        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task SubmitForApproval_On_Published_Fails()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var doc = new ContentDocument { Status = PublishingStatus.Published };
        _contentRepo.GetByIdAsync(contentId).Returns(doc);

        // Act
        var result = await _sut.SubmitForApprovalAsync(contentId);

        // Assert
        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task Operation_On_NonExistent_Content_Fails()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        _contentRepo.GetByIdAsync(contentId).Returns((ContentDocument)null);

        // Act
        var result = await _sut.PublishAsync(contentId);

        // Assert
        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain(x => x.Contains("not found"));
    }
}
