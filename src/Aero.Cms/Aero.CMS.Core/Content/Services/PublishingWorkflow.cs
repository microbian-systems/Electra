using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Content.Services;

public class PublishingWorkflow : IPublishingWorkflow
{
    private readonly IContentRepository _contentRepo;
    private readonly IContentTypeRepository _contentTypeRepo;
    private readonly ISystemClock _clock;

    public PublishingWorkflow(
        IContentRepository contentRepo, 
        IContentTypeRepository contentTypeRepo, 
        ISystemClock clock)
    {
        _contentRepo = contentRepo;
        _contentTypeRepo = contentTypeRepo;
        _clock = clock;
    }

    public async Task<HandlerResult> SubmitForApprovalAsync(Guid contentId, CancellationToken ct = default)
    {
        var doc = await _contentRepo.GetByIdAsync(contentId, ct);
        if (doc == null) return HandlerResult.Fail("Content not found.");
        
        if (doc.Status != PublishingStatus.Draft)
            return HandlerResult.Fail($"Cannot submit for approval from status {doc.Status}.");

        doc.Status = PublishingStatus.PendingApproval;
        return await _contentRepo.SaveAsync(doc, ct);
    }

    public async Task<HandlerResult> ApproveAsync(Guid contentId, CancellationToken ct = default)
    {
        var doc = await _contentRepo.GetByIdAsync(contentId, ct);
        if (doc == null) return HandlerResult.Fail("Content not found.");

        if (doc.Status != PublishingStatus.PendingApproval)
            return HandlerResult.Fail($"Cannot approve from status {doc.Status}.");

        doc.Status = PublishingStatus.Approved;
        return await _contentRepo.SaveAsync(doc, ct);
    }

    public async Task<HandlerResult> RejectAsync(Guid contentId, CancellationToken ct = default)
    {
        var doc = await _contentRepo.GetByIdAsync(contentId, ct);
        if (doc == null) return HandlerResult.Fail("Content not found.");

        if (doc.Status != PublishingStatus.PendingApproval)
            return HandlerResult.Fail($"Cannot reject from status {doc.Status}.");

        doc.Status = PublishingStatus.Draft;
        return await _contentRepo.SaveAsync(doc, ct);
    }

    public async Task<HandlerResult> PublishAsync(Guid contentId, CancellationToken ct = default)
    {
        var doc = await _contentRepo.GetByIdAsync(contentId, ct);
        if (doc == null) return HandlerResult.Fail("Content not found.");

        if (doc.Status == PublishingStatus.Published)
            return HandlerResult.Fail("Content is already published.");

        if (doc.Status == PublishingStatus.Draft)
        {
            var contentType = await _contentTypeRepo.GetByAliasAsync(doc.ContentTypeAlias, ct);
            if (contentType?.RequiresApproval == true)
            {
                return HandlerResult.Fail("Requires approval before publishing.");
            }
        }

        doc.Status = PublishingStatus.Published;
        if (doc.PublishedAt == null)
        {
            doc.PublishedAt = _clock.UtcNow;
        }

        return await _contentRepo.SaveAsync(doc, ct);
    }

    public async Task<HandlerResult> UnpublishAsync(Guid contentId, CancellationToken ct = default)
    {
        var doc = await _contentRepo.GetByIdAsync(contentId, ct);
        if (doc == null) return HandlerResult.Fail("Content not found.");

        if (doc.Status != PublishingStatus.Published)
            return HandlerResult.Fail("Content is not published.");

        doc.Status = PublishingStatus.Draft;
        return await _contentRepo.SaveAsync(doc, ct);
    }

    public async Task<HandlerResult> ExpireAsync(Guid contentId, CancellationToken ct = default)
    {
        var doc = await _contentRepo.GetByIdAsync(contentId, ct);
        if (doc == null) return HandlerResult.Fail("Content not found.");

        if (doc.Status != PublishingStatus.Published)
            return HandlerResult.Fail("Only published content can be expired.");

        doc.Status = PublishingStatus.Expired;
        return await _contentRepo.SaveAsync(doc, ct);
    }
}
