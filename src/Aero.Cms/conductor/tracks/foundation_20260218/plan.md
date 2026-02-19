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

- [ ] Task: Conductor - User Manual Verification 'Phase 0: Solution Scaffold' (Protocol in workflow.md)

---

## Phase 1: Core Primitives

- [ ] Task: Create IEntity interface
    - [ ] Create file: Aero.CMS.Core/Shared/Interfaces/IEntity.cs
    - [ ] Define IEntity<TId> with Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

- [ ] Task: Create AuditableDocument base class
    - [ ] Create file: Aero.CMS.Core/Shared/Models/AuditableDocument.cs
    - [ ] Implement IEntity<Guid> with default values

- [ ] Task: Write AuditableDocument tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Shared/AuditableDocumentTests.cs
    - [ ] Test: New subclass has non-empty Guid Id
    - [ ] Test: New subclass has CreatedAt ~= UtcNow
    - [ ] Test: UpdatedAt is null by default
    - [ ] Test: UpdatedBy is null by default
    - [ ] Test: Two instances have different Ids

- [ ] Task: Create ISystemClock interface
    - [ ] Create file: Aero.CMS.Core/Shared/Interfaces/ISystemClock.cs
    - [ ] Define UtcNow property

- [ ] Task: Create SystemClock implementation
    - [ ] Create file: Aero.CMS.Core/Shared/Services/SystemClock.cs
    - [ ] Implement ISystemClock returning DateTime.UtcNow

- [ ] Task: Write SystemClock tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Shared/SystemClockTests.cs
    - [ ] Test: UtcNow returns value ~= DateTime.UtcNow
    - [ ] Test: UtcNow Kind is DateTimeKind.Utc
    - [ ] Test: ISystemClock can be substituted with NSubstitute

- [ ] Task: Create HandlerResult classes
    - [ ] Create file: Aero.CMS.Core/Shared/Models/HandlerResult.cs
    - [ ] Implement HandlerResult with Ok(), Fail() factory methods
    - [ ] Implement HandlerResult<T> with Value property

- [ ] Task: Write HandlerResult tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Shared/HandlerResultTests.cs
    - [ ] Test: Ok() has Success=true, empty Errors
    - [ ] Test: Fail(string) has Success=false, Errors contains message
    - [ ] Test: Fail(IEnumerable) contains all messages
    - [ ] Test: HandlerResult<T>.Ok(value) has Success=true and Value set
    - [ ] Test: HandlerResult<T>.Fail has Success=false and Value=default

- [ ] Task: Create IKeyVaultService interface
    - [ ] Create file: Aero.CMS.Core/Shared/Interfaces/IKeyVaultService.cs
    - [ ] Define GetSecretAsync method

- [ ] Task: Create EnvironmentKeyVaultService implementation
    - [ ] Create file: Aero.CMS.Core/Shared/Services/EnvironmentKeyVaultService.cs
    - [ ] Implement using IConfiguration

- [ ] Task: Write EnvironmentKeyVaultService tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Shared/EnvironmentKeyVaultServiceTests.cs
    - [ ] Test: Returns value from IConfiguration for known key
    - [ ] Test: Returns null for unknown key
    - [ ] Test: Uses NSubstitute for IConfiguration

- [ ] Task: Verify Phase 1 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Shared"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 1: Core Primitives' (Protocol in workflow.md)

---

## Phase 2: RavenDB Infrastructure

- [ ] Task: Add NuGet packages to Aero.CMS.Core
    - [ ] RavenDB.Client
    - [ ] Microsoft.Extensions.Options
    - [ ] Microsoft.Extensions.DependencyInjection.Abstractions

- [ ] Task: Create GlobalSettings
    - [ ] Create file: Aero.CMS.Core/Settings/GlobalSettings.cs
    - [ ] Define GlobalSettings with RavenDbSettings
    - [ ] Define RavenDbSettings with Urls, Database, EnableRevisions, RevisionsToKeep

- [ ] Task: Create DocumentStoreFactory
    - [ ] Create file: Aero.CMS.Core/Data/DocumentStoreFactory.cs
    - [ ] Implement Create(RavenDbSettings) static method
    - [ ] Set IdentityPartsSeparator to '/'
    - [ ] Configure revisions if EnableRevisions is true

- [ ] Task: Create IRepository interface
    - [ ] Create file: Aero.CMS.Core/Data/Interfaces/IRepository.cs
    - [ ] Define GetByIdAsync, SaveAsync, DeleteAsync

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
