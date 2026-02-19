# Track Specification: Feature Subsystems

**Track ID:** features_20260218
**Phases:** 12-14
**Status:** New
**Dependency:** identity_ui_20260218

## Overview

This track implements markdown processing with Markdig, SEO analysis checks, and media management with pluggable storage providers.

## Phase 12: Markdown Subsystem

### Goal
Markdig pipeline, blog import.

### NuGet Package
- Markdig (Aero.CMS.Core)

### Deliverables

#### SlugHelper
- Static Generate(string) method
- Lowercase, replace spaces with hyphens, remove special chars, collapse multiple hyphens

#### MarkdownRendererService
- MarkdownPipeline with advanced extensions and YAML frontmatter
- ToHtml(string) method
- ParseWithFrontmatter(string) returning (body, frontmatter dictionary)

#### MarkdownImportService
- ImportAsync method
- Creates ContentDocument with blogPost ContentTypeAlias
- Extracts title, slug, author, tags from frontmatter
- Creates MarkdownBlock with body content

### Phase 12 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Markdown"
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Slug"
```

## Phase 13: SEO Subsystem

### Goal
ISeoCheck pipeline, redirect document, core checks.

### Deliverables

#### SEO Models
- SeoCheckStatus enum (Pass, Warning, Fail, Info)
- SeoCheckResultItem (CheckAlias, DisplayName, Status, Message)
- SeoCheckResult (Items list, Score calculated)

#### ISeoCheck Interface
- CheckAlias, DisplayName properties
- RunAsync method with SeoCheckContext

#### SeoCheckContext
- Content (required), RenderedHtml, PublicUrl

#### Core SEO Checks
- PageTitleSeoCheck (10-60 chars pass, fail if absent)
- MetaDescriptionSeoCheck (50-160 chars pass, fail if absent)
- WordCountSeoCheck (>=300 words pass, fail if empty)
- HeadingOneSeoCheck (exactly one H1 pass)

#### SeoRedirectDocument
- Extends AuditableDocument
- FromSlug, ToSlug, StatusCode (default 301), IsActive

#### SeoRedirectRepository
- ISeoRedirectRepository with FindByFromSlugAsync

### Phase 13 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Seo"
dotnet test Aero.CMS.Tests.Integration
```

## Phase 14: Media Domain

### Goal
MediaDocument, IMediaProvider, DiskStorageProvider.

### Deliverables

#### MediaType Enum
- Image, Video, Document, Audio, Other

#### MediaDocument
- Extends AuditableDocument
- Name, FileName, ContentType, FileSize, MediaType
- StorageKey, AltText, ParentFolderId
- Width, Height (for images)

#### IMediaProvider Interface
- ProviderAlias property
- UploadAsync returning MediaUploadResult
- DeleteAsync
- GetPublicUrl

#### MediaUploadResult Record
- Success, StorageKey, Error (optional)

#### DiskStorageProvider
- Stores in wwwroot/media
- ProviderAlias = "disk"
- Creates GUID-based storage keys

### Phase 14 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Media"
```

## Dependencies

- Track: identity_ui_20260218 (Phase 9-11 complete)

## Success Criteria

- Markdown with frontmatter parses correctly
- SEO checks return appropriate statuses
- Media files upload to disk correctly
- Storage keys are unique and URL-safe
