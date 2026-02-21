# Aero CMS — MVP Spec
# Site → Page → Section → Block

> Agent executable. No auth. Runnable pages. Working editor.
> Build on top of: AeroCMS-Agent-Spec.md (Phases 0–16 must be complete and green)

---

## What "MVP" Means Here

A functioning CMS means exactly this and nothing more:

1. Start the app — a blank site boots
2. Open the admin — create a Site
3. Create a Page under that Site
4. Add Sections to that Page (layout rows)
5. Add Blocks to each Section (content)
6. Click "View Page" — the page renders publicly at its slug
7. Edit a block inline — save — refresh public page — change is live

No login. No auth middleware. No roles. No publishing workflow gates.
Any request to the admin works. Any page is publicly visible.

These are deliberately stripped for MVP. The architecture keeps all
the interfaces so auth, workflow, and multi-site slot in later with
zero structural change.

---

## The Hierarchy — Precise Definitions

```
Site
  A named container. Holds global config: name, default layout,
  base URL. There is one Site document for MVP. Multi-site later.

  └── Page  (many per Site)
        A routable document with a slug. Has metadata (title, description).
        The unit a visitor sees at a URL.

        └── Section  (ordered list, many per Page)
              A full-width horizontal zone on the page.
              Has a layout hint: Full, TwoColumn, ThreeColumn, Sidebar.
              Sections are the drag-drop reorder targets at the page level.

              └── Block  (ordered list, many per Section)
                    A discrete piece of content within a Section column.
                    Types: RichText, Image, Hero, Quote, Markdown, HTML.
                    Blocks are drag-drop reorderable within a Section column.
```

### Why Section Exists Between Page and Block

Without Section, every block is a full-width row. Section is the
layout primitive — it lets editors put a RichText and an Image
side-by-side in a two-column layout without needing a full
CSS-grid block type. This is the minimum viable page builder concept.

Section maps directly to `ICompositeContentBlock` from the existing
architecture with a fixed layout schema. No new concepts required.

---

## Data Model Mapping to Existing Architecture

The existing `ContentDocument` + `ContentBlock` hierarchy handles
everything. The MVP just defines three specific aliases:

```
ContentDocument (ContentTypeAlias = "page")
  ├── Properties["siteId"]      → Guid of parent SiteDocument
  ├── Properties["title"]       → page <title> and H1
  ├── Properties["description"] → meta description
  └── Blocks[]                  → List<SectionBlock>
        Each SectionBlock is ICompositeContentBlock
        SectionBlock.Properties["layout"] → "full" | "twoColumn" |
                                            "threeColumn" | "sidebar"
        SectionBlock.Children[]  → List<ColumnBlock>
              Each ColumnBlock is ICompositeContentBlock
              ColumnBlock.Properties["colIndex"] → 0, 1, 2
              ColumnBlock.Children[] → List<ContentBlock> (leaf blocks)
```

And one new document:

```
SiteDocument : AuditableDocument
  ├── Name         string
  ├── BaseUrl      string    (e.g. "https://localhost:5001")
  ├── DefaultLayout string   (e.g. "MainLayout")
  └── Description  string?
```

---

## Project Structure for MVP

Add to existing solution:

```
Aero.CMS.sln (existing)
├── Aero.CMS.Core          (existing — no changes)
├── Aero.CMS.Components    (existing — add admin UI components here)
├── Aero.CMS.Routing       (existing — no changes)
├── Aero.CMS.Web           (existing — add public views here)
├── Aero.CMS.Tests.Unit    (existing)
└── Aero.CMS.Tests.Integration (existing)
```

No new projects. MVP fits inside the existing structure.

Files added in this spec:

```
Aero.CMS.Core/
  Site/
    Models/SiteDocument.cs
    Data/SiteRepository.cs
  Content/
    Models/Blocks/SectionBlock.cs
    Models/Blocks/ColumnBlock.cs
    Models/Blocks/HtmlBlock.cs

Aero.CMS.Components/
  Admin/
    Layout/
      AdminLayout.razor
      AdminNavBar.razor
    SiteSection/
      SiteEditor.razor
    PageSection/
      PageList.razor
      PageEditor.razor
      NewPageDialog.razor
      DeletePageDialog.razor
    BlockCanvas/
      BlockCanvas.razor
      BlockWrapper.razor
      BlockDropZone.razor
      AddSectionButton.razor
      AddBlockButton.razor
      BlockEditContext.cs
      Toolbar/
        BlockToolbar.razor
    BlockEditors/
      RichTextBlockEditor.razor
      ImageBlockEditor.razor
      HeroBlockEditor.razor
      QuoteBlockEditor.razor
      MarkdownBlockEditor.razor
      HtmlBlockEditor.razor
    BlockPreviews/
      RichTextBlockPreview.razor
      ImageBlockPreview.razor
      HeroBlockPreview.razor
      QuoteBlockPreview.razor
      MarkdownBlockPreview.razor
      HtmlBlockPreview.razor

Aero.CMS.Web/
  Layouts/
    PublicLayout.razor
  Pages/
    PublicPageView.razor
  Blocks/
    RichTextBlockView.razor
    ImageBlockView.razor
    HeroBlockView.razor
    QuoteBlockView.razor
    MarkdownBlockView.razor
    HtmlBlockView.razor
    SectionBlockView.razor
    ColumnBlockView.razor
```

---

# PHASE MVP-1 — Site Document

Goal: SiteDocument model and repository. First-run creates default site.
Prerequisites: Agent spec Phases 0–16 complete and green.

---

## Task MVP-1.1 — SiteDocument

FILE: Aero.CMS.Core/Site/Models/SiteDocument.cs

```csharp
namespace Aero.CMS.Core.Site.Models;

public class SiteDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultLayout { get; set; } = "PublicLayout";
    public string? Description { get; set; }
    public string? FaviconMediaId { get; set; }
    public string? LogoMediaId { get; set; }
    public string? FooterText { get; set; }
    public bool IsDefault { get; set; } = true;
}
```

---

## Task MVP-1.2 — SiteRepository

FILE: Aero.CMS.Core/Site/Data/SiteRepository.cs

```csharp
namespace Aero.CMS.Core.Site.Data;

public interface ISiteRepository : IRepository<SiteDocument>
{
    Task<SiteDocument?> GetDefaultAsync(CancellationToken ct = default);
    Task<List<SiteDocument>> GetAllAsync(CancellationToken ct = default);
}

public class SiteRepository(IDocumentStore store)
    : BaseRepository<SiteDocument>(store), ISiteRepository
{
    public async Task<SiteDocument?> GetDefaultAsync(CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<SiteDocument>()
            .Where(x => x.IsDefault)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<SiteDocument>> GetAllAsync(CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<SiteDocument>()
            .ToListAsync(ct);
    }
}
```

---

## Task MVP-1.3 — First-Run Site Seed

FILE: Aero.CMS.Core/Site/Services/SiteBootstrapService.cs

On app startup, if no SiteDocument exists, create one with defaults.
Run as an `IHostedService` that executes once on startup.

```csharp
namespace Aero.CMS.Core.Site.Services;

public class SiteBootstrapService(
    ISiteRepository siteRepo,
    IContentTypeRepository contentTypeRepo) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var existing = await siteRepo.GetDefaultAsync(ct);
        if (existing is not null) return;

        // Seed default site
        var site = new SiteDocument
        {
            Name = "My Aero Site",
            BaseUrl = "https://localhost:5001",
            Description = "Built with Aero CMS",
            IsDefault = true,
            CreatedBy = "system"
        };
        await siteRepo.SaveAsync(site, "system", ct);

        // Seed "page" content type if absent
        var pageType = await contentTypeRepo.GetByAliasAsync("page", ct);
        if (pageType is not null) return;

        var pageContentType = new ContentTypeDocument
        {
            Name = "Page",
            Alias = "page",
            Description = "Standard page type",
            AllowAtRoot = true,
            RequiresApproval = false,
            CreatedBy = "system",
            Properties =
            [
                new ContentTypeProperty
                {
                    Name = "Title",
                    Alias = "title",
                    PropertyType = PropertyType.Text,
                    Required = true,
                    SortOrder = 0
                },
                new ContentTypeProperty
                {
                    Name = "Description",
                    Alias = "description",
                    PropertyType = PropertyType.TextArea,
                    Required = false,
                    SortOrder = 1
                }
            ]
        };
        await contentTypeRepo.SaveAsync(pageContentType, "system", ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### TEST: MVP-1.1 — Unit

FILE: Aero.CMS.Tests.Unit/Site/SiteDocumentTests.cs

```
MUST TEST:
- New SiteDocument has IsDefault = true by default
- New SiteDocument has DefaultLayout = "PublicLayout"
- New SiteDocument inherits non-empty Guid Id from AuditableDocument
```

### TEST: MVP-1.2 — Integration

FILE: Aero.CMS.Tests.Integration/Site/SiteRepositoryTests.cs

```
MUST TEST:
- GetDefaultAsync returns site where IsDefault = true
- GetDefaultAsync returns null when no sites exist
- GetAllAsync returns all saved sites
- SaveAsync then GetDefaultAsync retrieves with all fields intact
- Two sites exist: GetDefaultAsync returns the one with IsDefault = true
```

### TEST: MVP-1.3 — Integration

FILE: Aero.CMS.Tests.Integration/Site/SiteBootstrapServiceTests.cs

```
MUST TEST:
- StartAsync creates default SiteDocument when none exists
- StartAsync creates "page" ContentTypeDocument when none exists
- StartAsync does NOT create duplicate site when one already exists
- StartAsync does NOT create duplicate content type when one already exists
- After StartAsync, GetDefaultAsync returns non-null
```

PHASE MVP-1 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Site"
  dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Site"
  Expected: All pass. Zero failures.

---

# PHASE MVP-2 — Section and Column Blocks

Goal: SectionBlock and ColumnBlock — the layout layer between Page and leaf blocks.
Prerequisites: MVP-1 green.

---

## Task MVP-2.1 — SectionLayout Enum

FILE: Aero.CMS.Core/Content/Models/SectionLayout.cs

```csharp
namespace Aero.CMS.Core.Content.Models;

