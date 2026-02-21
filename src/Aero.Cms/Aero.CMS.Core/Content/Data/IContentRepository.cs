using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data.Interfaces;

namespace Aero.CMS.Core.Content.Data;

public interface IContentRepository : IRepository<ContentDocument>
{
    Task<ContentDocument?> GetBySlugAsync(string slug, bool waitForNonStaleResults = false, CancellationToken ct = default);
    Task<List<ContentDocument>> GetChildrenAsync(Guid? parentId, PublishingStatus? statusFilter = null, bool waitForNonStaleResults = false, CancellationToken ct = default);
    Task<List<ContentDocument>> GetByContentTypeAsync(string contentTypeAlias, bool waitForNonStaleResults = false, CancellationToken ct = default);
}
