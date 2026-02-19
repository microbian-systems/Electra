# Track Plan: Extensibility & Composition

**Track ID:** extensibility_20260218
**Phases:** 15-16

---

## Phase 15: Plugin System

- [ ] Task: Create ICmsPlugin interface
    - [ ] Create file: Aero.CMS.Core/Plugins/Interfaces/ICmsPlugin.cs
    - [ ] Properties: Alias, Version, DisplayName
    - [ ] Methods: ConfigureServices, ConfigureBlocks

- [ ] Task: Create PluginLoader
    - [ ] Create file: Aero.CMS.Core/Plugins/PluginLoader.cs
    - [ ] LoadedPlugins property
    - [ ] LoadFromDirectory method
    - [ ] Handle loading errors gracefully (try/catch, continue)

- [ ] Task: Create PluginLoadContext
    - [ ] Internal class in PluginLoader.cs or separate file
    - [ ] Extend AssemblyLoadContext(isCollectible: true)
    - [ ] Use AssemblyDependencyResolver

- [ ] Task: Write PluginLoader unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Plugins/PluginLoaderTests.cs
    - [ ] Test: LoadFromDirectory with non-existent path returns empty
    - [ ] Test: LoadFromDirectory with empty directory returns empty
    - [ ] Test: LoadedPlugins starts empty

- [ ] Task: Verify Phase 15 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] Confirm full suite green (phases 1-15)

- [ ] Task: Conductor - User Manual Verification 'Phase 15: Plugin System' (Protocol in workflow.md)

---

## Phase 16: DI Composition Root

- [ ] Task: Update ServiceExtensions with complete registration
    - [ ] Modify: Aero.CMS.Core/Extensions/ServiceExtensions.cs
    - [ ] Infrastructure: IDocumentStore (singleton), ISystemClock, IKeyVaultService, IBlockRegistry
    - [ ] Repositories: IContentRepository, IContentTypeRepository, ISeoRedirectRepository (scoped)
    - [ ] Save hooks: SaveHookPipeline<ContentDocument>, ContentSearchIndexerHook
    - [ ] Search: BlockTreeTextExtractor, all IBlockTextExtractor implementations
    - [ ] Content: IPublishingWorkflow, ContentFinderPipeline, DefaultContentFinder
    - [ ] Markdown: MarkdownRendererService, MarkdownImportService
    - [ ] Rich text: IRichTextEditor -> NullRichTextEditor
    - [ ] Media: IMediaProvider -> DiskStorageProvider
    - [ ] SEO: All ISeoCheck implementations
    - [ ] Identity: IBanService
    - [ ] Plugins: PluginLoader (singleton)

- [ ] Task: Write CompositionRoot integration tests
    - [ ] Create file: Aero.CMS.Tests.Integration/Infrastructure/CompositionRootTests.cs
    - [ ] Build ServiceProvider with AddAeroCmsCore
    - [ ] Verify resolution of all services
    - [ ] Verify collection counts: ISeoCheck (4), IBlockTextExtractor (5), IContentFinder (1)
    - [ ] Verify SaveHookPipeline resolves
    - [ ] Verify IBeforeSaveHook<ContentDocument> count (1)

- [ ] Task: Verify final gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] Run `dotnet test --collect:"XPlat Code Coverage"`
    - [ ] Confirm zero failures
    - [ ] Confirm Aero.CMS.Core coverage >= 80%

- [ ] Task: Conductor - User Manual Verification 'Phase 16: DI Composition Root' (Protocol in workflow.md)

---

## Track Completion

- [ ] Task: Final verification
    - [ ] All 16 phases complete
    - [ ] All unit tests pass
    - [ ] All integration tests pass
    - [ ] Coverage >= 80%
    - [ ] No NotImplementedException or TODO
    - [ ] All file paths match spec
    - [ ] NSubstitute used for all mocks
    - [ ] Shouldly used for all assertions