public enum SectionLayout
{
    Full = 0,          // one column, 100%
    TwoColumn = 1,     // 50/50
    ThreeColumn = 2,   // 33/33/33
    Sidebar = 3        // 70/30 — content + sidebar
}
```

---

## Task MVP-2.2 — SectionBlock

FILE: Aero.CMS.Core/Content/Models/Blocks/SectionBlock.cs

SectionBlock is a composite that contains ColumnBlocks.
The number of ColumnBlocks it initialises equals the column count
implied by its Layout.

```csharp
namespace Aero.CMS.Core.Content.Models.Blocks;

public class SectionBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "sectionBlock";

    public SectionBlock() { Type = BlockType; }

    public SectionLayout Layout
    {
        get => Enum.TryParse<SectionLayout>(
                   Properties.GetValueOrDefault("layout")?.ToString(),
                   out var l) ? l : SectionLayout.Full;
        set => Properties["layout"] = value.ToString();
    }

    public string? BackgroundColor
    {
        get => Properties.GetValueOrDefault("backgroundColor")?.ToString();
        set => Properties["backgroundColor"] = value ?? string.Empty;
    }

    public string? CssClass
    {
        get => Properties.GetValueOrDefault("cssClass")?.ToString();
        set => Properties["cssClass"] = value ?? string.Empty;
    }

    // ICompositeContentBlock
    public IReadOnlyList<string>? AllowedChildTypes =>
        [ColumnBlock.BlockType]; // sections can only contain columns
    public bool AllowNestedComposites => false; // columns are terminal composites
    public int MaxChildren => 3; // max 3 columns

    /// Call after setting Layout to ensure correct column count.
    public void InitialiseColumns()
    {
        var count = Layout switch
        {
            SectionLayout.Full       => 1,
            SectionLayout.TwoColumn  => 2,
            SectionLayout.ThreeColumn => 3,
            SectionLayout.Sidebar    => 2,
            _ => 1
        };

        Children.Clear();
        for (var i = 0; i < count; i++)
        {
            Children.Add(new ColumnBlock
            {
                SortOrder = i,
                ColIndex = i
            });
        }
    }
}
```

---

## Task MVP-2.3 — ColumnBlock

FILE: Aero.CMS.Core/Content/Models/Blocks/ColumnBlock.cs

ColumnBlock is the direct parent of leaf blocks. It is a composite
that only allows leaf blocks as children (no further nesting).

```csharp
namespace Aero.CMS.Core.Content.Models.Blocks;

public class ColumnBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "columnBlock";

    public ColumnBlock() { Type = BlockType; }

    public int ColIndex
    {
        get => int.TryParse(
                   Properties.GetValueOrDefault("colIndex")?.ToString(),
                   out var c) ? c : 0;
        set => Properties["colIndex"] = value.ToString();
    }

    // ICompositeContentBlock
    public IReadOnlyList<string>? AllowedChildTypes => null; // all leaf types
    public bool AllowNestedComposites => false;              // no sections inside columns
    public int MaxChildren => -1;
}
```

---

## Task MVP-2.4 — HtmlBlock (new leaf type needed for MVP)

FILE: Aero.CMS.Core/Content/Models/Blocks/HtmlBlock.cs

```csharp
namespace Aero.CMS.Core.Content.Models.Blocks;

public class HtmlBlock : ContentBlock
{
    public static string BlockType => "htmlBlock";
    public HtmlBlock() { Type = BlockType; }

    public string Html
    {
        get => Properties.GetValueOrDefault("html")?.ToString() ?? string.Empty;
        set => Properties["html"] = value;
    }
}
```

---

## Task MVP-2.5 — SectionService

FILE: Aero.CMS.Core/Content/Services/SectionService.cs

Encapsulates section-level mutations on a ContentDocument.
The canvas calls these; they do not persist — the canvas
serialises the whole document on save.

```csharp
namespace Aero.CMS.Core.Content.Services;

public class SectionService
{
    /// Adds a new SectionBlock with initialised columns at the end of Blocks.
    public SectionBlock AddSection(ContentDocument page, SectionLayout layout)
    {
        var section = new SectionBlock
        {
            Layout = layout,
            SortOrder = page.Blocks.Count
        };
        section.InitialiseColumns();
        page.Blocks.Add(section);
        return section;
    }

    /// Removes the section with the given Id from page.Blocks.
    /// Returns false if not found.
    public bool RemoveSection(ContentDocument page, Guid sectionId)
    {
        var section = page.Blocks.FirstOrDefault(b => b.Id == sectionId);
        if (section is null) return false;
        page.Blocks.Remove(section);
        RenumberSortOrder(page.Blocks);
        return true;
    }

    /// Moves a section up or down by swapping SortOrder values.
    public bool MoveSection(ContentDocument page, Guid sectionId, int direction)
    {
        var ordered = page.Blocks.OrderBy(b => b.SortOrder).ToList();
        var idx = ordered.FindIndex(b => b.Id == sectionId);
        if (idx < 0) return false;

        var targetIdx = idx + direction;
        if (targetIdx < 0 || targetIdx >= ordered.Count) return false;

        (ordered[idx].SortOrder, ordered[targetIdx].SortOrder) =
            (ordered[targetIdx].SortOrder, ordered[idx].SortOrder);
        return true;
    }

    /// Adds a leaf block to the specified column of the specified section.
    public ContentBlock AddBlock(
        ContentDocument page,
        Guid sectionId,
        int colIndex,
        ContentBlock block)
    {
        var section = page.Blocks
            .OfType<SectionBlock>()
            .FirstOrDefault(s => s.Id == sectionId)
            ?? throw new InvalidOperationException($"Section {sectionId} not found.");

        var column = section.Children
            .OfType<ColumnBlock>()
            .FirstOrDefault(c => c.ColIndex == colIndex)
            ?? throw new InvalidOperationException($"Column {colIndex} not found.");

        block.SortOrder = column.Children.Count;
        column.Children.Add(block);
        return block;
    }

    /// Removes a block from any column within the section.
    public bool RemoveBlock(ContentDocument page, Guid sectionId, Guid blockId)
    {
        var section = page.Blocks
            .OfType<SectionBlock>()
            .FirstOrDefault(s => s.Id == sectionId);
        if (section is null) return false;

        foreach (var column in section.Children.OfType<ColumnBlock>())
        {
            var block = column.Children.FirstOrDefault(b => b.Id == blockId);
            if (block is null) continue;
            column.Children.Remove(block);
            RenumberSortOrder(column.Children);
            return true;
        }
        return false;
    }

    private static void RenumberSortOrder(List<ContentBlock> blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
            blocks[i].SortOrder = i;
    }
}
```

### TEST: MVP-2.2–2.3 — Unit

FILE: Aero.CMS.Tests.Unit/Content/SectionBlockTests.cs

```
MUST TEST:
- SectionBlock.BlockType == "sectionBlock"
- SectionBlock.AllowedChildTypes contains only "columnBlock"
- SectionBlock.AllowNestedComposites is false
- InitialiseColumns() with Full creates 1 ColumnBlock
- InitialiseColumns() with TwoColumn creates 2 ColumnBlocks
- InitialiseColumns() with ThreeColumn creates 3 ColumnBlocks
- InitialiseColumns() with Sidebar creates 2 ColumnBlocks
- InitialiseColumns() clears existing children before creating
- ColumnBlocks have ColIndex 0, 1, 2... in order
- ColumnBlock.BlockType == "columnBlock"
- ColumnBlock.AllowNestedComposites is false
- ColumnBlock.AllowedChildTypes is null (all leaf types allowed)
- HtmlBlock.BlockType == "htmlBlock"
- HtmlBlock.Html getter/setter round-trips through Properties
```

### TEST: MVP-2.5 — Unit

FILE: Aero.CMS.Tests.Unit/Content/SectionServiceTests.cs

```
MUST TEST:
- AddSection adds SectionBlock to page.Blocks
- AddSection returns the created SectionBlock
- AddSection assigns SortOrder = current Blocks.Count
- AddSection with TwoColumn creates section with 2 ColumnBlock children
- RemoveSection removes the correct section by Id
- RemoveSection returns false for unknown Id
- RemoveSection renumbers remaining sections SortOrder starting from 0
- MoveSection swaps SortOrder of adjacent sections
- MoveSection returns false when moving first section up
- MoveSection returns false when moving last section down
- MoveSection returns false for unknown sectionId
- AddBlock adds leaf block to correct column
- AddBlock throws when sectionId not found
- AddBlock throws when colIndex not found
- AddBlock assigns SortOrder = column.Children.Count before adding
- RemoveBlock removes block from correct column
- RemoveBlock renumbers remaining blocks in column
- RemoveBlock returns false when block not found
- RemoveBlock returns false when section not found
```

PHASE MVP-2 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Section"
  Expected: All pass. Zero failures.

---

# PHASE MVP-3 — Page Repository & Service

Goal: PageService wrapping ContentRepository with page-specific operations.
Prerequisites: MVP-2 green.

---

## Task MVP-3.1 — PageService

FILE: Aero.CMS.Core/Content/Services/PageService.cs

```csharp
namespace Aero.CMS.Core.Content.Services;

