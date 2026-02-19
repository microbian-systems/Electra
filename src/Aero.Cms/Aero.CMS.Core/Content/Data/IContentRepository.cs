using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data.Interfaces;

namespace Aero.CMS.Core.Content.Data;

public interface IContentRepository : IRepository<ContentDocument>
{
    Task<ContentDocument?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<ContentDocument>> GetChildrenAsync(Guid? parentId, PublishingStatus? statusFilter = null, CancellationToken ct = default);
    Task<List<ContentDocument>> GetByContentTypeAsync(string contentTypeAlias, CancellationToken ct = default);
}
