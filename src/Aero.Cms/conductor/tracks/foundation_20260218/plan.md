# Track Plan: Foundation Infrastructure

**Track ID:** foundation_20260218
**Phases:** 0-2

---

## Phase 0: Solution Scaffold

- [x] Task: Create solution and projects
    - [x] Run `dotnet new sln -n Aero.CMS`
    - [x] Run `dotnet new classlib -n Aero.CMS.Core -f net10.0`
    - [x] Run `dotnet new razorclasslib -n Aero.CMS.Components -f net10.0`
    - [x] Run `dotnet new classlib -n Aero.CMS.Routing -f net10.0`
    - [x] Run `dotnet new blazor -n Aero.CMS.Web -f net10.0 --interactivity Server`
    - [x] Run `dotnet new xunit -n Aero.CMS.Tests.Unit -f net10.0`
    - [x] Run `dotnet new xunit -n Aero.CMS.Tests.Integration -f net10.0`

- [x] Task: Add projects to solution
    - [x] Add all projects to Aero.CMS.sln

- [x] Task: Configure project references
    - [x] Components -> Core
    - [x] Routing -> Core
    - [x] Web -> Core, Components, Routing
    - [x] Tests.Unit -> Core
    - [x] Tests.Integration -> Core

- [x] Task: Add NuGet packages to Tests.Unit
    - [x] xunit
    - [x] xunit.runner.visualstudio
    - [x] Shouldly
    - [x] NSubstitute
    - [x] AutoFixture
    - [x] AutoFixture.AutoNSubstitute
    - [x] Bogus
    - [x] Microsoft.NET.Test.Sdk
    - [x] coverlet.collector

- [x] Task: Add NuGet packages to Tests.Integration
    - [x] xunit
    - [x] xunit.runner.visualstudio
    - [x] Shouldly
    - [x] NSubstitute
    - [x] RavenDB.TestDriver
    - [x] Microsoft.NET.Test.Sdk
    - [x] coverlet.collector

- [x] Task: Delete placeholder files
    - [x] Remove Class1.cs from classlib projects
    - [x] Remove template placeholders from other projects

- [x] Task: Verify Phase 0 gate
    - [x] Run `dotnet build Aero.CMS.sln`
    - [x] Confirm 0 errors, 0 warnings

- [x] Task: Conductor - User Manual Verification 'Phase 0: Solution Scaffold' (Protocol in workflow.md)

---

## Phase 1: Core Primitives

- [x] Task: Create IEntity interface
    - [x] Create file: Aero.CMS.Core/Shared/Interfaces/IEntity.cs
    - [x] Define IEntity<TId> with Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

- [x] Task: Create AuditableDocument base class
    - [x] Create file: Aero.CMS.Core/Shared/Models/AuditableDocument.cs
    - [x] Implement IEntity<Guid> with default values

- [x] Task: Write AuditableDocument tests
    - [x] Create file: Aero.CMS.Tests.Unit/Shared/AuditableDocumentTests.cs
    - [x] Test: New subclass has non-empty Guid Id
    - [x] Test: New subclass has CreatedAt ~= UtcNow
    - [x] Test: UpdatedAt is null by default
    - [x] Test: UpdatedBy is null by default
    - [x] Test: Two instances have different Ids

- [x] Task: Create ISystemClock interface
    - [x] Create file: Aero.CMS.Core/Shared/Interfaces/ISystemClock.cs
    - [x] Define UtcNow property

- [x] Task: Create SystemClock implementation
    - [x] Create file: Aero.CMS.Core/Shared/Services/SystemClock.cs
    - [x] Implement ISystemClock returning DateTime.UtcNow

- [x] Task: Write SystemClock tests
    - [x] Create file: Aero.CMS.Tests.Unit/Shared/SystemClockTests.cs
    - [x] Test: UtcNow returns value ~= DateTime.UtcNow
    - [x] Test: UtcNow Kind is DateTimeKind.Utc
    - [x] Test: ISystemClock can be substituted with NSubstitute

- [x] Task: Create HandlerResult classes
    - [x] Create file: Aero.CMS.Core/Shared/Models/HandlerResult.cs
    - [x] Implement HandlerResult with Ok(), Fail() factory methods
    - [x] Implement HandlerResult<T> with Value property

