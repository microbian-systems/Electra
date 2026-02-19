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

- [ ] Task: Create RichTextBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/RichTextBlock.cs
    - [ ] BlockType = "richTextBlock"
    - [ ] Html property using Properties dictionary

- [ ] Task: Create MarkdownBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/MarkdownBlock.cs
    - [ ] BlockType = "markdownBlock"
    - [ ] Markdown property using Properties dictionary

- [ ] Task: Create ImageBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/ImageBlock.cs
    - [ ] BlockType = "imageBlock"
    - [ ] MediaId and Alt properties

- [ ] Task: Create HeroBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/HeroBlock.cs
    - [ ] BlockType = "heroBlock"
    - [ ] Heading and Subtext properties

- [ ] Task: Create QuoteBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/QuoteBlock.cs
    - [ ] BlockType = "quoteBlock"
    - [ ] Quote and Attribution properties

- [ ] Task: Create DivBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/DivBlock.cs
    - [ ] BlockType = "divBlock"
    - [ ] Implement ICompositeContentBlock
    - [ ] CssClass property

- [ ] Task: Create GridBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/GridBlock.cs
    - [ ] BlockType = "gridBlock"
    - [ ] Implement ICompositeContentBlock (MaxChildren=12, AllowNestedComposites=false)
    - [ ] Columns property

- [ ] Task: Write ContentBlock unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/ContentBlockTests.cs
    - [ ] Test: RichTextBlock.Type == "richTextBlock"
    - [ ] Test: RichTextBlock.Html getter/setter round-trips
    - [ ] Test: ImageBlock.MediaId returns null when absent
    - [ ] Test: DivBlock implements ICompositeContentBlock
    - [ ] Test: GridBlock.MaxChildren is 12
    - [ ] Test: GridBlock.AllowNestedComposites is false
    - [ ] Test: New block has non-empty Guid Id

- [ ] Task: Create SearchMetadata
    - [ ] Create file: Aero.CMS.Core/Content/Models/SearchMetadata.cs
    - [ ] Properties: Title, Description, ImageAlts, LastIndexed

- [ ] Task: Create ContentDocument
    - [ ] Create file: Aero.CMS.Core/Content/Models/ContentDocument.cs
    - [ ] Extend AuditableDocument
    - [ ] Properties: Name, Slug, ContentTypeAlias, Status, PublishedAt, ExpiresAt, ParentId, SortOrder, LanguageCode
    - [ ] Properties dictionary, Blocks list
    - [ ] SearchText, Search metadata

- [ ] Task: Write ContentDocument unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/ContentDocumentTests.cs
    - [ ] Test: New ContentDocument has Status = Draft
    - [ ] Test: New ContentDocument has empty Blocks list
    - [ ] Test: PublishedAt is null
    - [ ] Test: ParentId is null

- [ ] Task: Create IContentRepository interface
    - [ ] Create file: Aero.CMS.Core/Content/Data/ContentRepository.cs
    - [ ] Extend IRepository<ContentDocument>
    - [ ] Add GetBySlugAsync, GetChildrenAsync, GetByContentTypeAsync

- [ ] Task: Create ContentRepository implementation
    - [ ] Implement all three methods
    - [ ] Each opens its own session

- [ ] Task: Write ContentRepository integration tests
    - [ ] Create file: Aero.CMS.Tests.Integration/Content/ContentRepositoryTests.cs
    - [ ] Test: SaveAsync then GetByIdAsync retrieves correctly
    - [ ] Test: GetBySlugAsync returns correct document
    - [ ] Test: GetBySlugAsync returns null for unknown slug
    - [ ] Test: GetChildrenAsync returns all children by ParentId
    - [ ] Test: GetChildrenAsync with statusFilter only returns matching status
    - [ ] Test: GetChildrenAsync results ordered by SortOrder
    - [ ] Test: GetByContentTypeAsync returns only matching alias
    - [ ] **CRITICAL:** Test polymorphic block serialization (RichTextBlock, MarkdownBlock, DivBlock with nested RichTextBlock)

- [ ] Task: Verify Phase 3 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Content"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Content"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 3: Content Domain Model' (Protocol in workflow.md)

---

## Phase 4: Content Type Document

- [ ] Task: Create PropertyType enum
    - [ ] Create file: Aero.CMS.Core/Content/Models/PropertyType.cs
    - [ ] Define all property types (Text, TextArea, RichText, etc.)

- [ ] Task: Create ContentTypeProperty
    - [ ] Create file: Aero.CMS.Core/Content/Models/ContentTypeProperty.cs
    - [ ] Properties: Id, Name, Alias, PropertyType, Description, Required, SortOrder, TabAlias, Settings

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
