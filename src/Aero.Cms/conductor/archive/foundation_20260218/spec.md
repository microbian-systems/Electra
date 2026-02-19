# Track Specification: Foundation Infrastructure

**Track ID:** foundation_20260218
**Phases:** 0-2
**Status:** New

## Overview

This track establishes the foundational infrastructure for Aero CMS, including the solution scaffold, core domain primitives, and RavenDB persistence layer.

## Phase 0: Solution Scaffold

### Goal
Create compilable solution with all projects, no business logic.

### Projects
```
Aero.CMS.sln
├── Aero.CMS.Core         # Models, interfaces, services, repositories
├── Aero.CMS.Components   # Blazor admin UI components
├── Aero.CMS.Routing      # MVC shell, route transformer, content finder
├── Aero.CMS.Web          # Public site, content views, block views
├── Aero.CMS.Tests.Unit
└── Aero.CMS.Tests.Integration
```

### Project Types
- `Aero.CMS.Core` - Class Library (net10.0)
- `Aero.CMS.Components` - Razor Class Library (net10.0)
- `Aero.CMS.Routing` - Class Library (net10.0)
- `Aero.CMS.Web` - Blazor Server (net10.0, --interactivity Server)
- `Aero.CMS.Tests.Unit` - xUnit (net10.0)
- `Aero.CMS.Tests.Integration` - xUnit (net10.0)

### Project References
- Components -> Core
- Routing -> Core
- Web -> Core, Components, Routing
- Tests.Unit -> Core
- Tests.Integration -> Core

### Phase 0 Gate
```bash
dotnet build Aero.CMS.sln
```
Expected: 0 errors, 0 warnings

## Phase 1: Core Primitives

### Goal
Base entity contracts, clock abstraction, result type.

### Deliverables

#### Task 1.1: IEntity and AuditableDocument
- `IEntity<TId>` interface with audit fields
- `AuditableDocument` abstract base class

#### Task 1.2: ISystemClock
- `ISystemClock` interface
- `SystemClock` implementation

#### Task 1.3: HandlerResult
- `HandlerResult` class with Ok/Fail factory methods
- `HandlerResult<T>` generic class

#### Task 1.4: IKeyVaultService
- `IKeyVaultService` interface
- `EnvironmentKeyVaultService` implementation

### Phase 1 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Shared"
```
Expected: All pass, zero failures

## Phase 2: RavenDB Infrastructure

### Goal
Document store config, base repository, verified against TestServer.

### NuGet Packages (Aero.CMS.Core)
- RavenDB.Client
- Microsoft.Extensions.Options
- Microsoft.Extensions.DependencyInjection.Abstractions

### Deliverables

#### Task 2.1: GlobalSettings
- `GlobalSettings` with `RavenDbSettings`
- Configuration for URLs, database name, revisions

#### Task 2.2: DocumentStoreFactory
- Static factory creating IDocumentStore
- IdentityPartsSeparator = '/'
- Revisions configuration

#### Task 2.3: IRepository and BaseRepository
- `IRepository<T>` interface
- `BaseRepository<T>` abstract class with CRUD

#### Task 2.4: Integration Test Base
- `RavenTestBase` abstract class extending RavenTestDriver

#### Task 2.5: DI ServiceExtensions
- `AddAeroCmsCore()` extension method

### Phase 2 Gate
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Data"
```
Expected: All pass, zero failures

## Dependencies

None - this is the first track.

## Success Criteria

- Solution compiles with zero warnings
- All unit tests pass
- All integration tests pass
- RavenDB TestDriver functional
- DI container can resolve all registered services
