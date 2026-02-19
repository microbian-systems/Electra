# Track Plan: Feature Subsystems

**Track ID:** features_20260218
**Phases:** 12-14

---

## Phase 12: Markdown Subsystem

- [ ] Task: Add Markdig NuGet to Aero.CMS.Core
    - [ ] Add package reference to project file

- [ ] Task: Create SlugHelper
    - [ ] Create file: Aero.CMS.Core/Extensions/SlugHelper.cs
    - [ ] Static Generate(string) method
    - [ ] Lowercase, remove special chars, spaces to hyphens, collapse hyphens

- [ ] Task: Write SlugHelper unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/SlugHelperTests.cs
    - [ ] Test: Lowercases input
    - [ ] Test: Spaces become hyphens
    - [ ] Test: Special chars removed
    - [ ] Test: Multiple hyphens collapsed
    - [ ] Test: Leading/trailing hyphens trimmed
    - [ ] Test: "Hello World!" => "hello-world"
    - [ ] Test: Empty input returns empty string

- [ ] Task: Create MarkdownRendererService
    - [ ] Create file: Aero.CMS.Core/Content/Services/MarkdownRendererService.cs
    - [ ] Create MarkdownPipeline with UseAdvancedExtensions, UseYamlFrontMatter
    - [ ] ToHtml method
    - [ ] ParseWithFrontmatter method returning tuple

- [ ] Task: Write MarkdownRendererService unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/MarkdownRendererServiceTests.cs
    - [ ] Test: ToHtml converts paragraph to <p>
    - [ ] Test: ToHtml converts # to <h1>
    - [ ] Test: ToHtml converts **bold** to <strong>
    - [ ] Test: ParseWithFrontmatter extracts title
    - [ ] Test: ParseWithFrontmatter returns body without YAML
    - [ ] Test: No frontmatter returns full content as body

- [ ] Task: Create MarkdownImportService
    - [ ] Create file: Aero.CMS.Core/Content/Services/MarkdownImportService.cs
    - [ ] Dependencies: MarkdownRendererService, IContentRepository
    - [ ] ImportAsync creates ContentDocument with blogPost type
    - [ ] Extract frontmatter for title, slug, author, tags
    - [ ] Generate slug from title if not provided

- [ ] Task: Write MarkdownImportService unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/MarkdownImportServiceTests.cs
    - [ ] Use NSubstitute for IContentRepository
    - [ ] Test: Returns HandlerResult Success
    - [ ] Test: ContentTypeAlias = "blogPost"
    - [ ] Test: Status = Draft
    - [ ] Test: Title from frontmatter
    - [ ] Test: Slug generated from title when absent
    - [ ] Test: One MarkdownBlock in Blocks
    - [ ] Test: SaveAsync called exactly once

- [ ] Task: Verify Phase 12 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Markdown"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Slug"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 12: Markdown Subsystem' (Protocol in workflow.md)

---

## Phase 13: SEO Subsystem

- [ ] Task: Create SEO models
    - [ ] Create file: Aero.CMS.Core/Seo/Models/SeoCheckResult.cs
    - [ ] SeoCheckStatus enum
    - [ ] SeoCheckResultItem class
    - [ ] SeoCheckResult class with Score calculation

- [ ] Task: Create ISeoCheck interface
    - [ ] Create file: Aero.CMS.Core/Seo/Interfaces/ISeoCheck.cs
    - [ ] SeoCheckContext class
    - [ ] ISeoCheck interface

- [ ] Task: Create PageTitleSeoCheck
    - [ ] Create file: Aero.CMS.Core/Seo/Checks/PageTitleSeoCheck.cs
    - [ ] Pass: 10-60 chars
    - [ ] Warning: <10 or >60
    - [ ] Fail: absent

- [ ] Task: Create MetaDescriptionSeoCheck
    - [ ] Create file: Aero.CMS.Core/Seo/Checks/MetaDescriptionSeoCheck.cs
    - [ ] Pass: 50-160 chars
    - [ ] Warning: >160
    - [ ] Fail: absent

- [ ] Task: Create WordCountSeoCheck
    - [ ] Create file: Aero.CMS.Core/Seo/Checks/WordCountSeoCheck.cs
    - [ ] Pass: >=300 words
    - [ ] Warning: <300
    - [ ] Fail: empty SearchText

- [ ] Task: Create HeadingOneSeoCheck
    - [ ] Create file: Aero.CMS.Core/Seo/Checks/HeadingOneSeoCheck.cs
    - [ ] Pass: exactly one <h1>
    - [ ] Warning: multiple <h1>
    - [ ] Fail: no <h1>
    - [ ] Info: RenderedHtml is null

- [ ] Task: Write SEO check unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Seo/SeoCheckTests.cs
    - [ ] Test PageTitleSeoCheck: pass/warning/fail conditions
    - [ ] Test MetaDescriptionSeoCheck: pass/warning/fail conditions
    - [ ] Test WordCountSeoCheck: pass/warning/fail conditions
    - [ ] Test HeadingOneSeoCheck: pass/warning/fail/info conditions

- [ ] Task: Create SeoRedirectDocument
    - [ ] Create file: Aero.CMS.Core/Seo/Models/SeoRedirectDocument.cs
    - [ ] Extend AuditableDocument
    - [ ] Properties: FromSlug, ToSlug, StatusCode (default 301), IsActive

- [ ] Task: Create SeoRedirectRepository
    - [ ] Create file: Aero.CMS.Core/Seo/Data/SeoRedirectRepository.cs
    - [ ] ISeoRedirectRepository interface
    - [ ] FindByFromSlugAsync method

- [ ] Task: Verify Phase 13 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Seo"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 13: SEO Subsystem' (Protocol in workflow.md)

---

## Phase 14: Media Domain

- [ ] Task: Create MediaType enum
    - [ ] Create file: Aero.CMS.Core/Media/Models/MediaDocument.cs (include enum)
    - [ ] Image, Video, Document, Audio, Other

- [ ] Task: Create MediaDocument
    - [ ] Extend AuditableDocument
    - [ ] Properties: Name, FileName, ContentType, FileSize, MediaType
    - [ ] StorageKey, AltText, ParentFolderId, Width, Height

- [ ] Task: Create IMediaProvider interface
    - [ ] Create file: Aero.CMS.Core/Media/Interfaces/IMediaProvider.cs
    - [ ] MediaUploadResult record
    - [ ] ProviderAlias, UploadAsync, DeleteAsync, GetPublicUrl

- [ ] Task: Create DiskStorageProvider
    - [ ] Create file: Aero.CMS.Core/Media/Providers/DiskStorageProvider.cs
    - [ ] Dependency: IWebHostEnvironment
    - [ ] ProviderAlias = "disk"
    - [ ] BasePath = wwwroot/media
    - [ ] Storage key format: {Guid}/{fileName}

- [ ] Task: Write DiskStorageProvider unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Media/DiskStorageProviderTests.cs
    - [ ] Test: ProviderAlias == "disk"
    - [ ] Test: UploadAsync returns success with StorageKey
    - [ ] Test: UploadAsync creates file
    - [ ] Test: DeleteAsync removes file
    - [ ] Test: DeleteAsync on non-existent key does not throw
    - [ ] Test: GetPublicUrl starts with /media/
    - [ ] Test: GetPublicUrl uses forward slashes

- [ ] Task: Verify Phase 14 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Media"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 14: Media Domain' (Protocol in workflow.md)