public interface IPageService
{
    Task<List<ContentDocument>> GetPagesForSiteAsync(
        Guid siteId, CancellationToken ct = default);

    Task<ContentDocument?> GetBySlugAsync(
        string slug, CancellationToken ct = default);

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

    public async Task<HandlerResult<ContentDocument>> CreatePageAsync(
        Guid siteId, string name, string slug,
        string createdBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return HandlerResult<ContentDocument>.Fail("Page name is required.");

        if (string.IsNullOrWhiteSpace(slug))
            slug = SlugHelper.Generate(name);

        // Ensure slug is unique
        var existing = await contentRepo.GetBySlugAsync(slug, ct);
        if (existing is not null)
            return HandlerResult<ContentDocument>.Fail(
                $"A page with slug '{slug}' already exists.");

        var page = new ContentDocument
        {
            Name = name,
            Slug = slug.StartsWith('/') ? slug : $"/{slug}",
            ContentTypeAlias = "page",
            Status = PublishingStatus.Published, // MVP: all pages immediately live
            PublishedAt = clock.UtcNow,
            CreatedBy = createdBy
        };

        page.Properties["siteId"] = siteId.ToString();
        page.Properties["title"] = name;
        page.Properties["description"] = string.Empty;

        var result = await contentRepo.SaveAsync(page, createdBy, ct);
        return result.Success
            ? HandlerResult<ContentDocument>.Ok(page)
            : HandlerResult<ContentDocument>.Fail(result.Errors);
    }

    public Task<HandlerResult> SavePageAsync(
        ContentDocument page, string savedBy, CancellationToken ct = default)
        => contentRepo.SaveAsync(page, savedBy, ct);

    public async Task<HandlerResult> DeletePageAsync(
        Guid pageId, string deletedBy, CancellationToken ct = default)
    {
        var page = await contentRepo.GetByIdAsync(pageId, ct);
        if (page is null)
            return HandlerResult.Fail("Page not found.");
        return await contentRepo.DeleteAsync(pageId, ct);
    }
}
```

### TEST: MVP-3.1 — Unit

FILE: Aero.CMS.Tests.Unit/Content/PageServiceTests.cs

Use NSubstitute for IContentRepository and ISystemClock.

```
MUST TEST:
- GetPagesForSiteAsync returns only pages matching siteId
- GetPagesForSiteAsync returns empty list when no pages for site
- GetPagesForSiteAsync orders by SortOrder then Name
- CreatePageAsync returns Fail when name is empty
- CreatePageAsync generates slug from name when slug not provided
- CreatePageAsync returns Fail when slug already exists
- CreatePageAsync sets ContentTypeAlias = "page"
- CreatePageAsync sets Status = Published
- CreatePageAsync sets PublishedAt from ISystemClock
- CreatePageAsync prepends '/' to slug if not present
- CreatePageAsync stores siteId in Properties["siteId"]
- CreatePageAsync calls contentRepo.SaveAsync exactly once
- SavePageAsync delegates to contentRepo.SaveAsync
- DeletePageAsync returns Fail when page not found
- DeletePageAsync calls contentRepo.DeleteAsync when page found
```

PHASE MVP-3 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~PageService"
  Expected: All pass. Zero failures.

---

# PHASE MVP-4 — Public Rendering

Goal: Public site renders pages at their slugs. No admin yet.
Prerequisites: MVP-3 green.

---

## Task MVP-4.1 — Register Block Types in IBlockRegistry

In `ServiceExtensions.AddAeroCmsCore`, after existing registrations add:

```csharp
// Register block types with their public view components
// Views are in Aero.CMS.Web — registered at app startup, not here.
// The registry maps aliases to types; Web project provides the types.
// Registration happens in Aero.CMS.Web/Program.cs after AddAeroCmsCore.
```

The Web project's `Program.cs` calls `AddAeroCmsCore` then
registers block views against the singleton `IBlockRegistry`:

```csharp
// Program.cs in Aero.CMS.Web
builder.Services.AddAeroCmsCore(builder.Configuration);

// Register built-in block view components
builder.Services.AddSingleton<IBlockRegistry>(sp =>
{
    var registry = sp.GetRequiredService<IBlockRegistry>();
    registry.Register<RichTextBlock, RichTextBlockView>();
    registry.Register<ImageBlock, ImageBlockView>();
    registry.Register<HeroBlock, HeroBlockView>();
    registry.Register<QuoteBlock, QuoteBlockView>();
    registry.Register<MarkdownBlock, MarkdownBlockView>();
    registry.Register<HtmlBlock, HtmlBlockView>();
    registry.Register<SectionBlock, SectionBlockView>();
    registry.Register<ColumnBlock, ColumnBlockView>();
    return registry;
});
```

AGENT DECISION FORBIDDEN: IBlockRegistry is already a singleton from
Phase 16 DI registration. Do not re-register it. Resolve the existing
singleton and call Register on it in Program.cs.

---

## Task MVP-4.2 — Public Layout

FILE: Aero.CMS.Web/Layouts/PublicLayout.razor

```razor
@inherits LayoutComponentBase

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@(ViewData?["Title"]?.ToString() ?? "Aero CMS")</title>
    <link rel="stylesheet" href="/css/site.css" />
</head>
<body>
    <main>
        @Body
    </main>
</body>
</html>
```

---

## Task MVP-4.3 — Public Page View

FILE: Aero.CMS.Web/Pages/PublicPageView.razor

This component is the `IContentView` implementation for ContentTypeAlias = "page".
It receives a `ContentDocument` and renders its block tree.

```razor
@implements IContentView
@inject MarkdownRendererService MarkdownRenderer

<PageTitle>@Page.Properties.GetValueOrDefault("title")?.ToString()</PageTitle>

<div class="page-content">
    @foreach (var block in Page.Blocks.OrderBy(b => b.SortOrder))
    {
        <DynamicComponent Type="@BlockRegistry.Resolve(block.Type)"
                          Parameters="@BuildParams(block)" />
    }
</div>

@code {
    [Parameter] public ContentDocument Page { get; set; } = default!;
    [Inject] IBlockRegistry BlockRegistry { get; set; } = default!;

    // IContentView contract
    public string ContentTypeAlias => "page";

    private Dictionary<string, object> BuildParams(ContentBlock block) => new()
    {
        ["Block"] = block,
        ["IsEditing"] = false
    };
}
```

---

## Task MVP-4.4 — Block View Components (Public, Read-Only)

All view components live in Aero.CMS.Web/Blocks/.
All implement: `[Parameter] ContentBlock Block` and `[Parameter] bool IsEditing`.
IsEditing is always false on the public site. It is the same parameter
the admin canvas sets to true — same component, dual mode.

FILE: Aero.CMS.Web/Blocks/SectionBlockView.razor

```razor
@{
    var section = (SectionBlock)Block;
    var columns = section.Children
        .OfType<ColumnBlock>()
        .OrderBy(c => c.ColIndex)
        .ToList();

    var gridClass = section.Layout switch
    {
        SectionLayout.Full        => "grid-cols-1",
        SectionLayout.TwoColumn   => "grid-cols-2",
        SectionLayout.ThreeColumn => "grid-cols-3",
        SectionLayout.Sidebar     => "grid-cols-[2fr_1fr]",
        _ => "grid-cols-1"
    };
}

<section class="section @(section.CssClass)"
         style="@(section.BackgroundColor != null ? $"background:{section.BackgroundColor}" : "")">
    <div class="grid @gridClass gap-6 max-w-7xl mx-auto px-4">
        @foreach (var column in columns)
        {
            <DynamicComponent Type="@BlockRegistry.Resolve(column.Type)"
                              Parameters="@ColumnParams(column)" />
        }
    </div>
