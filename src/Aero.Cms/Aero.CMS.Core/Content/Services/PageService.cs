using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Extensions;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Content.Services;

public interface IPageService
{
    Task<List<ContentDocument>> GetPagesForSiteAsync(
        Guid siteId, CancellationToken ct = default);

    Task<ContentDocument?> GetBySlugAsync(
        string slug, CancellationToken ct = default);

    Task<ContentDocument?> GetByIdAsync(
        Guid pageId, CancellationToken ct = default);

    Task<HandlerResult<ContentDocument>> CreatePageAsync(
        Guid siteId, string name, string slug,
        string createdBy, CancellationToken ct = default);

    Task<HandlerResult> SavePageAsync(
        ContentDocument page, string savedBy, CancellationToken ct = default);

    Task<HandlerResult> DeletePageAsync(
        Guid pageId, string deletedBy, CancellationToken ct = default);
}

public class PageService(
    IContentRepository contentRepo,
    ISystemClock clock) : IPageService
{
    public async Task<List<ContentDocument>> GetPagesForSiteAsync(
        Guid siteId, CancellationToken ct = default)
    {
        var all = await contentRepo.GetByContentTypeAsync("page", ct);
        return all
            .Where(p => p.Properties.TryGetValue("siteId", out var id)
                        && id?.ToString() == siteId.ToString())
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public Task<ContentDocument?> GetBySlugAsync(
        string slug, CancellationToken ct = default)
        => contentRepo.GetBySlugAsync(slug, ct);

    public Task<ContentDocument?> GetByIdAsync(
        Guid pageId, CancellationToken ct = default)
        => contentRepo.GetByIdAsync(pageId, ct);

    public async Task<HandlerResult<ContentDocument>> CreatePageAsync(
        Guid siteId, string name, string slug,
        string createdBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return HandlerResult<ContentDocument>.Fail("Page name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            slug = SlugHelper.Generate(name);

        var existing = await contentRepo.GetBySlugAsync(slug, ct);
        if (existing is not null)
            return HandlerResult<ContentDocument>.Fail(
                $"A page with slug '{slug}' already exists.");

        var page = new ContentDocument
        {
            Name = name,
            Slug = slug.StartsWith('/') ? slug : $"/{slug}",
            ContentTypeAlias = "page",
            Status = PublishingStatus.Published,
            PublishedAt = clock.UtcNow,
            CreatedBy = createdBy
        };

        page.Properties["siteId"] = siteId.ToString();
        page.Properties["title"] = name;
        page.Properties["description"] = string.Empty;

        var result = await contentRepo.SaveAsync(page, ct);
        return result.Success
            ? HandlerResult<ContentDocument>.Ok(page)
            : HandlerResult<ContentDocument>.Fail(result.Errors);
    }

    public Task<HandlerResult> SavePageAsync(
        ContentDocument page, string savedBy, CancellationToken ct = default)
        => contentRepo.SaveAsync(page, ct);

    public async Task<HandlerResult> DeletePageAsync(
        Guid pageId, string deletedBy, CancellationToken ct = default)
    {
        var page = await contentRepo.GetByIdAsync(pageId, ct);
        if (page is null)
            return HandlerResult.Fail("Page not found.");
        return await contentRepo.DeleteAsync(pageId, ct);
    }
}