- [x] Task: Write HandlerResult tests
    - [x] Create file: Aero.CMS.Tests.Unit/Shared/HandlerResultTests.cs
    - [x] Test: Ok() has Success=true, empty Errors
    - [x] Test: Fail(string) has Success=false, Errors contains message
    - [x] Test: Fail(IEnumerable) contains all messages
    - [x] Test: HandlerResult<T>.Ok(value) has Success=true and Value set
    - [x] Test: HandlerResult<T>.Fail has Success=false and Value=default

- [x] Task: Create IKeyVaultService interface
    - [x] Create file: Aero.CMS.Core/Shared/Interfaces/IKeyVaultService.cs
    - [x] Define GetSecretAsync method

- [x] Task: Create EnvironmentKeyVaultService implementation
    - [x] Create file: Aero.CMS.Core/Shared/Services/EnvironmentKeyVaultService.cs
    - [x] Implement using IConfiguration

- [x] Task: Write EnvironmentKeyVaultService tests
    - [x] Create file: Aero.CMS.Tests.Unit/Shared/EnvironmentKeyVaultServiceTests.cs
    - [x] Test: Returns value from IConfiguration for known key
    - [x] Test: Returns null for unknown key
    - [x] Test: Uses NSubstitute for IConfiguration

- [x] Task: Verify Phase 1 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Shared"`
    - [x] Confirm all pass, zero failures

- [x] Task: Conductor - User Manual Verification 'Phase 1: Core Primitives' (Protocol in workflow.md)

---

## Phase 2: RavenDB Infrastructure

- [x] Task: Add NuGet packages to Aero.CMS.Core
    - [x] RavenDB.Client
    - [x] Microsoft.Extensions.Options
    - [x] Microsoft.Extensions.DependencyInjection.Abstractions

- [x] Task: Create GlobalSettings
    - [x] Create file: Aero.CMS.Core/Settings/GlobalSettings.cs
    - [x] Define GlobalSettings with RavenDbSettings
    - [x] Define RavenDbSettings with Urls, Database, EnableRevisions, RevisionsToKeep

- [x] Task: Create DocumentStoreFactory
    - [x] Create file: Aero.CMS.Core/Data/DocumentStoreFactory.cs
    - [x] Implement Create(RavenDbSettings) static method
    - [x] Set IdentityPartsSeparator to '/'
    - [x] Configure revisions if EnableRevisions is true

- [x] Task: Create IRepository interface
    - [x] Create file: Aero.CMS.Core/Data/Interfaces/IRepository.cs
    - [x] Define GetByIdAsync, SaveAsync, DeleteAsync

- [ ] Task: Create BaseRepository implementation
    - [ ] Create file: Aero.CMS.Core/Data/BaseRepository.cs
    - [ ] Implement IRepository<T> for AuditableDocument
    - [ ] Handle audit fields in SaveAsync

- [ ] Task: Create RavenTestBase
    - [ ] Create file: Aero.CMS.Tests.Integration/Infrastructure/RavenTestBase.cs
    - [ ] Extend RavenTestDriver
    - [ ] Provide IDocumentStore property

- [ ] Task: Create ServiceExtensions (initial)
    - [ ] Create file: Aero.CMS.Core/Extensions/ServiceExtensions.cs
    - [ ] Implement AddAeroCmsCore() extension method
    - [ ] Register IDocumentStore, ISystemClock, IKeyVaultService

- [ ] Task: Write BaseRepository integration tests
    - [ ] Create file: Aero.CMS.Tests.Integration/Data/BaseRepositoryTests.cs
    - [ ] Create TestDocument : AuditableDocument
    - [ ] Create TestRepository : BaseRepository<TestDocument>
    - [ ] Test: SaveAsync then GetByIdAsync retrieves document
    - [ ] Test: SaveAsync sets UpdatedAt and UpdatedBy
    - [ ] Test: SaveAsync sets CreatedAt on new entity
    - [ ] Test: DeleteAsync removes document
    - [ ] Test: SaveAsync returns HandlerResult Success=true
    - [ ] Test: GetByIdAsync returns null for non-existent Guid

- [ ] Task: Verify Phase 2 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Data"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 2: RavenDB Infrastructure' (Protocol in workflow.md)
