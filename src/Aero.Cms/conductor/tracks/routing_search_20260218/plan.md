# Track Plan: Routing & Search

**Track ID:** routing_search_20260218
**Phases:** 6-8

---

## Phase 6: Content Finder Pipeline

- [ ] Task: Create ContentFinderContext
    - [ ] Create file: Aero.CMS.Core/Content/Models/ContentFinderContext.cs
    - [ ] Properties: Slug (required), HttpContext (required), LanguageCode, IsPreview, PreviewToken

- [ ] Task: Create IContentFinder interface
    - [ ] Create file: Aero.CMS.Core/Content/Interfaces/IContentFinder.cs
    - [ ] Priority property (int)
    - [ ] FindAsync method returning ContentDocument?

- [ ] Task: Create ContentFinderPipeline
    - [ ] Create file: Aero.CMS.Core/Content/Services/ContentFinderPipeline.cs
    - [ ] Accept IEnumerable<IContentFinder>
    - [ ] Order by Priority ascending
    - [ ] ExecuteAsync returns first non-null result

- [ ] Task: Write ContentFinderPipeline unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/ContentFinderPipelineTests.cs
    - [ ] Test: Finders called in Priority order (lower first)
    - [ ] Test: Returns first non-null result
    - [ ] Test: Returns null when all finders return null
    - [ ] Test: Stops after first success

- [ ] Task: Create DefaultContentFinder
    - [ ] Create file: Aero.CMS.Core/Content/ContentFinders/DefaultContentFinder.cs
    - [ ] Priority = 100
    - [ ] Check Status == Published, PublishedAt <= UtcNow, ExpiresAt > UtcNow (or null)
    - [ ] Bypass checks if IsPreview

- [ ] Task: Write DefaultContentFinder unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/DefaultContentFinderTests.cs
    - [ ] Test: Returns doc when Published, PublishedAt in past, no ExpiresAt
    - [ ] Test: Returns null when doc not found
    - [ ] Test: Returns null for Draft in non-preview mode
    - [ ] Test: Returns null when PublishedAt is future
    - [ ] Test: Returns null when ExpiresAt is past
    - [ ] Test: Returns doc in preview regardless of Status

- [ ] Task: Add Microsoft.AspNetCore.App reference to Aero.CMS.Routing
    - [ ] Update project file with FrameworkReference

- [ ] Task: Create AeroRouteValueTransformer
    - [ ] Create file: Aero.CMS.Routing/AeroRouteValueTransformer.cs
    - [ ] Extend DynamicRouteValueTransformer
    - [ ] Extract slug from Request.Path
    - [ ] Call ContentFinderPipeline.ExecuteAsync
    - [ ] Set controller/action to AeroRender
    - [ ] Store content in HttpContext.Items["AeroContent"]

- [ ] Task: Verify Phase 6 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~ContentFinder"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Pipeline"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 6: Content Finder Pipeline' (Protocol in workflow.md)

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
