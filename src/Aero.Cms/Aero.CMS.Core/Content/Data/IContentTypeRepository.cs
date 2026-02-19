using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data.Interfaces;

namespace Aero.CMS.Core.Content.Data;

public interface IContentTypeRepository : IRepository<ContentTypeDocument>
{
    Task<ContentTypeDocument?> GetByAliasAsync(string alias, CancellationToken ct = default);
    Task<List<ContentTypeDocument>> GetAllAsync(CancellationToken ct = default);
}
