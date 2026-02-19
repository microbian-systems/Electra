using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Shared.Models;
using Aero.CMS.Core.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.CMS.Core.Content.Services;

public class MarkdownImportService
{
    private readonly IContentRepository _contentRepository;
    private readonly MarkdownRendererService _markdownRenderer;

    public MarkdownImportService(
        IContentRepository contentRepository,
        MarkdownRendererService markdownRenderer)
    {
        _contentRepository = contentRepository;
        _markdownRenderer = markdownRenderer;
    }

    public async Task<HandlerResult<ContentDocument>> ImportAsync(
        string markdown,
        CancellationToken ct = default)
    {
        // Parse frontmatter and body
        var (body, frontmatter) = _markdownRenderer.ParseWithFrontmatter(markdown);

        // Extract metadata
        frontmatter.TryGetValue("title", out var title);
        frontmatter.TryGetValue("slug", out var slug);
        frontmatter.TryGetValue("author", out var author);
        frontmatter.TryGetValue("tags", out var tags);

        // Generate slug from title if missing
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = SlugHelper.Generate(title ?? "Untitled");
        }

        // Create content document
        var document = new ContentDocument
        {
            Name = title ?? "Untitled",
            Slug = slug ?? string.Empty,
            ContentTypeAlias = "blogPost",
            Status = PublishingStatus.Draft,
            LanguageCode = "en",
            Blocks = new List<ContentBlock>
            {
                new MarkdownBlock { Markdown = body }
            }
        };

        if (!string.IsNullOrWhiteSpace(author))
            document.Properties["author"] = author;
        if (!string.IsNullOrWhiteSpace(tags))
            document.Properties["tags"] = tags;

        // Save
        await _contentRepository.SaveAsync(document, ct);

        return HandlerResult<ContentDocument>.Ok(document);
    }
}