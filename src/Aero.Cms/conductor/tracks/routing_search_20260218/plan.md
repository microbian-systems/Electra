# Track Plan: Routing & Search

**Track ID:** routing_search_20260218
**Phases:** 6-8

---

## Phase 6: Content Finder Pipeline

- [x] Task: Create ContentFinderContext
    - [x] Create file: Aero.CMS.Core/Content/Models/ContentFinderContext.cs
    - [x] Properties: Slug (required), HttpContext (required), LanguageCode, IsPreview, PreviewToken

- [x] Task: Create IContentFinder interface
    - [x] Create file: Aero.CMS.Core/Content/Interfaces/IContentFinder.cs
    - [x] Priority property (int)
    - [x] FindAsync method returning ContentDocument?

- [x] Task: Create ContentFinderPipeline
    - [x] Create file: Aero.CMS.Core/Content/Services/ContentFinderPipeline.cs
    - [x] Accept IEnumerable<IContentFinder>
    - [x] Order by Priority ascending
    - [x] ExecuteAsync returns first non-null result

- [x] Task: Write ContentFinderPipeline unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/ContentFinderPipelineTests.cs
    - [x] Test: Finders called in Priority order (lower first)
    - [x] Test: Returns first non-null result
    - [x] Test: Returns null when all finders return null
    - [x] Test: Stops after first success

- [x] Task: Create DefaultContentFinder
    - [x] Create file: Aero.CMS.Core/Content/ContentFinders/DefaultContentFinder.cs
    - [x] Priority = 100
    - [x] Check Status == Published, PublishedAt <= UtcNow, ExpiresAt > UtcNow (or null)
    - [x] Bypass checks if IsPreview

- [x] Task: Write DefaultContentFinder unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/DefaultContentFinderTests.cs
    - [x] Test: Returns doc when Published, PublishedAt in past, no ExpiresAt
    - [x] Test: Returns null when doc not found
    - [x] Test: Returns null for Draft in non-preview mode
    - [x] Test: Returns null when PublishedAt is future
    - [x] Test: Returns null when ExpiresAt is past
    - [x] Test: Returns doc in preview regardless of Status

- [x] Task: Add Microsoft.AspNetCore.App reference to Aero.CMS.Routing
    - [x] Update project file with FrameworkReference

- [x] Task: Create AeroRouteValueTransformer
    - [x] Create file: Aero.CMS.Routing/AeroRouteValueTransformer.cs
    - [x] Extend DynamicRouteValueTransformer
    - [x] Extract slug from Request.Path
    - [x] Call ContentFinderPipeline.ExecuteAsync
    - [x] Set controller/action to AeroRender
    - [x] Store content in HttpContext.Items["AeroContent"]

- [x] Task: Verify Phase 6 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~ContentFinder"`
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Pipeline"`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 6: Content Finder Pipeline' (Protocol in workflow.md)

---

## Phase 7: Save Hook Pipeline

- [ ] Task: Create ISaveHook interfaces
    - [ ] Create file: Aero.CMS.Core/Shared/Interfaces/ISaveHook.cs
    - [ ] IBeforeSaveHook<T> with Priority and ExecuteAsync
    - [ ] IAfterSaveHook<T> with Priority and ExecuteAsync

- [ ] Task: Create SaveHookPipeline
    - [ ] Create file: Aero.CMS.Core/Shared/Services/SaveHookPipeline.cs
    - [ ] Accept before/after hooks in constructor
    - [ ] RunBeforeAsync executes in Priority order
    - [ ] RunAfterAsync executes in Priority order

- [ ] Task: Write SaveHookPipeline unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Shared/SaveHookPipelineTests.cs
    - [ ] Test: Before hooks execute in Priority order
    - [ ] Test: After hooks execute in Priority order
    - [ ] Test: All registered hooks called
    - [ ] Test: Hooks receive correct entity instance
    - [ ] Test: Empty hook lists do not throw

- [ ] Task: Update ContentRepository
    - [ ] Modify: Aero.CMS.Core/Content/Data/ContentRepository.cs
    - [ ] Add SaveHookPipeline<ContentDocument> to constructor
    - [ ] Call RunBeforeAsync before session.StoreAsync
    - [ ] Call RunAfterAsync after session.SaveChangesAsync

