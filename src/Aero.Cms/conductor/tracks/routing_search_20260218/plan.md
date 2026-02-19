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

- [x] Task: Create ISaveHook interfaces
    - [x] Create file: Aero.CMS.Core/Shared/Interfaces/ISaveHook.cs
    - [x] IBeforeSaveHook<T> with Priority and ExecuteAsync
    - [x] IAfterSaveHook<T> with Priority and ExecuteAsync

- [x] Task: Create SaveHookPipeline
    - [x] Create file: Aero.CMS.Core/Shared/Services/SaveHookPipeline.cs
    - [x] Accept before/after hooks in constructor
    - [x] RunBeforeAsync executes in Priority order
    - [x] RunAfterAsync executes in Priority order

- [x] Task: Write SaveHookPipeline unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Shared/SaveHookPipelineTests.cs
    - [x] Test: Before hooks execute in Priority order
    - [x] Test: After hooks execute in Priority order
    - [x] Test: All registered hooks called
    - [x] Test: Hooks receive correct entity instance
    - [x] Test: Empty hook lists do not throw

- [x] Task: Update ContentRepository
    - [x] Modify: Aero.CMS.Core/Content/Data/ContentRepository.cs
    - [x] Add SaveHookPipeline<ContentDocument> to constructor
    - [x] Call RunBeforeAsync before session.StoreAsync
    - [x] Call RunAfterAsync after session.SaveChangesAsync

- [x] Task: Write ContentRepositoryWithHooks unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/ContentRepositoryWithHooksTests.cs
    - [x] Use NSubstitute for hooks and session
    - [x] Test: Before hook called before save
    - [x] Test: After hook called after save
    - [x] Test: Both hooks receive same entity instance
    - [x] Test: Hook order matches Priority

- [x] Task: Verify Phase 7 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit`
    - [x] Run `dotnet test Aero.CMS.Tests.Integration`
    - [x] Confirm full suite green

- [x] Task: Conductor - User Manual Verification 'Phase 7: Save Hook Pipeline' (Protocol in workflow.md)

---

## Phase 8: Search Text Extraction

- [x] Task: Create IBlockTextExtractor interface
    - [x] Create file: Aero.CMS.Core/Content/Interfaces/IBlockTextExtractor.cs
    - [x] BlockType property (string)
    - [x] Extract method returning string?

- [x] Task: Create RichTextBlockExtractor
    - [x] Create file: Aero.CMS.Core/Content/Search/Extractors/RichTextBlockExtractor.cs
    - [x] BlockType = "richTextBlock"
    - [x] Strip HTML using Regex: `<[^>]+>`
    - [x] Return null for empty html

- [x] Task: Create MarkdownBlockExtractor
    - [x] Create file: Aero.CMS.Core/Content/Search/Extractors/MarkdownBlockExtractor.cs
    - [x] BlockType = "markdownBlock"
    - [x] Strip: # markers, ** bold, []() links
    - [x] Return null for empty markdown

- [x] Task: Create ImageBlockExtractor
    - [x] Create file: Aero.CMS.Core/Content/Search/Extractors/ImageBlockExtractor.cs
    - [x] BlockType = "imageBlock"
    - [x] Return alt text from Properties

- [x] Task: Create HeroBlockExtractor
    - [x] Create file: Aero.CMS.Core/Content/Search/Extractors/HeroBlockExtractor.cs
    - [x] BlockType = "heroBlock"
    - [x] Return heading + "\n" + subtext

- [x] Task: Create QuoteBlockExtractor
    - [x] Create file: Aero.CMS.Core/Content/Search/Extractors/QuoteBlockExtractor.cs
    - [x] BlockType = "quoteBlock"
    - [x] Return quote + "\n" + attribution

- [x] Task: Write BlockTextExtractor unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/BlockTextExtractorTests.cs
    - [x] Test RichTextBlockExtractor: strips HTML, returns null for empty
    - [x] Test MarkdownBlockExtractor: strips syntax, returns null for empty
    - [x] Test ImageBlockExtractor: returns alt, null when absent
    - [x] Test HeroBlockExtractor: returns heading/subtext
    - [x] Test QuoteBlockExtractor: returns quote/attribution

- [x] Task: Create BlockTreeTextExtractor
    - [x] Create file: Aero.CMS.Core/Content/Search/BlockTreeTextExtractor.cs
    - [x] Build dictionary from registered extractors
    - [x] Implement DFS traversal
    - [x] Add double newlines between blocks

- [x] Task: Write BlockTreeTextExtractor unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/BlockTreeTextExtractorTests.cs
    - [x] Test: Flat list extracts from all blocks
    - [x] Test: DivBlock children recursively extracted
    - [x] Test: Nested DivBlock > DivBlock > RichText fully traversed
    - [x] Test: Unregistered block types skipped
    - [x] Test: Empty list returns empty string
    - [x] Test: Double newline between blocks
    - [x] Test: Order matches DFS (parent before children)

- [x] Task: Create ContentSearchIndexerHook
    - [x] Create file: Aero.CMS.Core/Content/Search/ContentSearchIndexerHook.cs
    - [x] Implement IBeforeSaveHook<ContentDocument>
    - [x] Priority = 10
    - [x] Set SearchText from BlockTreeTextExtractor
    - [x] Set Search.Title from pageTitle property or Name
    - [x] Set Search.LastIndexed from ISystemClock

- [x] Task: Write ContentSearchIndexerHook unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/ContentSearchIndexerHookTests.cs
    - [x] Test: SearchText populated from blocks
    - [x] Test: Search.Title from pageTitle property
    - [x] Test: Search.Title falls back to Name
    - [x] Test: Search.LastIndexed set from clock
    - [x] Test: Priority is 10

- [x] Task: Verify Phase 8 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Search"`
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Extractor"`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 8: Search Text Extraction' (Protocol in workflow.md)
