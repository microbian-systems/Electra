# Track Plan: Feature Subsystems

**Track ID:** features_20260218
**Phases:** 12-14

---

## Phase 12: Markdown Subsystem

- [x] Task: Add Markdig NuGet to Aero.CMS.Core
    - [x] Add package reference to project file

- [x] Task: Create SlugHelper
    - [x] Create file: Aero.CMS.Core/Extensions/SlugHelper.cs
    - [x] Static Generate(string) method
    - [x] Lowercase, remove special chars, spaces to hyphens, collapse hyphens

- [x] Task: Write SlugHelper unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/SlugHelperTests.cs
    - [x] Test: Lowercases input
    - [x] Test: Spaces become hyphens
    - [x] Test: Special chars removed
    - [x] Test: Multiple hyphens collapsed
    - [x] Test: Leading/trailing hyphens trimmed
    - [x] Test: "Hello World!" => "hello-world"
    - [x] Test: Empty input returns empty string

- [x] Task: Create MarkdownRendererService
    - [x] Create file: Aero.CMS.Core/Content/Services/MarkdownRendererService.cs
    - [x] Create MarkdownPipeline with UseAdvancedExtensions, UseYamlFrontMatter
    - [x] ToHtml method
    - [x] ParseWithFrontmatter method returning tuple

- [x] Task: Write MarkdownRendererService unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/MarkdownRendererServiceTests.cs
    - [x] Test: ToHtml converts paragraph to <p>
    - [x] Test: ToHtml converts # to <h1>
    - [x] Test: ToHtml converts **bold** to <strong>
    - [x] Test: ParseWithFrontmatter extracts title
    - [x] Test: ParseWithFrontmatter returns body without YAML
    - [x] Test: No frontmatter returns full content as body

- [x] Task: Create MarkdownImportService
    - [x] Create file: Aero.CMS.Core/Content/Services/MarkdownImportService.cs
    - [x] Dependencies: MarkdownRendererService, IContentRepository
    - [x] ImportAsync creates ContentDocument with blogPost type
    - [x] Extract frontmatter for title, slug, author, tags
    - [x] Generate slug from title if not provided

- [x] Task: Write MarkdownImportService unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/MarkdownImportServiceTests.cs
    - [x] Use NSubstitute for IContentRepository
    - [x] Test: Returns HandlerResult Success
    - [x] Test: ContentTypeAlias = "blogPost"
    - [x] Test: Status = Draft
    - [x] Test: Title from frontmatter
    - [x] Test: Slug generated from title when absent
    - [x] Test: One MarkdownBlock in Blocks
    - [x] Test: SaveAsync called exactly once

- [x] Task: Verify Phase 12 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Markdown"`
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Slug"`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 12: Markdown Subsystem' (Protocol in workflow.md)

---

## Phase 13: SEO Subsystem

- [x] Task: Create SEO models
    - [x] Create file: Aero.CMS.Core/Seo/Models/SeoCheckResult.cs
    - [x] SeoCheckStatus enum
    - [x] SeoCheckResultItem class
    - [x] SeoCheckResult class with Score calculation

- [x] Task: Create ISeoCheck interface
    - [x] Create file: Aero.CMS.Core/Seo/Interfaces/ISeoCheck.cs
    - [x] SeoCheckContext class
    - [x] ISeoCheck interface

- [x] Task: Create PageTitleSeoCheck
    - [x] Create file: Aero.CMS.Core/Seo/Checks/PageTitleSeoCheck.cs
    - [x] Pass: 10-60 chars
    - [x] Warning: <10 or >60
    - [x] Fail: absent

- [x] Task: Create MetaDescriptionSeoCheck
    - [x] Create file: Aero.CMS.Core/Seo/Checks/MetaDescriptionSeoCheck.cs
    - [x] Pass: 50-160 chars
    - [x] Warning: >160
    - [x] Fail: absent

- [x] Task: Create WordCountSeoCheck
    - [x] Create file: Aero.CMS.Core/Seo/Checks/WordCountSeoCheck.cs
    - [x] Pass: >=300 words
    - [x] Warning: <300
    - [x] Fail: empty SearchText

- [x] Task: Create HeadingOneSeoCheck
    - [x] Create file: Aero.CMS.Core/Seo/Checks/HeadingOneSeoCheck.cs
    - [x] Pass: exactly one <h1>
    - [x] Warning: multiple <h1>
    - [x] Fail: no <h1>
    - [x] Info: RenderedHtml is null

- [x] Task: Write SEO check unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Seo/SeoCheckTests.cs
    - [x] Test PageTitleSeoCheck: pass/warning/fail conditions
    - [x] Test MetaDescriptionSeoCheck: pass/warning/fail conditions
    - [x] Test WordCountSeoCheck: pass/warning/fail conditions
    - [x] Test HeadingOneSeoCheck: pass/warning/fail/info conditions

- [x] Task: Create SeoRedirectDocument
    - [x] Create file: Aero.CMS.Core/Seo/Models/SeoRedirectDocument.cs
    - [x] Extend AuditableDocument
    - [x] Properties: FromSlug, ToSlug, StatusCode (default 301), IsActive

- [x] Task: Create SeoRedirectRepository
    - [x] Create file: Aero.CMS.Core/Seo/Data/SeoRedirectRepository.cs
    - [x] ISeoRedirectRepository interface
    - [x] FindByFromSlugAsync method

- [x] Task: Verify Phase 13 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Seo"`
    - [x] Run `dotnet test Aero.CMS.Tests.Integration`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 13: SEO Subsystem' (Protocol in workflow.md)

---

## Phase 14: Media Domain

- [x] Task: Create MediaType enum
    - [x] Create file: Aero.CMS.Core/Media/Models/MediaDocument.cs (include enum)
    - [x] Image, Video, Document, Audio, Other

- [x] Task: Create MediaDocument
    - [x] Extend AuditableDocument
    - [x] Properties: Name, FileName, ContentType, FileSize, MediaType
    - [x] StorageKey, AltText, ParentFolderId, Width, Height

- [x] Task: Create IMediaProvider interface
    - [x] Create file: Aero.CMS.Core/Media/Interfaces/IMediaProvider.cs
    - [x] MediaUploadResult record
    - [x] ProviderAlias, UploadAsync, DeleteAsync, GetPublicUrl

- [x] Task: Create DiskStorageProvider
    - [x] Create file: Aero.CMS.Core/Media/Providers/DiskStorageProvider.cs
    - [x] Dependency: IWebHostEnvironment
    - [x] ProviderAlias = "disk"
    - [x] BasePath = wwwroot/media
    - [x] Storage key format: {Guid}/{fileName}

- [x] Task: Write DiskStorageProvider unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Media/DiskStorageProviderTests.cs
    - [x] Test: ProviderAlias == "disk"
    - [x] Test: UploadAsync returns success with StorageKey
    - [x] Test: UploadAsync creates file
    - [x] Test: DeleteAsync removes file
    - [x] Test: DeleteAsync on non-existent key does not throw
    - [x] Test: GetPublicUrl starts with /media/
    - [x] Test: GetPublicUrl uses forward slashes

- [x] Task: Verify Phase 14 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Media"`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 14: Media Domain' (Protocol in workflow.md)
