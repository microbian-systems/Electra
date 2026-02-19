# Track Plan: Extensibility & Composition

**Track ID:** extensibility_20260218
**Phases:** 15-16

---

## Phase 15: Plugin System

- [x] Task: Create ICmsPlugin interface
    - [x] Create file: Aero.CMS.Core/Plugins/Interfaces/ICmsPlugin.cs
    - [x] Properties: Alias, Version, DisplayName
    - [x] Methods: ConfigureServices, ConfigureBlocks

- [x] Task: Create PluginLoader
    - [x] Create file: Aero.CMS.Core/Plugins/PluginLoader.cs
    - [x] LoadedPlugins property
    - [x] LoadFromDirectory method
    - [x] Handle loading errors gracefully (try/catch, continue)

- [x] Task: Create PluginLoadContext
    - [x] Internal class in PluginLoader.cs or separate file
    - [x] Extend AssemblyLoadContext(isCollectible: true)
    - [x] Use AssemblyDependencyResolver

- [x] Task: Write PluginLoader unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Plugins/PluginLoaderTests.cs
    - [x] Test: LoadFromDirectory with non-existent path returns empty
    - [x] Test: LoadFromDirectory with empty directory returns empty
    - [x] Test: LoadedPlugins starts empty

- [x] Task: Verify Phase 15 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit`
    - [x] Run `dotnet test Aero.CMS.Tests.Integration`
    - [x] Confirm full suite green (phases 1-15)

- [x] Task: Conductor - User Manual Verification 'Phase 15: Plugin System' (Protocol in workflow.md)

---

## Phase 16: DI Composition Root

- [x] Task: Update ServiceExtensions with complete registration
    - [x] Modify: Aero.CMS.Core/Extensions/ServiceExtensions.cs
    - [x] Infrastructure: IDocumentStore (singleton), ISystemClock, IKeyVaultService, IBlockRegistry
    - [x] Repositories: IContentRepository, IContentTypeRepository, ISeoRedirectRepository (scoped)
    - [x] Save hooks: SaveHookPipeline<ContentDocument>, ContentSearchIndexerHook
    - [x] Search: BlockTreeTextExtractor, all IBlockTextExtractor implementations
    - [x] Content: IPublishingWorkflow, ContentFinderPipeline, DefaultContentFinder
    - [x] Markdown: MarkdownRendererService, MarkdownImportService
    - [x] Rich text: IRichTextEditor -> NullRichTextEditor
    - [x] Media: IMediaProvider -> DiskStorageProvider
    - [x] SEO: All ISeoCheck implementations
    - [x] Identity: IBanService
    - [x] Plugins: PluginLoader (singleton)

- [x] Task: Write CompositionRoot integration tests
    - [x] Create file: Aero.CMS.Tests.Integration/Infrastructure/CompositionRootTests.cs
    - [x] Build ServiceProvider with AddAeroCmsCore
    - [x] Verify resolution of all services
    - [x] Verify collection counts: ISeoCheck (4), IBlockTextExtractor (5), IContentFinder (1)
    - [x] Verify SaveHookPipeline resolves
    - [x] Verify IBeforeSaveHook<ContentDocument> count (1)

- [x] Task: Verify final gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit`
    - [x] Run `dotnet test Aero.CMS.Tests.Integration`
    - [x] Run `dotnet test --collect:"XPlat Code Coverage"`
    - [x] Confirm zero failures
    - [x] Confirm Aero.CMS.Core coverage >= 80%

- [x] Task: Conductor - User Manual Verification 'Phase 16: DI Composition Root' (Protocol in workflow.md)

---

## Track Completion

- [x] Task: Final verification
    - [x] All 16 phases complete
    - [x] All unit tests pass
    - [x] All integration tests pass
    - [x] Coverage >= 80%
    - [x] No NotImplementedException or TODO
    - [x] All file paths match spec
    - [x] NSubstitute used for all mocks
    - [x] Shouldly used for all assertions
