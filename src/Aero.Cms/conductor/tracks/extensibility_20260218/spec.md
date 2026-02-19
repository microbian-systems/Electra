# Track Specification: Extensibility & Composition

**Track ID:** extensibility_20260218
**Phases:** 15-16
**Status:** New
**Dependency:** features_20260218

## Overview

This track implements the plugin system with AssemblyLoadContext and completes the DI composition root with full service registration.

## Phase 15: Plugin System

### Goal
ICmsPlugin, AssemblyLoadContext loader.

### Deliverables

#### ICmsPlugin Interface
- Alias, Version, DisplayName properties
- ConfigureServices(IServiceCollection)
- ConfigureBlocks(IBlockRegistry)

#### PluginLoader
- LoadFromDirectory(string path)
- Uses PluginLoadContext (collectible AssemblyLoadContext)
- AssemblyDependencyResolver for dependency resolution
- Returns IReadOnlyList<ICmsPlugin>

#### PluginLoadContext (internal)
- Extends AssemblyLoadContext(isCollectible: true)
- Uses AssemblyDependencyResolver

### Phase 15 Gate
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration
```
Full suite green (phases 1-15)

## Phase 16: DI Composition Root

### Goal
Full AddAeroCmsCore registration. Integration test validates.

### Deliverables

#### Complete ServiceExtensions
Replace stub with full registration:
- Infrastructure: IDocumentStore, ISystemClock, IKeyVaultService, IBlockRegistry
- Repositories: IContentRepository, IContentTypeRepository, ISeoRedirectRepository
- Save hooks: SaveHookPipeline<ContentDocument>, IBeforeSaveHook implementations
- Search: BlockTreeTextExtractor, IBlockTextExtractor implementations
- Content services: IPublishingWorkflow, ContentFinderPipeline, IContentFinder
- Markdown: MarkdownRendererService, MarkdownImportService
- Rich text: IRichTextEditor (NullRichTextEditor default)
- Media: IMediaProvider (DiskStorageProvider)
- SEO: ISeoCheck implementations
- Identity: IBanService
- Plugins: PluginLoader

### Phase 16 Gate (FINAL)
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration
dotnet test --collect:"XPlat Code Coverage"
```
- Zero failures
- Aero.CMS.Core line coverage >= 80%

## Dependencies

- Track: features_20260218 (Phase 12-14 complete)

## Success Criteria

- Plugin loader loads assemblies from directory
- All services resolve from DI container
- Coverage meets 80% threshold
- All 16 phases complete with green tests
