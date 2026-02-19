# Track Plan: Content Domain Model

**Track ID:** content_domain_20260218
**Phases:** 3-5

---

## Phase 3: Content Domain Model

- [x] Task: Create PublishingStatus enum
    - [x] Create file: Aero.CMS.Core/Content/Models/PublishingStatus.cs
    - [x] Define: Draft=0, PendingApproval=1, Approved=2, Published=3, Expired=4

- [x] Task: Create ContentBlock base class
    - [x] Create file: Aero.CMS.Core/Content/Models/ContentBlock.cs
    - [x] Properties: Id, Type, SortOrder, Properties, Children

- [x] Task: Create ICompositeContentBlock interface
    - [x] Create file: Aero.CMS.Core/Content/Models/ICompositeContentBlock.cs
    - [x] Properties: Children, AllowedChildTypes, AllowNestedComposites, MaxChildren

- [x] Task: Create RichTextBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/RichTextBlock.cs
    - [x] BlockType = "richTextBlock"
    - [x] Html property using Properties dictionary

- [x] Task: Create MarkdownBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/MarkdownBlock.cs
    - [x] BlockType = "markdownBlock"
    - [x] Markdown property using Properties dictionary

- [x] Task: Create ImageBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/ImageBlock.cs
    - [x] BlockType = "imageBlock"
    - [x] MediaId and Alt properties

- [x] Task: Create HeroBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/HeroBlock.cs
    - [x] BlockType = "heroBlock"
    - [x] Heading and Subtext properties

- [x] Task: Create QuoteBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/QuoteBlock.cs
    - [x] BlockType = "quoteBlock"
    - [x] Quote and Attribution properties

- [x] Task: Create DivBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/DivBlock.cs
    - [x] BlockType = "divBlock"
    - [x] Implement ICompositeContentBlock
    - [x] CssClass property

- [x] Task: Create GridBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/GridBlock.cs
    - [x] BlockType = "gridBlock"
    - [x] Implement ICompositeContentBlock (MaxChildren=12, AllowNestedComposites=false)
    - [x] Columns property

- [x] Task: Write ContentBlock unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/ContentBlockTests.cs
    - [x] Test: RichTextBlock.Type == "richTextBlock"
    - [x] Test: RichTextBlock.Html getter/setter round-trips
    - [x] Test: ImageBlock.MediaId returns null when absent
    - [x] Test: DivBlock implements ICompositeContentBlock
    - [x] Test: GridBlock.MaxChildren is 12
    - [x] Test: GridBlock.AllowNestedComposites is false
    - [x] Test: New block has non-empty Guid Id

- [x] Task: Create SearchMetadata
    - [x] Create file: Aero.CMS.Core/Content/Models/SearchMetadata.cs
    - [x] Properties: Title, Description, ImageAlts, LastIndexed

- [x] Task: Create ContentDocument
    - [x] Create file: Aero.CMS.Core/Content/Models/ContentDocument.cs
    - [x] Extend AuditableDocument
    - [x] Properties: Name, Slug, ContentTypeAlias, Status, PublishedAt, ExpiresAt, ParentId, SortOrder, LanguageCode
    - [x] Properties dictionary, Blocks list
    - [x] SearchText, Search metadata

- [x] Task: Write ContentDocument unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/ContentDocumentTests.cs
    - [x] Test: New ContentDocument has Status = Draft
    - [x] Test: New ContentDocument has empty Blocks list
    - [x] Test: PublishedAt is null
    - [x] Test: ParentId is null

- [x] Task: Create IContentRepository interface
    - [x] Create file: Aero.CMS.Core/Content/Data/IContentRepository.cs
    - [x] Extend IRepository<ContentDocument>
    - [x] Add GetBySlugAsync, GetChildrenAsync, GetByContentTypeAsync

- [x] Task: Create ContentRepository implementation
    - [x] Implement all three methods
    - [x] Each opens its own session

- [x] Task: Write ContentRepository integration tests
    - [x] Create file: Aero.CMS.Tests.Integration/Content/ContentRepositoryTests.cs
    - [x] Test: SaveAsync then GetByIdAsync retrieves correctly
    - [x] Test: GetBySlugAsync returns correct document
    - [x] Test: GetBySlugAsync returns null for unknown slug
    - [x] Test: GetChildrenAsync returns all children by ParentId
    - [x] Test: GetChildrenAsync with statusFilter only returns matching status
    - [x] Test: GetChildrenAsync results ordered by SortOrder
    - [x] Test: GetByContentTypeAsync returns only matching alias
    - [x] **CRITICAL:** Test polymorphic block serialization (RichTextBlock, MarkdownBlock, DivBlock with nested RichTextBlock)

