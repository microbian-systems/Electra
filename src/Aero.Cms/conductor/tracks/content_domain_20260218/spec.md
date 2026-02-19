# Track Specification: Content Domain Model

**Track ID:** content_domain_20260218
**Phases:** 3-5
**Status:** New
**Dependency:** foundation_20260218

## Overview

This track implements the content domain model including the ContentBlock hierarchy, ContentDocument, ContentTypeDocument, and the publishing workflow state machine.

## Phase 3: Content Domain Model

### Goal
ContentDocument, ContentBlock hierarchy, PublishingStatus, all serializable to/from RavenDB with correct $type discrimination.

### Deliverables

#### PublishingStatus Enum
- Draft, PendingApproval, Approved, Published, Expired

#### ContentBlock Hierarchy
- `ContentBlock` abstract base with Id, Type, SortOrder, Properties, Children
- `ICompositeContentBlock` interface for blocks with children
- Block types: RichTextBlock, MarkdownBlock, ImageBlock, HeroBlock, QuoteBlock, DivBlock, GridBlock

#### ContentDocument
- Extends AuditableDocument
- Properties: Name, Slug, ContentTypeAlias, Status, PublishedAt, ExpiresAt, ParentId, SortOrder, LanguageCode
- Blocks list for content composition
- SearchText and SearchMetadata

#### ContentRepository
- `IContentRepository` interface
- `ContentRepository` implementation
- Methods: GetByIdAsync, GetBySlugAsync, GetChildrenAsync, GetByContentTypeAsync

### Critical Test
Polymorphic block serialization must survive RavenDB round-trip with correct concrete types.

### Phase 3 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Content"
dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Content"
```

## Phase 4: Content Type Document

### Goal
ContentTypeDocument — defines schema for page types.

### Deliverables

#### PropertyType Enum
- Text, TextArea, RichText, Markdown, Number, Toggle, DatePicker, MediaPicker, ContentPicker, Tags, BlockList, DropdownList, ColourPicker, Custom

#### ContentTypeProperty
- Id, Name, Alias, PropertyType, Description, Required, SortOrder, TabAlias, Settings

#### ContentTypeDocument
- Extends AuditableDocument
- Properties: Name, Alias, Description, Icon, RequiresApproval, AllowAtRoot
- AllowedChildContentTypes, Properties list

#### ContentTypeRepository
- `IContentTypeRepository` interface
- `ContentTypeRepository` implementation
- Methods: GetByIdAsync, GetByAliasAsync, GetAllAsync

### Phase 4 Gate
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration
```
Full suite green (phases 1-4)

## Phase 5: Publishing Workflow

### Goal
Status transition logic with invariant enforcement.

### State Transitions

| From | To | Condition |
|------|-----|-----------|
| Draft | PendingApproval | Always allowed |
| Draft | Published | Only if RequiresApproval == false |
| PendingApproval | Approved | Always allowed |
| PendingApproval | Draft | Reject — returns to Draft |
| Approved | Published | Always allowed |
| Published | Draft | Unpublish |
| Published | Expired | Always allowed |

### Deliverables

#### IPublishingWorkflow Interface
- SubmitForApprovalAsync
- ApproveAsync
- RejectAsync
- PublishAsync
- UnpublishAsync
- ExpireAsync

#### PublishingWorkflow Implementation
- Enforce all transitions
- Set PublishedAt on first publish
- Do NOT clear PublishedAt on unpublish

### Phase 5 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Publishing"
```

## Dependencies

- Track: foundation_20260218 (Phase 0-2 complete)

## Success Criteria

- ContentBlock hierarchy serializes correctly to RavenDB
- Polymorphic types preserved on load
- Publishing workflow enforces all transitions
- Invalid transitions return HandlerResult.Fail