- [ ] Task: Write ContentRepositoryWithHooks unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/ContentRepositoryWithHooksTests.cs
    - [ ] Use NSubstitute for hooks and session
    - [ ] Test: Before hook called before save
    - [ ] Test: After hook called after save
    - [ ] Test: Both hooks receive same entity instance
    - [ ] Test: Hook order matches Priority

- [ ] Task: Verify Phase 7 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] Confirm full suite green

- [ ] Task: Conductor - User Manual Verification 'Phase 7: Save Hook Pipeline' (Protocol in workflow.md)

---

## Phase 8: Search Text Extraction

- [ ] Task: Create IBlockTextExtractor interface
    - [ ] Create file: Aero.CMS.Core/Content/Interfaces/IBlockTextExtractor.cs
    - [ ] BlockType property (string)
    - [ ] Extract method returning string?

- [ ] Task: Create RichTextBlockExtractor
    - [ ] Create file: Aero.CMS.Core/Content/Search/Extractors/RichTextBlockExtractor.cs
    - [ ] BlockType = "richTextBlock"
    - [ ] Strip HTML using Regex: `<[^>]+>`
    - [ ] Return null for empty html

- [ ] Task: Create MarkdownBlockExtractor
    - [ ] Create file: Aero.CMS.Core/Content/Search/Extractors/MarkdownBlockExtractor.cs
    - [ ] BlockType = "markdownBlock"
    - [ ] Strip: # markers, ** bold, []() links
    - [ ] Return null for empty markdown

- [ ] Task: Create ImageBlockExtractor
    - [ ] Create file: Aero.CMS.Core/Content/Search/Extractors/ImageBlockExtractor.cs
    - [ ] BlockType = "imageBlock"
    - [ ] Return alt text from Properties

- [ ] Task: Create HeroBlockExtractor
    - [ ] Create file: Aero.CMS.Core/Content/Search/Extractors/HeroBlockExtractor.cs
    - [ ] BlockType = "heroBlock"
    - [ ] Return heading + "\n" + subtext

- [ ] Task: Create QuoteBlockExtractor
    - [ ] Create file: Aero.CMS.Core/Content/Search/Extractors/QuoteBlockExtractor.cs
    - [ ] BlockType = "quoteBlock"
    - [ ] Return quote + "\n" + attribution

- [ ] Task: Write BlockTextExtractor unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/BlockTextExtractorTests.cs
    - [ ] Test RichTextBlockExtractor: strips HTML, returns null for empty
    - [ ] Test MarkdownBlockExtractor: strips syntax, returns null for empty
    - [ ] Test ImageBlockExtractor: returns alt, null when absent
    - [ ] Test HeroBlockExtractor: returns heading/subtext
    - [ ] Test QuoteBlockExtractor: returns quote/attribution

- [ ] Task: Create BlockTreeTextExtractor
    - [ ] Create file: Aero.CMS.Core/Content/Search/BlockTreeTextExtractor.cs
    - [ ] Build dictionary from registered extractors
    - [ ] Implement DFS traversal
    - [ ] Add double newlines between blocks

- [ ] Task: Write BlockTreeTextExtractor unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/BlockTreeTextExtractorTests.cs
    - [ ] Test: Flat list extracts from all blocks
    - [ ] Test: DivBlock children recursively extracted
    - [ ] Test: Nested DivBlock > DivBlock > RichText fully traversed
    - [ ] Test: Unregistered block types skipped
    - [ ] Test: Empty list returns empty string
    - [ ] Test: Double newline between blocks
    - [ ] Test: Order matches DFS (parent before children)

- [ ] Task: Create ContentSearchIndexerHook
    - [ ] Create file: Aero.CMS.Core/Content/Search/ContentSearchIndexerHook.cs
    - [ ] Implement IBeforeSaveHook<ContentDocument>
    - [ ] Priority = 10
    - [ ] Set SearchText from BlockTreeTextExtractor
    - [ ] Set Search.Title from pageTitle property or Name
    - [ ] Set Search.LastIndexed from ISystemClock

- [ ] Task: Write ContentSearchIndexerHook unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/ContentSearchIndexerHookTests.cs
    - [ ] Test: SearchText populated from blocks
    - [ ] Test: Search.Title from pageTitle property
    - [ ] Test: Search.Title falls back to Name
    - [ ] Test: Search.LastIndexed set from clock
    - [ ] Test: Priority is 10

- [ ] Task: Verify Phase 8 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Search"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Extractor"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 8: Search Text Extraction' (Protocol in workflow.md)
