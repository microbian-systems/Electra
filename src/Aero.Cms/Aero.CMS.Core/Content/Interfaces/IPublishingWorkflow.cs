using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Content.Interfaces;

public interface IPublishingWorkflow
{
    Task<HandlerResult> SubmitForApprovalAsync(Guid contentId, CancellationToken ct = default);
    Task<HandlerResult> ApproveAsync(Guid contentId, CancellationToken ct = default);
    Task<HandlerResult> RejectAsync(Guid contentId, CancellationToken ct = default);
    Task<HandlerResult> PublishAsync(Guid contentId, CancellationToken ct = default);
    Task<HandlerResult> UnpublishAsync(Guid contentId, CancellationToken ct = default);
    Task<HandlerResult> ExpireAsync(Guid contentId, CancellationToken ct = default);
}
