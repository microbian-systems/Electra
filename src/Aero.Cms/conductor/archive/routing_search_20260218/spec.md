# Track Specification: Routing & Search

**Track ID:** routing_search_20260218
**Phases:** 6-8
**Status:** New
**Dependency:** content_domain_20260218

## Overview

This track implements the content routing pipeline, save hooks for cross-cutting concerns, and search text extraction from content blocks.

## Phase 6: Content Finder Pipeline

### Goal
Route transformer, content finder chain.

### Deliverables

#### ContentFinderContext
- Slug, HttpContext, LanguageCode, IsPreview, PreviewToken

#### IContentFinder Interface
- Priority property
- FindAsync method returning ContentDocument?

#### ContentFinderPipeline
- Ordered by Priority
- ExecuteAsync returns first non-null result

#### DefaultContentFinder
- Priority 100
- Returns Published content (unless preview mode)
- Checks PublishedAt, ExpiresAt

#### AeroRouteValueTransformer
- DynamicRouteValueTransformer implementation
- Maps slug to AeroRender controller
- Stores content in HttpContext.Items

### Phase 6 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~ContentFinder"
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Pipeline"
```

## Phase 7: Save Hook Pipeline

### Goal
Cross-cutting before/after save hooks.

### Deliverables

#### ISaveHook Interfaces
- `IBeforeSaveHook<T>` with Priority and ExecuteAsync
- `IAfterSaveHook<T>` with Priority and ExecuteAsync

#### SaveHookPipeline<T>
- RunBeforeAsync - ordered by Priority
- RunAfterAsync - ordered by Priority

#### Updated ContentRepository
- Accept SaveHookPipeline<ContentDocument> in constructor
- Call RunBeforeAsync before session.StoreAsync
- Call RunAfterAsync after session.SaveChangesAsync

### Phase 7 Gate
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration
```

## Phase 8: Search Text Extraction

### Goal
DFS block extractor, per-block strategies, SearchText populated on save.

### Deliverables

#### IBlockTextExtractor Interface
- BlockType property
- Extract method returning string?

#### Concrete Extractors
- RichTextBlockExtractor (strips HTML)
- MarkdownBlockExtractor (strips markdown syntax)
- ImageBlockExtractor (returns alt text)
- HeroBlockExtractor (heading + subtext)
- QuoteBlockExtractor (quote + attribution)

#### BlockTreeTextExtractor
- DFS traversal of block tree
- Uses registered extractors
- Returns aggregated search text

#### ContentSearchIndexerHook
- Implements IBeforeSaveHook<ContentDocument>
- Populates SearchText from blocks
- Sets Search.Title and Search.LastIndexed

### Phase 8 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Search"
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Extractor"
```

## Dependencies

- Track: content_domain_20260218 (Phase 3-5 complete)

## Success Criteria

- Content finder pipeline executes in priority order
- Save hooks execute before/after repository operations
- Search text extracted from all block types including nested
- Double newlines between block text for RavenDB chunk boundaries