</section>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Inject] IBlockRegistry BlockRegistry { get; set; } = default!;

    private Dictionary<string, object> ColumnParams(ColumnBlock col) => new()
    {
        ["Block"] = col,
        ["IsEditing"] = IsEditing
    };
}
```

FILE: Aero.CMS.Web/Blocks/ColumnBlockView.razor

```razor
@{
    var column = (ColumnBlock)Block;
}
<div class="column">
    @foreach (var child in column.Children.OrderBy(b => b.SortOrder))
    {
        <DynamicComponent Type="@BlockRegistry.Resolve(child.Type)"
                          Parameters="@BuildParams(child)" />
    }
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Inject] IBlockRegistry BlockRegistry { get; set; } = default!;

    private Dictionary<string, object> BuildParams(ContentBlock b) => new()
    {
        ["Block"] = b,
        ["IsEditing"] = IsEditing
    };
}
```

FILE: Aero.CMS.Web/Blocks/RichTextBlockView.razor

```razor
@{
    var rtb = (RichTextBlock)Block;
}
@if (IsEditing)
{
    <div class="block-edit-field" contenteditable="true"
         @oninput="OnInput"
         @ref="_element">
        @((MarkupString)rtb.Html)
    </div>
}
else
{
    <div class="rich-text">@((MarkupString)rtb.Html)</div>
}

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private ElementReference _element;

    private async Task OnInput()
    {
        var rtb = (RichTextBlock)Block;
        // Read updated HTML from the contenteditable element via JS
        // Simplified for MVP: just notify parent to save
        await OnChanged.InvokeAsync();
    }
}
```

FILE: Aero.CMS.Web/Blocks/HeroBlockView.razor

```razor
@{
    var hero = (HeroBlock)Block;
}
<div class="hero">
    <h1 class="hero-heading">@hero.Heading</h1>
    @if (!string.IsNullOrEmpty(hero.Subtext))
    {
        <p class="hero-subtext">@hero.Subtext</p>
    }
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
}
```

FILE: Aero.CMS.Web/Blocks/QuoteBlockView.razor

```razor
@{
    var quote = (QuoteBlock)Block;
}
<figure class="quote">
    <blockquote>@quote.Quote</blockquote>
    @if (!string.IsNullOrEmpty(quote.Attribution))
    {
        <figcaption>— @quote.Attribution</figcaption>
    }
</figure>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
}
```

FILE: Aero.CMS.Web/Blocks/MarkdownBlockView.razor

```razor
@inject MarkdownRendererService Renderer
@{
    var mb = (MarkdownBlock)Block;
    var html = Renderer.ToHtml(mb.Markdown);
}
<div class="markdown-content">@((MarkupString)html)</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
}
```

FILE: Aero.CMS.Web/Blocks/HtmlBlockView.razor

```razor
@{
    var hb = (HtmlBlock)Block;
}
<div class="html-block">@((MarkupString)hb.Html)</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
}
```

FILE: Aero.CMS.Web/Blocks/ImageBlockView.razor

```razor
@{
    var img = (ImageBlock)Block;
}
@if (img.MediaId.HasValue)
{
    <figure class="image-block">
        <img src="/media/@img.MediaId" alt="@img.Alt" loading="lazy" />
        @if (!string.IsNullOrEmpty(img.Alt))
        {
            <figcaption>@img.Alt</figcaption>
        }
    </figure>
}

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
}
```

---

## Task MVP-4.5 — ContentView Resolution (EntryPage)

FILE: Aero.CMS.Web/Pages/EntryPage.razor

Receives the resolved ContentDocument from `HttpContext.Items["AeroContent"]`
and dispatches to the correct `IContentView` implementation.
For MVP only "page" ContentTypeAlias exists.

```razor
@page "/aero-entry"
@inject IHttpContextAccessor HttpContextAccessor

@if (_content is not null)
{
    <DynamicComponent Type="@ResolveView(_content.ContentTypeAlias)"
                      Parameters="@new Dictionary<string, object> { [""Page""] = _content }" />
}
else
{
    <h1>404 — Page not found</h1>
}