- [x] Task: Verify Phase 3 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Content"`
    - [x] Run `dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Content"`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 3: Content Domain Model' (Protocol in workflow.md)

---

## Phase 4: Content Type Document

- [x] Task: Create PropertyType enum
    - [x] Create file: Aero.CMS.Core/Content/Models/PropertyType.cs
    - [x] Define all property types (Text, TextArea, RichText, etc.)

- [x] Task: Create ContentTypeProperty
    - [x] Create file: Aero.CMS.Core/Content/Models/ContentTypeProperty.cs
    - [x] Properties: Id, Name, Alias, PropertyType, Description, Required, SortOrder, TabAlias, Settings

- [ ] Task: Create ContentTypeDocument
    - [ ] Create file: Aero.CMS.Core/Content/Models/ContentTypeDocument.cs
    - [ ] Extend AuditableDocument
    - [ ] Properties: Name, Alias, Description, Icon, RequiresApproval, AllowAtRoot
    - [ ] AllowedChildContentTypes, Properties list

- [ ] Task: Write ContentTypeDocument unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/ContentTypeDocumentTests.cs
    - [ ] Test: New ContentTypeDocument has empty Properties list
    - [ ] Test: RequiresApproval is false by default
    - [ ] Test: New ContentTypeProperty has non-empty Guid Id

- [ ] Task: Create IContentTypeRepository interface
    - [ ] Create file: Aero.CMS.Core/Content/Data/ContentTypeRepository.cs
    - [ ] Extend IRepository<ContentTypeDocument>
    - [ ] Add GetByAliasAsync, GetAllAsync

- [ ] Task: Create ContentTypeRepository implementation
    - [ ] Implement both methods

- [ ] Task: Write ContentTypeRepository integration tests
    - [ ] Create file: Aero.CMS.Tests.Integration/Content/ContentTypeRepositoryTests.cs
    - [ ] Test: GetByAliasAsync returns correct document
    - [ ] Test: GetByAliasAsync returns null for unknown alias
    - [ ] Test: GetAllAsync returns all saved documents
    - [ ] Test: SaveAsync then GetByAlias retrieves with all Properties intact

- [ ] Task: Verify Phase 4 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] Confirm full suite green (phases 1-4)

- [ ] Task: Conductor - User Manual Verification 'Phase 4: Content Type Document' (Protocol in workflow.md)

---

## Phase 5: Publishing Workflow

- [ ] Task: Create IPublishingWorkflow interface
    - [ ] Create file: Aero.CMS.Core/Content/Interfaces/IPublishingWorkflow.cs
    - [ ] Methods: SubmitForApprovalAsync, ApproveAsync, RejectAsync, PublishAsync, UnpublishAsync, ExpireAsync

- [ ] Task: Create PublishingWorkflow implementation
    - [ ] Create file: Aero.CMS.Core/Content/Services/PublishingWorkflow.cs
    - [ ] Dependencies: IContentRepository, IContentTypeRepository, ISystemClock
    - [ ] Implement all state transitions per spec table

- [ ] Task: Write PublishingWorkflow unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/PublishingWorkflowTests.cs
    - [ ] Use NSubstitute for all dependencies
    - [ ] Test: Draft -> PendingApproval succeeds
    - [ ] Test: Draft -> Published succeeds when RequiresApproval=false
    - [ ] Test: Draft -> Published fails when RequiresApproval=true
    - [ ] Test: PendingApproval -> Approved succeeds
    - [ ] Test: PendingApproval -> Draft succeeds (reject)
    - [ ] Test: Approved -> Published succeeds and sets PublishedAt
    - [ ] Test: Published -> Draft succeeds, PublishedAt NOT cleared
    - [ ] Test: Published -> Expired succeeds
    - [ ] Test: PublishAsync on already-Published fails
    - [ ] Test: SubmitForApproval on Published fails
    - [ ] Test: Non-existent contentId fails
    - [ ] Test: PublishAsync sets PublishedAt via ISystemClock

- [ ] Task: Verify Phase 5 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Publishing"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 5: Publishing Workflow' (Protocol in workflow.md)