@code {
    private ContentDocument? _content;

    protected override void OnInitialized()
    {
        _content = HttpContextAccessor.HttpContext?
            .Items["AeroContent"] as ContentDocument;
    }

    private static Type ResolveView(string alias) => alias switch
    {
        "page" => typeof(PublicPageView),
        _      => typeof(NotFoundView)
    };
}
```

---

## Task MVP-4.6 — Minimal CSS

FILE: Aero.CMS.Web/wwwroot/css/site.css

```css
/* Aero CMS — MVP baseline styles */
*, *::before, *::after { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, sans-serif; line-height: 1.6; color: #1a1a1a; }
.section { padding: 3rem 0; }
.grid { display: grid; }
.grid-cols-1 { grid-template-columns: 1fr; }
.grid-cols-2 { grid-template-columns: 1fr 1fr; }
.grid-cols-3 { grid-template-columns: 1fr 1fr 1fr; }
.grid-cols-\[2fr_1fr\] { grid-template-columns: 2fr 1fr; }
.gap-6 { gap: 1.5rem; }
.max-w-7xl { max-width: 80rem; }
.mx-auto { margin-left: auto; margin-right: auto; }
.px-4 { padding-left: 1rem; padding-right: 1rem; }
.hero { padding: 4rem 2rem; text-align: center; }
.hero-heading { font-size: 3rem; font-weight: 700; margin: 0 0 1rem; }
.hero-subtext { font-size: 1.25rem; color: #555; }
.rich-text img { max-width: 100%; height: auto; }
.quote { border-left: 4px solid #333; margin: 0; padding: 1rem 2rem; }
blockquote { font-size: 1.25rem; font-style: italic; margin: 0 0 0.5rem; }
figcaption { font-size: 0.875rem; color: #666; }
```

PHASE MVP-4 GATE:
  dotnet build Aero.CMS.sln
  Manual test: start app, request "/" — expect 404 or empty site (no pages yet)
  Expected: 0 build errors.

---

# PHASE MVP-5 — Admin Editor UI

Goal: Admin UI at /admin. Create sites, pages, sections, blocks.
No auth. Full CRUD. Inline block editing. Runnable pages.
Prerequisites: MVP-4 green.

This is the largest phase. It is broken into sub-tasks.

---

## Task MVP-5.1 — Admin Route & Layout

FILE: Aero.CMS.Components/Admin/Layout/AdminLayout.razor

```razor
@inherits LayoutComponentBase

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Aero CMS Admin</title>
    <link rel="stylesheet" href="/css/admin.css" />
</head>
<body class="admin-shell">
    <nav class="admin-nav">
        <a href="/admin" class="admin-logo">⬡ Aero CMS</a>
        <div class="admin-nav-links">
            <a href="/admin">Pages</a>
            <a href="/admin/site">Site Settings</a>
        </div>
    </nav>
    <main class="admin-main">
        @Body
    </main>
</body>
</html>
```

FILE: Aero.CMS.Components/Admin/Layout/AdminNavBar.razor

Simple top bar rendered inside AdminLayout. Shows:
- Aero CMS logo/link back to /admin
- "View Site" link to "/"
- Current page name (passed as parameter)

```razor
<nav class="admin-topbar">
    <span class="page-name">@PageName</span>
    <a href="/" target="_blank" class="btn-secondary">View Site ↗</a>
</nav>

@code {
    [Parameter] public string PageName { get; set; } = string.Empty;
}
```

FILE: Aero.CMS.Web/wwwroot/css/admin.css

```css
/* Aero CMS — Admin styles */
.admin-shell { display: flex; flex-direction: column; min-height: 100vh;
               font-family: system-ui, sans-serif; background: #f5f5f5; }
.admin-nav { display: flex; align-items: center; gap: 2rem;
             padding: 0 1.5rem; height: 56px; background: #18181b;
             color: #fff; }
.admin-logo { color: #fff; font-weight: 700; font-size: 1.1rem;
              text-decoration: none; }
.admin-nav-links a { color: #a1a1aa; text-decoration: none; font-size: 0.9rem; }
.admin-nav-links a:hover { color: #fff; }
.admin-main { flex: 1; padding: 2rem; }
.admin-topbar { display: flex; justify-content: space-between; align-items: center;
                margin-bottom: 1.5rem; }
.btn { padding: 0.5rem 1rem; border-radius: 6px; border: none;
       cursor: pointer; font-size: 0.875rem; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-primary:hover { background: #1d4ed8; }
.btn-secondary { background: #e4e4e7; color: #18181b; text-decoration: none; }
.btn-secondary:hover { background: #d4d4d8; }
.btn-danger { background: #ef4444; color: #fff; }
.btn-ghost { background: transparent; border: 1px solid #d4d4d8; color: #18181b; }
.card { background: #fff; border-radius: 8px; padding: 1.5rem;
        box-shadow: 0 1px 3px rgba(0,0,0,0.1); margin-bottom: 1rem; }
.page-list-item { display: flex; justify-content: space-between; align-items: center;
                  padding: 1rem; background: #fff; border-radius: 8px;
                  margin-bottom: 0.5rem; box-shadow: 0 1px 2px rgba(0,0,0,0.05); }
.page-list-item .page-name { font-weight: 500; }
.page-list-item .page-slug { font-size: 0.8rem; color: #71717a; }
.page-list-item .actions { display: flex; gap: 0.5rem; }

/* Canvas */
.canvas { max-width: 900px; margin: 0 auto; }
.section-block { border: 2px dashed transparent; border-radius: 8px;
                 margin-bottom: 1rem; position: relative; }
.section-block:hover { border-color: #2563eb33; }
.section-block.is-active { border-color: #2563eb; }
.section-toolbar { display: flex; gap: 0.5rem; padding: 0.5rem;
                   background: #2563eb; border-radius: 4px 4px 0 0;
                   position: absolute; top: -38px; left: -2px; }
.section-toolbar button { background: transparent; border: none; color: #fff;
                           cursor: pointer; font-size: 0.8rem; }
.section-grid { display: grid; gap: 1rem; min-height: 80px; padding: 0.75rem; }
.section-grid.cols-1 { grid-template-columns: 1fr; }
.section-grid.cols-2 { grid-template-columns: 1fr 1fr; }
.section-grid.cols-3 { grid-template-columns: 1fr 1fr 1fr; }
.section-grid.cols-sidebar { grid-template-columns: 2fr 1fr; }
.column-drop-target { min-height: 60px; border: 2px dashed #d4d4d8;
                      border-radius: 6px; background: #fafafa; padding: 0.5rem; }
.block-item { background: #fff; border-radius: 6px; padding: 0.75rem;
              margin-bottom: 0.5rem; border: 1px solid #e4e4e7;
              cursor: pointer; position: relative; }
.block-item:hover { border-color: #2563eb; }
.block-item.is-editing { border-color: #2563eb; box-shadow: 0 0 0 3px #2563eb22; }
.block-type-badge { font-size: 0.7rem; color: #71717a; margin-bottom: 0.25rem; }
.block-toolbar { display: flex; gap: 0.25rem; position: absolute;
                 top: 0.5rem; right: 0.5rem; }
.drop-zone { height: 8px; border-radius: 4px; transition: all 0.2s; }
.drop-zone.active { height: 32px; background: #dbeafe; border: 2px dashed #2563eb; }
.add-section-btn { display: flex; align-items: center; justify-content: center;
                   gap: 0.5rem; padding: 0.75rem; border: 2px dashed #d4d4d8;
                   border-radius: 8px; background: transparent; width: 100%;
                   cursor: pointer; color: #71717a; font-size: 0.875rem;
                   margin-top: 0.5rem; }
.add-section-btn:hover { border-color: #2563eb; color: #2563eb; background: #eff6ff; }
.add-block-btn { display: flex; align-items: center; justify-content: center;
                 padding: 0.5rem; border: 1px dashed #d4d4d8; border-radius: 4px;
                 background: transparent; width: 100%; cursor: pointer;
                 color: #71717a; font-size: 0.8rem; margin-top: 0.25rem; }
.add-block-btn:hover { border-color: #2563eb; color: #2563eb; }
.block-edit-panel { padding: 0.75rem 0; }
.block-edit-panel label { display: block; font-size: 0.8rem; color: #71717a;
                           margin-bottom: 0.25rem; }
.block-edit-panel input, .block-edit-panel textarea {
    width: 100%; padding: 0.5rem; border: 1px solid #d4d4d8;
    border-radius: 4px; font-size: 0.875rem; font-family: inherit; }
.block-edit-panel textarea { min-height: 120px; resize: vertical; }
.modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.5);
                  display: flex; align-items: center; justify-content: center;
                  z-index: 100; }
.modal { background: #fff; border-radius: 12px; padding: 2rem;
         min-width: 400px; max-width: 560px; }
.modal h2 { margin: 0 0 1.5rem; font-size: 1.25rem; }
.modal-actions { display: flex; gap: 0.75rem; justify-content: flex-end;
                 margin-top: 1.5rem; }
.form-field { margin-bottom: 1rem; }
.form-field label { display: block; font-size: 0.875rem; font-weight: 500;
                    margin-bottom: 0.375rem; }
.form-field input, .form-field textarea, .form-field select {
    width: 100%; padding: 0.5rem 0.75rem; border: 1px solid #d4d4d8;
    border-radius: 6px; font-size: 0.875rem; }
.error-msg { color: #ef4444; font-size: 0.8rem; margin-top: 0.25rem; }
```

---

## Task MVP-5.2 — Page List (Admin Index)

FILE: Aero.CMS.Components/Admin/PageSection/PageList.razor

Route: `/admin`

Displays all pages for the default site.
Actions per page: Edit (goes to page editor), View (opens slug in new tab), Delete.
Top action: New Page button → opens NewPageDialog.

```razor
@page "/admin"
@inject IPageService PageService
@inject ISiteRepository SiteRepo
@inject NavigationManager Nav

<AdminNavBar PageName="Pages" />

<div class="page-header" style="display:flex;justify-content:space-between;
     align-items:center;margin-bottom:1.5rem;">
    <h1 style="margin:0;font-size:1.5rem;">Pages</h1>
    <button class="btn btn-primary" @onclick="OpenNewPageDialog">+ New Page</button>
</div>

@if (_pages.Count == 0)
{
    <div class="card" style="text-align:center;color:#71717a;padding:3rem;">
        No pages yet. Create your first page to get started.
    </div>
}
else
{
    @foreach (var page in _pages)
    {
        <div class="page-list-item">
            <div>
                <div class="page-name">@page.Name</div>
                <div class="page-slug">@page.Slug</div>
            </div>
            <div class="actions">
                <a href="@page.Slug" target="_blank" class="btn btn-ghost">View</a>
                <button class="btn btn-secondary"
                        @onclick="() => EditPage(page.Id)">Edit</button>
                <button class="btn btn-danger"
                        @onclick="() => OpenDeleteDialog(page)">Delete</button>
            </div>
        </div>
    }
}

@if (_showNewPage)
{
    <NewPageDialog SiteId="_siteId"
                   OnSaved="HandlePageCreated"
                   OnCancelled="() => _showNewPage = false" />
}

@if (_deletingPage is not null)
{
    <DeletePageDialog Page="_deletingPage"
                      OnConfirmed="HandlePageDeleted"
                      OnCancelled="() => _deletingPage = null" />
}

@code {
    private List<ContentDocument> _pages = [];
    private Guid _siteId;
    private bool _showNewPage;
    private ContentDocument? _deletingPage;

    protected override async Task OnInitializedAsync()
    {
        var site = await SiteRepo.GetDefaultAsync();
        if (site is null) return;
        _siteId = site.Id;
        _pages = await PageService.GetPagesForSiteAsync(_siteId);
    }

    private void OpenNewPageDialog() => _showNewPage = true;
    private void OpenDeleteDialog(ContentDocument page) => _deletingPage = page;
    private void EditPage(Guid id) => Nav.NavigateTo($"/admin/page/{id}");

    private async Task HandlePageCreated()
    {
        _showNewPage = false;
        _pages = await PageService.GetPagesForSiteAsync(_siteId);
    }

    private async Task HandlePageDeleted()
    {
        _deletingPage = null;
        _pages = await PageService.GetPagesForSiteAsync(_siteId);
    }
}
```

---

## Task MVP-5.3 — New Page Dialog

FILE: Aero.CMS.Components/Admin/PageSection/NewPageDialog.razor

```razor
@inject IPageService PageService

<div class="modal-backdrop">
    <div class="modal">
        <h2>New Page</h2>

        <div class="form-field">
            <label>Page Name *</label>
            <input @bind="_name" @bind:event="oninput"
                   @oninput="GenerateSlug" placeholder="e.g. About Us" />
        </div>
        <div class="form-field">
            <label>Slug</label>
            <input @bind="_slug" placeholder="/about-us" />
        </div>
        <div class="form-field">
            <label>Description</label>
            <textarea @bind="_description" rows="2"
                      placeholder="Brief page description"></textarea>
        </div>

        @if (!string.IsNullOrEmpty(_error))
        {
            <p class="error-msg">@_error</p>
        }

        <div class="modal-actions">
            <button class="btn btn-ghost" @onclick="OnCancelled">Cancel</button>
            <button class="btn btn-primary" @onclick="Save"
                    disabled="@_saving">
                @(_saving ? "Creating..." : "Create Page")
            </button>
        </div>
    </div>
</div>

@code {
    [Parameter] public Guid SiteId { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private string _name = string.Empty;
    private string _slug = string.Empty;
    private string _description = string.Empty;
    private string _error = string.Empty;
    private bool _saving;

    private void GenerateSlug()
        => _slug = "/" + SlugHelper.Generate(_name);

    private async Task Save()
    {
        _error = string.Empty;
        if (string.IsNullOrWhiteSpace(_name))
        {
            _error = "Page name is required.";
            return;
        }

        _saving = true;
        var result = await PageService.CreatePageAsync(
            SiteId, _name, _slug, "admin");

        _saving = false;
        if (!result.Success)
        {
            _error = string.Join(", ", result.Errors);
            return;
        }

        await OnSaved.InvokeAsync();
    }
}
```

---

## Task MVP-5.4 — Delete Page Dialog

FILE: Aero.CMS.Components/Admin/PageSection/DeletePageDialog.razor

```razor
@inject IPageService PageService

<div class="modal-backdrop">
    <div class="modal">
        <h2>Delete Page</h2>
        <p>Are you sure you want to delete <strong>@Page.Name</strong>?
           This cannot be undone.</p>
        <div class="modal-actions">
            <button class="btn btn-ghost" @onclick="OnCancelled">Cancel</button>
            <button class="btn btn-danger" @onclick="Confirm"
                    disabled="@_deleting">
                @(_deleting ? "Deleting..." : "Delete Page")
            </button>
        </div>
    </div>
</div>

@code {
    [Parameter] public ContentDocument Page { get; set; } = default!;
    [Parameter] public EventCallback OnConfirmed { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private bool _deleting;

    private async Task Confirm()
    {
        _deleting = true;
        await PageService.DeletePageAsync(Page.Id, "admin");
        await OnConfirmed.InvokeAsync();
    }
}
```

---

## Task MVP-5.5 — BlockEditContext

FILE: Aero.CMS.Components/Admin/BlockCanvas/BlockEditContext.cs

```csharp
namespace Aero.CMS.Components.Admin.BlockCanvas;

public class BlockEditContext
{
    public Guid? ActiveBlockId { get; private set; }
    public Guid? ActiveSectionId { get; private set; }
    public event Action? OnChanged;

    public void SetActiveBlock(Guid blockId, Guid sectionId)
    {
        ActiveBlockId = blockId;
        ActiveSectionId = sectionId;
        OnChanged?.Invoke();
    }

    public void ClearBlock()
    {
        ActiveBlockId = null;
        OnChanged?.Invoke();
    }

    public void ClearAll()
    {
        ActiveBlockId = null;
        ActiveSectionId = null;
        OnChanged?.Invoke();
    }

    public bool IsBlockActive(Guid blockId) => ActiveBlockId == blockId;
    public bool IsSectionActive(Guid sectionId) => ActiveSectionId == sectionId;
}
```

---

## Task MVP-5.6 — AddBlockButton

FILE: Aero.CMS.Components/Admin/BlockCanvas/AddBlockButton.razor

Shows a menu of available block types. On selection calls OnAdd
with the chosen block type alias.

```razor
<div style="position:relative;">
    <button class="add-block-btn" @onclick="Toggle">+ Add Block</button>

    @if (_open)
    {
        <div style="position:absolute;top:100%;left:0;z-index:50;background:#fff;
                    border:1px solid #e4e4e7;border-radius:8px;padding:0.5rem;
                    min-width:180px;box-shadow:0 4px 12px rgba(0,0,0,0.1);">
            @foreach (var type in BlockTypes)
            {
                <button style="display:block;width:100%;text-align:left;padding:0.5rem 0.75rem;
                               background:transparent;border:none;cursor:pointer;
                               font-size:0.875rem;border-radius:4px;"
                        @onclick="() => Select(type.Alias)">
                    @type.Label
                </button>
            }
        </div>
    }
</div>

@code {
    [Parameter] public EventCallback<string> OnAdd { get; set; }

    private bool _open;
    private void Toggle() => _open = !_open;

    private async Task Select(string alias)
    {
        _open = false;
        await OnAdd.InvokeAsync(alias);
    }

    private record BlockTypeOption(string Alias, string Label);

    private static readonly List<BlockTypeOption> BlockTypes =
    [
        new("richTextBlock", "Rich Text"),
        new("heroBlock",     "Hero"),
        new("imageBlock",    "Image"),
        new("quoteBlock",    "Quote"),
        new("markdownBlock", "Markdown"),
        new("htmlBlock",     "HTML")
    ];
}
```

---

## Task MVP-5.7 — AddSectionButton

FILE: Aero.CMS.Components/Admin/BlockCanvas/AddSectionButton.razor

```razor
@if (!_picking)
{
    <button class="add-section-btn" @onclick="() => _picking = true">
        + Add Section
    </button>
}
else
{
    <div class="card" style="margin-top:0.5rem;">
        <p style="margin:0 0 0.75rem;font-weight:500;">Choose a layout:</p>
        <div style="display:grid;grid-template-columns:repeat(4,1fr);gap:0.5rem;">
            @foreach (var layout in Layouts)
            {
                <button class="btn btn-ghost" style="padding:0.75rem;font-size:0.8rem;"
                        @onclick="() => Select(layout.Value)">
                    @layout.Label
                </button>
            }
        </div>
        <button class="btn btn-ghost" style="margin-top:0.75rem;"
                @onclick="() => _picking = false">Cancel</button>
    </div>
}

@code {
    [Parameter] public EventCallback<SectionLayout> OnAdd { get; set; }
    private bool _picking;

    private async Task Select(SectionLayout layout)
    {
        _picking = false;
        await OnAdd.InvokeAsync(layout);
    }

    private record LayoutOption(string Label, SectionLayout Value);
    private static readonly List<LayoutOption> Layouts =
    [
        new("Full Width",   SectionLayout.Full),
        new("Two Column",   SectionLayout.TwoColumn),
        new("Three Column", SectionLayout.ThreeColumn),
        new("Sidebar",      SectionLayout.Sidebar)
    ];
}
```

---

## Task MVP-5.8 — Block Inline Editors

Each editor renders in-place when `IsEditing = true` inside a block item.
These are NOT separate dialogs — they render inline below the block preview.

FILE: Aero.CMS.Components/Admin/BlockEditors/HeroBlockEditor.razor

```razor
@{
    var hero = (HeroBlock)Block;
}
<div class="block-edit-panel">
    <div class="form-field">
        <label>Heading</label>
        <input value="@hero.Heading"
               @oninput="e => { hero.Heading = e.Value?.ToString() ?? ""; NotifyChanged(); }" />
    </div>
    <div class="form-field">
        <label>Subtext</label>
        <input value="@hero.Subtext"
               @oninput="e => { hero.Subtext = e.Value?.ToString() ?? ""; NotifyChanged(); }" />
    </div>
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    private void NotifyChanged() => OnChanged.InvokeAsync();
}
```

FILE: Aero.CMS.Components/Admin/BlockEditors/RichTextBlockEditor.razor

For MVP: simple textarea. IRichTextEditor swap-in is wired here later.

```razor
@{
    var rtb = (RichTextBlock)Block;
}
<div class="block-edit-panel">
    <div class="form-field">
        <label>Content (HTML)</label>
        <textarea value="@rtb.Html"
                  @oninput="e => { rtb.Html = e.Value?.ToString() ?? ""; NotifyChanged(); }"
                  rows="6"></textarea>
    </div>
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    private void NotifyChanged() => OnChanged.InvokeAsync();
}
```

FILE: Aero.CMS.Components/Admin/BlockEditors/QuoteBlockEditor.razor

```razor
@{
    var q = (QuoteBlock)Block;
}
<div class="block-edit-panel">
    <div class="form-field">
        <label>Quote</label>
        <textarea value="@q.Quote"
                  @oninput="e => { q.Quote = e.Value?.ToString() ?? ""; NotifyChanged(); }"
                  rows="3"></textarea>
    </div>
    <div class="form-field">
        <label>Attribution</label>
        <input value="@q.Attribution"
               @oninput="e => { q.Attribution = e.Value?.ToString() ?? ""; NotifyChanged(); }" />
    </div>
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    private void NotifyChanged() => OnChanged.InvokeAsync();
}
```

FILE: Aero.CMS.Components/Admin/BlockEditors/MarkdownBlockEditor.razor

```razor
@{
    var mb = (MarkdownBlock)Block;
}
<div class="block-edit-panel">
    <div class="form-field">
        <label>Markdown</label>
        <textarea value="@mb.Markdown"
                  @oninput="e => { mb.Markdown = e.Value?.ToString() ?? ""; NotifyChanged(); }"
                  rows="8" style="font-family:monospace;"></textarea>
    </div>
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    private void NotifyChanged() => OnChanged.InvokeAsync();
}
```

FILE: Aero.CMS.Components/Admin/BlockEditors/HtmlBlockEditor.razor

```razor
@{
    var hb = (HtmlBlock)Block;
}
<div class="block-edit-panel">
    <div class="form-field">
        <label>Raw HTML</label>
        <textarea value="@hb.Html"
                  @oninput="e => { hb.Html = e.Value?.ToString() ?? ""; NotifyChanged(); }"
                  rows="8" style="font-family:monospace;"></textarea>
    </div>
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    private void NotifyChanged() => OnChanged.InvokeAsync();
}
```

FILE: Aero.CMS.Components/Admin/BlockEditors/ImageBlockEditor.razor

```razor
@{
    var img = (ImageBlock)Block;
}
<div class="block-edit-panel">
    <div class="form-field">
        <label>Image URL (temporary — media picker in later phase)</label>
        <input value="@img.Alt"
               placeholder="Alt text"
               @oninput="e => { img.Alt = e.Value?.ToString() ?? ""; NotifyChanged(); }" />
    </div>
    <p style="font-size:0.75rem;color:#71717a;">
        Full media picker available after media phase. For MVP use HtmlBlock for images.
    </p>
</div>

@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
    private void NotifyChanged() => OnChanged.InvokeAsync();
}
```

---

## Task MVP-5.9 — BlockWrapper (Admin Canvas Item)

FILE: Aero.CMS.Components/Admin/BlockCanvas/BlockWrapper.razor

Renders one block within a column. Handles:
- Click to activate edit mode
- Inline editor below preview when active
- Delete button
- Move up/down buttons

```razor
@inject IBlockRegistry BlockRegistry

<div class="block-item @(EditContext.IsBlockActive(Block.Id) ? "is-editing" : "")"
     @onclick="Activate" @onclick:stopPropagation="true">

    <div class="block-type-badge">@Block.Type</div>

    @* Preview — always shown *@
    <div class="block-preview">
        <DynamicComponent Type="@BlockRegistry.Resolve(Block.Type)"
                          Parameters="@PreviewParams()" />
    </div>

    @* Inline editor — shown when active *@
    @if (EditContext.IsBlockActive(Block.Id))
    {
        <div @onclick:stopPropagation="true">
            <DynamicComponent Type="@ResolveEditor(Block.Type)"
                              Parameters="@EditorParams()" />
        </div>
    }

    @* Toolbar — shown when active *@
    @if (EditContext.IsBlockActive(Block.Id))
    {
        <div class="block-toolbar" @onclick:stopPropagation="true">
            <button class="btn btn-ghost" style="padding:2px 6px;font-size:0.75rem;"
                    @onclick="MoveUp" title="Move up">↑</button>
            <button class="btn btn-ghost" style="padding:2px 6px;font-size:0.75rem;"
                    @onclick="MoveDown" title="Move down">↓</button>
            <button class="btn btn-danger" style="padding:2px 6px;font-size:0.75rem;"
                    @onclick="Delete" title="Delete block">✕</button>
        </div>
    }
</div>

@code {
    [CascadingParameter] BlockEditContext EditContext { get; set; } = default!;
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public Guid SectionId { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }
    [Parameter] public EventCallback<Guid> OnDelete { get; set; }
    [Parameter] public EventCallback<int> OnMove { get; set; }

    private void Activate() => EditContext.SetActiveBlock(Block.Id, SectionId);

    private async Task Delete()
    {
        EditContext.ClearBlock();
        await OnDelete.InvokeAsync(Block.Id);
    }

    private Task MoveUp() => OnMove.InvokeAsync(-1);
    private Task MoveDown() => OnMove.InvokeAsync(1);

    private Dictionary<string, object> PreviewParams() => new()
    {
        ["Block"] = Block,
        ["IsEditing"] = false
    };

    private Dictionary<string, object> EditorParams() => new()
    {
        ["Block"] = Block,
        ["OnChanged"] = EventCallback.Factory.Create(this, OnChanged)
    };

    private static Type ResolveEditor(string alias) => alias switch
    {
        "richTextBlock" => typeof(RichTextBlockEditor),
        "heroBlock"     => typeof(HeroBlockEditor),
        "quoteBlock"    => typeof(QuoteBlockEditor),
        "markdownBlock" => typeof(MarkdownBlockEditor),
        "htmlBlock"     => typeof(HtmlBlockEditor),
        "imageBlock"    => typeof(ImageBlockEditor),
        _ => typeof(FallbackBlockEditor)
    };
}
```

---

## Task MVP-5.10 — FallbackBlockEditor

FILE: Aero.CMS.Components/Admin/BlockEditors/FallbackBlockEditor.razor

```razor
<div class="block-edit-panel">
    <p style="color:#71717a;font-size:0.875rem;">
        No editor available for block type: <code>@Block.Type</code>
    </p>
</div>
@code {
    [Parameter] public ContentBlock Block { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }
}
```

---

## Task MVP-5.11 — BlockCanvas (Admin, with SortableList)

FILE: Aero.CMS.Components/Admin/BlockCanvas/BlockCanvas.razor

The root canvas for a page. Renders ordered SectionBlocks.
Supports add section, reorder sections, add blocks per column,
reorder blocks within columns. All mutations go through SectionService.
After each mutation, fires OnChanged so PageEditor saves.

```razor
@inject SectionService SectionSvc
@inject IBlockRegistry BlockRegistry

<CascadingValue Value="_editContext">
<div class="canvas" @onclick="() => _editContext.ClearAll()">

    @{
        var sections = Page.Blocks
            .OfType<SectionBlock>()
            .OrderBy(s => s.SortOrder)
            .ToList();
    }

    @foreach (var section in sections)
    {
        var gridClass = section.Layout switch
        {
            SectionLayout.Full        => "cols-1",
            SectionLayout.TwoColumn   => "cols-2",
            SectionLayout.ThreeColumn => "cols-3",
            SectionLayout.Sidebar     => "cols-sidebar",
            _ => "cols-1"
        };

        <div class="section-block @(_editContext.IsSectionActive(section.Id) ? "is-active" : "")"
             @onclick:stopPropagation="true">

            @* Section toolbar *@
            <div class="section-toolbar">
                <button @onclick="() => MoveSection(section.Id, -1)" title="Move up">↑</button>
                <button @onclick="() => MoveSection(section.Id, 1)"  title="Move down">↓</button>
                <button @onclick="() => DeleteSection(section.Id)"   title="Delete section">
                    ✕ Section
                </button>
            </div>

            <div class="section-grid @gridClass">
                @{
                    var columns = section.Children
                        .OfType<ColumnBlock>()
                        .OrderBy(c => c.ColIndex)
                        .ToList();
                }
                @foreach (var column in columns)
                {
                    <div class="column-drop-target">
                        @{
                            var blocks = column.Children
                                .OrderBy(b => b.SortOrder)
                                .ToList();
                        }
                        @foreach (var block in blocks)
                        {
                            <BlockWrapper Block="block"
                                          SectionId="section.Id"
                                          OnChanged="SavePage"
                                          OnDelete="id => DeleteBlock(section.Id, id)"
                                          OnMove="dir => MoveBlock(column, block, dir)" />
                        }

                        <AddBlockButton OnAdd="alias => AddBlock(section.Id, column.ColIndex, alias)" />
                    </div>
                }
            </div>
        </div>
    }

    <AddSectionButton OnAdd="AddSection" />
</div>
</CascadingValue>

@code {
    [Parameter] public ContentDocument Page { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private readonly BlockEditContext _editContext = new();

    private async Task AddSection(SectionLayout layout)
    {
        SectionSvc.AddSection(Page, layout);
        await SavePage();
    }

    private async Task DeleteSection(Guid sectionId)
    {
        _editContext.ClearAll();
        SectionSvc.RemoveSection(Page, sectionId);
        await SavePage();
    }

    private async Task MoveSection(Guid sectionId, int direction)
    {
        SectionSvc.MoveSection(Page, sectionId, direction);
        await SavePage();
    }

    private async Task AddBlock(Guid sectionId, int colIndex, string blockAlias)
    {
        var block = CreateBlock(blockAlias);
        SectionSvc.AddBlock(Page, sectionId, colIndex, block);
        _editContext.SetActiveBlock(block.Id, sectionId);
        await SavePage();
    }

    private async Task DeleteBlock(Guid sectionId, Guid blockId)
    {
        SectionSvc.RemoveBlock(Page, sectionId, blockId);
        await SavePage();
    }

    private async Task MoveBlock(ColumnBlock column, ContentBlock block, int direction)
    {
        var ordered = column.Children.OrderBy(b => b.SortOrder).ToList();
        var idx = ordered.IndexOf(block);
        var targetIdx = idx + direction;
        if (targetIdx < 0 || targetIdx >= ordered.Count) return;
        (ordered[idx].SortOrder, ordered[targetIdx].SortOrder) =
            (ordered[targetIdx].SortOrder, ordered[idx].SortOrder);
        await SavePage();
    }

    private Task SavePage() => OnChanged.InvokeAsync();

    private static ContentBlock CreateBlock(string alias) => alias switch
    {
        "richTextBlock" => new RichTextBlock(),
        "heroBlock"     => new HeroBlock(),
        "quoteBlock"    => new QuoteBlock(),
        "markdownBlock" => new MarkdownBlock(),
        "htmlBlock"     => new HtmlBlock(),
        "imageBlock"    => new ImageBlock(),
        _ => throw new InvalidOperationException($"Unknown block type: {alias}")
    };
}
```

---

## Task MVP-5.12 — PageEditor

FILE: Aero.CMS.Components/Admin/PageSection/PageEditor.razor

Route: `/admin/page/{PageId:guid}`

The full page editing experience.
Header: page name, slug, save button, "View Page" link.
Body: BlockCanvas.
Autosave on every block change with 800ms debounce.

```razor
@page "/admin/page/{PageId:guid}"
@inject IPageService PageService
@inject NavigationManager Nav
@implements IAsyncDisposable

<AdminNavBar PageName="@(_page?.Name ?? "Loading...")" />

@if (_page is null)
{
    <p>Loading...</p>
}
else
{
    <div style="display:flex;justify-content:space-between;align-items:center;
                margin-bottom:1.5rem;">
        <div>
            <h1 style="margin:0;font-size:1.25rem;">@_page.Name</h1>
            <div style="font-size:0.8rem;color:#71717a;">
                @_page.Slug
                @if (_saving) { <span> · Saving...</span> }
                else if (_saved) { <span style="color:#16a34a;"> · Saved</span> }
            </div>
        </div>
        <div style="display:flex;gap:0.75rem;align-items:center;">
            <a href="@_page.Slug" target="_blank" class="btn btn-ghost">View Page ↗</a>
            <button class="btn btn-primary" @onclick="SaveNow">Save</button>
            <button class="btn btn-ghost" @onclick="() => Nav.NavigateTo("/admin")">
                ← Back
            </button>
        </div>
    </div>

    @* Page metadata *@
    <div class="card" style="margin-bottom:1rem;">
        <div style="display:flex;gap:1rem;">
            <div class="form-field" style="flex:1;">
                <label>Page Title</label>
                <input value="@_page.Properties.GetValueOrDefault("title")?.ToString()"
                       @oninput="e => { _page.Properties["title"] = e.Value ?? ""; ScheduleSave(); }" />
            </div>
            <div class="form-field" style="flex:2;">
                <label>Meta Description</label>
                <input value="@_page.Properties.GetValueOrDefault("description")?.ToString()"
                       @oninput="e => { _page.Properties["description"] = e.Value ?? ""; ScheduleSave(); }" />
            </div>
        </div>
    </div>

    <BlockCanvas Page="_page" OnChanged="ScheduleSave" />
}

@code {
    [Parameter] public Guid PageId { get; set; }

    private ContentDocument? _page;
    private bool _saving;
    private bool _saved;
    private CancellationTokenSource? _debounce;

    protected override async Task OnInitializedAsync()
    {
        _page = await PageService.GetPagesForSiteAsync(Guid.Empty) // workaround: load by id
            .ContinueWith(_ => PageService.GetBySlugAsync(string.Empty));

        // Direct load by id via content repo
        // AGENT: inject IContentRepository and call GetByIdAsync(PageId)
        // PageService does not have GetByIdAsync — add it or inject the repo directly
    }

    private void ScheduleSave()
    {
        _saved = false;
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        Task.Delay(800, token).ContinueWith(async t =>
        {
            if (!t.IsCanceled) await SaveNow();
        }, TaskScheduler.Current);
    }

    private async Task SaveNow()
    {
        if (_page is null) return;
        _saving = true;
        _saved = false;
        StateHasChanged();
        await PageService.SavePageAsync(_page, "admin");
        _saving = false;
        _saved = true;
        StateHasChanged();
        // Clear saved indicator after 2 seconds
        await Task.Delay(2000);
        _saved = false;
        StateHasChanged();
    }

    public ValueTask DisposeAsync()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        return ValueTask.CompletedTask;
    }
}
```

AGENT NOTE: `PageEditor.OnInitializedAsync` needs a direct content lookup by Id.
Add the following method to `IPageService` and `PageService`:

```csharp
// Add to IPageService:
Task<ContentDocument?> GetByIdAsync(Guid pageId, CancellationToken ct = default);

// Add to PageService:
public Task<ContentDocument?> GetByIdAsync(Guid pageId, CancellationToken ct = default)
    => contentRepo.GetByIdAsync(pageId, ct);
```

Then in `OnInitializedAsync`:
```csharp
_page = await PageService.GetByIdAsync(PageId);
if (_page is null) Nav.NavigateTo("/admin");
```

---

## Task MVP-5.13 — Site Settings Editor

FILE: Aero.CMS.Components/Admin/SiteSection/SiteEditor.razor

Route: `/admin/site`

```razor
@page "/admin/site"
@inject ISiteRepository SiteRepo

<AdminNavBar PageName="Site Settings" />

<h1 style="font-size:1.25rem;margin-bottom:1.5rem;">Site Settings</h1>

@if (_site is null)
{
    <p>Loading...</p>
}
else
{
    <div class="card">
        <div class="form-field">
            <label>Site Name</label>
            <input @bind="_site.Name" />
        </div>
        <div class="form-field">
            <label>Base URL</label>
            <input @bind="_site.BaseUrl" />
        </div>
        <div class="form-field">
            <label>Description</label>
            <textarea @bind="_site.Description" rows="3"></textarea>
        </div>
        <div class="form-field">
            <label>Footer Text</label>
            <input @bind="_site.FooterText" />
        </div>

        @if (_saved) { <p style="color:#16a34a;">Settings saved.</p> }
        @if (!string.IsNullOrEmpty(_error)) { <p class="error-msg">@_error</p> }

        <button class="btn btn-primary" @onclick="Save"
                disabled="@_saving">
            @(_saving ? "Saving..." : "Save Settings")
        </button>
    </div>
}

@code {
    private SiteDocument? _site;
    private bool _saving;
    private bool _saved;
    private string _error = string.Empty;

    protected override async Task OnInitializedAsync()
        => _site = await SiteRepo.GetDefaultAsync();

    private async Task Save()
    {
        if (_site is null) return;
        _saving = true;
        _error = string.Empty;
        var result = await SiteRepo.SaveAsync(_site, "admin");
        _saving = false;
        if (result.Success) { _saved = true; await Task.Delay(2000); _saved = false; }
        else _error = string.Join(", ", result.Errors);
        StateHasChanged();
    }
}
```

---

## Task MVP-5.14 — Register Admin Routes in Web Project

FILE: Aero.CMS.Web/Program.cs (additions)

```csharp
// After builder.Build():
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .AddAdditionalAssemblies(typeof(Aero.CMS.Components.Admin.PageSection.PageList).Assembly);

// Register SectionService and SiteBootstrapService
builder.Services.AddScoped<SectionService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddHostedService<SiteBootstrapService>();
builder.Services.AddHttpContextAccessor();
```

---

# PHASE MVP-6 — Wiring & End-to-End Verification

Goal: Everything connected. App starts, admin works, pages render publicly.
Prerequisites: MVP-5 green.

---

## Task MVP-6.1 — App.razor Router

FILE: Aero.CMS.Web/Components/App.razor

Ensure the Router covers both public and admin routes, using
`AdminLayout` for /admin/* and `PublicLayout` for everything else.

```razor
<Router AppAssembly="typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(PageList).Assembly }">
    <Found Context="routeData">
        @if (routeData.PageType.Namespace?.Contains("Admin") == true)
        {
            <RouteView RouteData="routeData" DefaultLayout="typeof(AdminLayout)" />
        }
        else
        {
            <RouteView RouteData="routeData" DefaultLayout="typeof(PublicLayout)" />
        }
    </Found>
    <NotFound>
        <LayoutView Layout="typeof(PublicLayout)">
            <h1>404 — Not Found</h1>
        </LayoutView>
    </NotFound>
</Router>
```

---

## Task MVP-6.2 — End-to-End Checklist (agent must verify each)

The agent must manually verify (or write integration test for) each item:

```
CHECKLIST:
[ ] dotnet build Aero.CMS.sln — 0 errors, 0 warnings

[ ] dotnet run --project Aero.CMS.Web starts without exception

[ ] GET / returns 404 (no pages yet) — does NOT throw 500

[ ] GET /admin returns PageList with "No pages yet" message

[ ] Create a page via admin:
    - Click "+ New Page"
    - Enter name "Home", slug "/", click Create
    - Page appears in list

[ ] GET / now renders the Home page (empty sections)

[ ] Open page editor via "Edit" button
    - Add section (Full Width)
    - Add Hero block to the section
    - Enter heading and subtext
    - Saved indicator appears

[ ] GET / renders the Hero block with the heading text

[ ] Add a RichText block below the Hero in the same section
    - Enter some HTML content
    - Saved

[ ] GET / renders both blocks in order

[ ] Add a second section (Two Column)
    - Add a Quote block to left column
    - Add a Rich Text block to right column
    - Saved

[ ] GET / renders two sections — single column, then two-column

[ ] Delete a block — it disappears from the public page

[ ] Delete a section — all its blocks gone from public page

[ ] Delete the page — redirected to /admin, page no longer in list

[ ] GET / returns 404 again
```

---

# PHASE MVP-7 — Tests for MVP Components

Goal: Unit test coverage for all new MVP services.
Prerequisites: MVP-6 checklist passing.

---

## Task MVP-7.1 — Tests for SectionService (if not already done in MVP-2)

Confirm Aero.CMS.Tests.Unit/Content/SectionServiceTests.cs
passes all tests specified in MVP-2.

---

## Task MVP-7.2 — Tests for PageService (if not already done in MVP-3)

Confirm Aero.CMS.Tests.Unit/Content/PageServiceTests.cs
passes all tests specified in MVP-3.

---

## Task MVP-7.3 — Integration test: full page create → save → load cycle

FILE: Aero.CMS.Tests.Integration/Content/PageCycleTests.cs

```
MUST TEST:
- CreatePageAsync creates page in RavenDB with correct slug
- GetBySlugAsync retrieves the created page
- AddSection + AddBlock + SavePageAsync persists full block tree
- After save and load, SectionBlock type preserved
- After save and load, HeroBlock children of column preserved
- After save and load, Hero.Heading value correct
- DeletePageAsync removes page from RavenDB
- After delete, GetBySlugAsync returns null
```

---

## Task MVP-7.4 — Integration test: SiteBootstrap

Already specified in MVP-1.3. Confirm passing.

FINAL MVP GATE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration
  dotnet build Aero.CMS.sln

  Expected:
  - Zero failures, zero errors
  - MVP-6 end-to-end checklist complete
  - A page with at least one Hero block and one RichText block
    renders at its public slug

---

# WHAT THIS MVP DELIBERATELY OMITS

The following are designed in but NOT in this spec.
They slot in without structural change:

| Feature                | Where it connects                              |
|------------------------|------------------------------------------------|
| Auth / login           | Middleware before /admin routes                |
| Publishing workflow    | IPublishingWorkflow already exists             |
| Media picker           | IMediaProvider already exists                  |
| Rich text editor swap  | IRichTextEditor — replace textarea in editor   |
| SortableJS drag-drop   | Replace manual move buttons in BlockCanvas     |
| Multi-site             | SiteDocument + siteId already in ContentDocument |
| SEO fields             | ISeoCheck already exists, add to PageEditor    |
| Semantic search        | SearchText already extracted on save           |
| Plugin blocks          | IBlockRegistry + ICmsPlugin already exists     |
| Preview mode           | IsPreview flag already in ContentFinderContext |

---

# AGENT FINAL CHECKLIST

Before declaring MVP complete:

- [ ] dotnet build — 0 errors
- [ ] dotnet test — 0 failures
- [ ] App starts without exception
- [ ] /admin loads PageList
- [ ] Can create a page
- [ ] Can add sections and blocks
- [ ] Public slug renders the page
- [ ] Block edits persist after browser refresh
- [ ] Page delete works end-to-end
- [ ] No [Skip] tests anywhere
- [ ] No NotImplementedException anywhere
- [ ] No hardcoded Guids or magic strings outside constants
