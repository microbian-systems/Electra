# Aero CMS — AI Agent Spec-Driven Development Plan

## How to Use This Document

This document is written FOR an AI coding agent. Each task is atomic,
unambiguous, and verifiable. Rules the agent must follow without exception:

1. Never proceed to the next task if the current task tests fail.
2. Never infer or invent interfaces — all signatures are specified exactly.
3. Never skip a TEST block — every TEST must be implemented.
4. Every file path is exact — do not rename or restructure.
5. Phase gates are hard stops — run dotnet test before advancing.
   All tests must be green. Zero failures permitted.
6. Do not add unrequested NuGet packages.
7. Do not implement future phases.
8. AGENT DECISION FORBIDDEN means the answer is already provided.

---

## Architecture Reference

```
Aero.CMS.sln
├── Aero.CMS.Core         # Models, interfaces, services, repositories
├── Aero.CMS.Components   # Blazor admin UI components
├── Aero.CMS.Routing      # MVC shell, route transformer, content finder
├── Aero.CMS.Web          # Public site, content views, block views
├── Aero.CMS.Tests.Unit
└── Aero.CMS.Tests.Integration
```

ID Strategy: Guid throughout.
Persistence: RavenDB CRUD. Revisions for versioning.
Mock library: NSubstitute ONLY. No Moq. No FakeItEasy.
Assertion library: Shouldly ONLY.

---

# PHASE 0 — Solution Scaffold

Goal: Compilable solution with all projects, no business logic.
Prerequisites: .NET 10 SDK on PATH.

## Task 0.1 — Create Solution and Projects

```
dotnet new sln -n Aero.CMS
dotnet new classlib -n Aero.CMS.Core -f net10.0
dotnet new razorclasslib -n Aero.CMS.Components -f net10.0
dotnet new classlib -n Aero.CMS.Routing -f net10.0
dotnet new blazor -n Aero.CMS.Web -f net10.0 --interactivity Server
dotnet new xunit -n Aero.CMS.Tests.Unit -f net10.0
dotnet new xunit -n Aero.CMS.Tests.Integration -f net10.0
```

Add all to solution. Add references:
- Components -> Core
- Routing -> Core
- Web -> Core, Components, Routing
- Tests.Unit -> Core
- Tests.Integration -> Core

NuGet for Tests.Unit:
  xunit, xunit.runner.visualstudio, Shouldly, NSubstitute,
  AutoFixture, AutoFixture.AutoNSubstitute, Bogus, Microsoft.NET.Test.Sdk,
  coverlet.collector

NuGet for Tests.Integration:
  xunit, xunit.runner.visualstudio, Shouldly, NSubstitute,
  RavenDB.TestDriver, Microsoft.NET.Test.Sdk, coverlet.collector

Delete all placeholder files from templates.

PHASE 0 GATE:
  dotnet build Aero.CMS.sln
  Expected: 0 errors, 0 warnings.

---

# PHASE 1 — Core Primitives

Goal: Base entity contracts, clock abstraction, result type.
Prerequisites: Phase 0 green.

## Task 1.1 — IEntity and AuditableDocument

FILE: Aero.CMS.Core/Shared/Interfaces/IEntity.cs
```csharp
namespace Aero.CMS.Core.Shared.Interfaces;

public interface IEntity<TId>
{
    TId Id { get; set; }
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
```

FILE: Aero.CMS.Core/Shared/Models/AuditableDocument.cs
```csharp
namespace Aero.CMS.Core.Shared.Models;

public abstract class AuditableDocument : IEntity<Guid>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

TEST: Aero.CMS.Tests.Unit/Shared/AuditableDocumentTests.cs
  - New subclass has non-empty Guid Id
  - New subclass has CreatedAt ~= UtcNow
  - UpdatedAt is null by default
  - UpdatedBy is null by default
  - Two instances have different Ids

## Task 1.2 — ISystemClock

FILE: Aero.CMS.Core/Shared/Interfaces/ISystemClock.cs
```csharp
namespace Aero.CMS.Core.Shared.Interfaces;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}
```

FILE: Aero.CMS.Core/Shared/Services/SystemClock.cs
```csharp
namespace Aero.CMS.Core.Shared.Services;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

TEST: Aero.CMS.Tests.Unit/Shared/SystemClockTests.cs
  - UtcNow returns value ~= DateTime.UtcNow
  - UtcNow Kind is DateTimeKind.Utc
  - ISystemClock can be substituted with NSubstitute returning fixed value

## Task 1.3 — HandlerResult

FILE: Aero.CMS.Core/Shared/Models/HandlerResult.cs
```csharp
namespace Aero.CMS.Core.Shared.Models;

public class HandlerResult
{
    public bool Success { get; private init; }
    public List<string> Errors { get; private init; } = [];

    public static HandlerResult Ok() => new() { Success = true };
    public static HandlerResult Fail(string error)
        => new() { Success = false, Errors = [error] };
    public static HandlerResult Fail(IEnumerable<string> errors)
        => new() { Success = false, Errors = [..errors] };
}

public class HandlerResult<T> : HandlerResult
{
    public T? Value { get; private init; }

    public static HandlerResult<T> Ok(T value)
        => new() { Success = true, Value = value };
    public new static HandlerResult<T> Fail(string error)
        => new() { Success = false, Errors = [error] };
    public new static HandlerResult<T> Fail(IEnumerable<string> errors)
        => new() { Success = false, Errors = [..errors] };
}
```

TEST: Aero.CMS.Tests.Unit/Shared/HandlerResultTests.cs
  - Ok() has Success=true, empty Errors
  - Fail(string) has Success=false, Errors contains message
  - Fail(IEnumerable) contains all messages
  - HandlerResult<T>.Ok(value) has Success=true and Value set
  - HandlerResult<T>.Fail has Success=false and Value=default
  - Value type preserved for string and class types

## Task 1.4 — IKeyVaultService

FILE: Aero.CMS.Core/Shared/Interfaces/IKeyVaultService.cs
```csharp
namespace Aero.CMS.Core.Shared.Interfaces;

public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
}
```

FILE: Aero.CMS.Core/Shared/Services/EnvironmentKeyVaultService.cs
```csharp
namespace Aero.CMS.Core.Shared.Services;

public class EnvironmentKeyVaultService(IConfiguration configuration) : IKeyVaultService
{
    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
        => Task.FromResult(configuration[key]);
}
```

TEST: Aero.CMS.Tests.Unit/Shared/EnvironmentKeyVaultServiceTests.cs
  - Returns value from IConfiguration for known key
  - Returns null for unknown key
  - Uses NSubstitute for IConfiguration

PHASE 1 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Shared"
  Expected: All pass. Zero failures.

---

# PHASE 2 — RavenDB Infrastructure

Goal: Document store config, base repository, verified against TestServer.
Prerequisites: Phase 1 green.

NuGet for Aero.CMS.Core: RavenDB.Client, Microsoft.Extensions.Options,
  Microsoft.Extensions.DependencyInjection.Abstractions

## Task 2.1 — GlobalSettings

FILE: Aero.CMS.Core/Settings/GlobalSettings.cs
```csharp
namespace Aero.CMS.Core.Settings;

public class GlobalSettings
{
    public RavenDbSettings RavenDb { get; set; } = new();
}

public class RavenDbSettings
{
    public string[] Urls { get; set; } = ["http://localhost:8080"];
    public string Database { get; set; } = "AeroCms";
    public bool EnableRevisions { get; set; } = true;
    public int RevisionsToKeep { get; set; } = 50;
}
```

## Task 2.2 — DocumentStoreFactory

FILE: Aero.CMS.Core/Data/DocumentStoreFactory.cs

Implement static factory that:
1. Creates and initialises IDocumentStore
2. Sets IdentityPartsSeparator to '/'
3. If EnableRevisions true, configures revisions on ContentDocuments
   collection with MinimumRevisionsToKeep
4. Calls IndexCreation.CreateIndexes with Aero.CMS.Core assembly

```csharp
namespace Aero.CMS.Core.Data;

public static class DocumentStoreFactory
{
    public static IDocumentStore Create(RavenDbSettings settings) { ... }
    private static void ConfigureRevisions(IDocumentStore store, RavenDbSettings settings) { ... }
}
```

## Task 2.3 — IRepository and BaseRepository

FILE: Aero.CMS.Core/Data/Interfaces/IRepository.cs
```csharp
namespace Aero.CMS.Core.Data.Interfaces;

public interface IRepository<T> where T : AuditableDocument
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<HandlerResult> SaveAsync(T entity, string savedBy, CancellationToken ct = default);
    Task<HandlerResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

FILE: Aero.CMS.Core/Data/BaseRepository.cs
```csharp
namespace Aero.CMS.Core.Data;

public abstract class BaseRepository<T>(IDocumentStore store) : IRepository<T>
    where T : AuditableDocument
{
    protected readonly IDocumentStore Store = store;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.LoadAsync<T>(id.ToString(), ct);
    }

    public async Task<HandlerResult> SaveAsync(T entity, string savedBy,
        CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = savedBy;
        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;
        using var session = Store.OpenAsyncSession();
        await session.StoreAsync(entity, ct);
        await session.SaveChangesAsync(ct);
        return HandlerResult.Ok();
    }

    public async Task<HandlerResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        session.Delete(id.ToString());
        await session.SaveChangesAsync(ct);
        return HandlerResult.Ok();
    }
}
```

## Task 2.4 — Integration Test Base

FILE: Aero.CMS.Tests.Integration/Infrastructure/RavenTestBase.cs
```csharp
namespace Aero.CMS.Tests.Integration.Infrastructure;

public abstract class RavenTestBase : RavenTestDriver, IDisposable
{
    protected IDocumentStore Store { get; }
    protected RavenTestBase() { Store = GetDocumentStore(); }
    public new void Dispose() { Store.Dispose(); base.Dispose(); }
}
```

## Task 2.5 — DI ServiceExtensions (initial)

FILE: Aero.CMS.Core/Extensions/ServiceExtensions.cs
```csharp
namespace Aero.CMS.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddAeroCmsCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GlobalSettings>(configuration.GetSection("AeroCms"));
        services.AddSingleton<IDocumentStore>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<GlobalSettings>>().Value;
            return DocumentStoreFactory.Create(settings.RavenDb);
        });
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IKeyVaultService, EnvironmentKeyVaultService>();
        return services;
    }
}
```

TEST: Aero.CMS.Tests.Integration/Data/BaseRepositoryTests.cs
  Create concrete TestDocument : AuditableDocument and
  TestRepository : BaseRepository<TestDocument> for testing only.
  - SaveAsync then GetByIdAsync retrieves document
  - SaveAsync sets UpdatedAt and UpdatedBy correctly
  - SaveAsync on new entity sets CreatedAt if not set
  - DeleteAsync removes document (subsequent Get returns null)
  - SaveAsync returns HandlerResult Success=true
  - GetByIdAsync returns null for non-existent Guid

PHASE 2 GATE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Data"
  Expected: All pass. Zero failures.

---

# PHASE 3 — Content Domain Model

Goal: ContentDocument, ContentBlock hierarchy, PublishingStatus,
all serialisable to/from RavenDB with correct $type discrimination.
Prerequisites: Phase 2 green.

## Task 3.1 — PublishingStatus

FILE: Aero.CMS.Core/Content/Models/PublishingStatus.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public enum PublishingStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Published = 3,
    Expired = 4
}
```

## Task 3.2 — ContentBlock Hierarchy

FILE: Aero.CMS.Core/Content/Models/ContentBlock.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public abstract class ContentBlock
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = string.Empty;
    public int SortOrder { get; set; }
    public Dictionary<string, object> Properties { get; set; } = [];
    public List<ContentBlock> Children { get; set; } = [];
}
```

FILE: Aero.CMS.Core/Content/Models/ICompositeContentBlock.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public interface ICompositeContentBlock
{
    List<ContentBlock> Children { get; set; }
    IReadOnlyList<string>? AllowedChildTypes { get; }
    bool AllowNestedComposites { get; }
    int MaxChildren { get; }
}
```

Create in Aero.CMS.Core/Content/Models/Blocks/:

FILE: RichTextBlock.cs
```csharp
public class RichTextBlock : ContentBlock
{
    public static string BlockType => "richTextBlock";
    public RichTextBlock() { Type = BlockType; }
    public string Html
    {
        get => Properties.GetValueOrDefault("html")?.ToString() ?? string.Empty;
        set => Properties["html"] = value;
    }
}
```

FILE: MarkdownBlock.cs
```csharp
public class MarkdownBlock : ContentBlock
{
    public static string BlockType => "markdownBlock";
    public MarkdownBlock() { Type = BlockType; }
    public string Markdown
    {
        get => Properties.GetValueOrDefault("markdown")?.ToString() ?? string.Empty;
        set => Properties["markdown"] = value;
    }
}
```

FILE: ImageBlock.cs
```csharp
public class ImageBlock : ContentBlock
{
    public static string BlockType => "imageBlock";
    public ImageBlock() { Type = BlockType; }
    public Guid? MediaId
    {
        get => Properties.TryGetValue("mediaId", out var v)
               && Guid.TryParse(v?.ToString(), out var g) ? g : null;
        set => Properties["mediaId"] = value?.ToString() ?? string.Empty;
    }
    public string Alt
    {
        get => Properties.GetValueOrDefault("alt")?.ToString() ?? string.Empty;
        set => Properties["alt"] = value;
    }
}
```

FILE: HeroBlock.cs
```csharp
public class HeroBlock : ContentBlock
{
    public static string BlockType => "heroBlock";
    public HeroBlock() { Type = BlockType; }
    public string Heading
    {
        get => Properties.GetValueOrDefault("heading")?.ToString() ?? string.Empty;
        set => Properties["heading"] = value;
    }
    public string Subtext
    {
        get => Properties.GetValueOrDefault("subtext")?.ToString() ?? string.Empty;
        set => Properties["subtext"] = value;
    }
}
```

FILE: QuoteBlock.cs
```csharp
public class QuoteBlock : ContentBlock
{
    public static string BlockType => "quoteBlock";
    public QuoteBlock() { Type = BlockType; }
    public string Quote
    {
        get => Properties.GetValueOrDefault("quote")?.ToString() ?? string.Empty;
        set => Properties["quote"] = value;
    }
    public string Attribution
    {
        get => Properties.GetValueOrDefault("attribution")?.ToString() ?? string.Empty;
        set => Properties["attribution"] = value;
    }
}
```

FILE: DivBlock.cs
```csharp
public class DivBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "divBlock";
    public DivBlock() { Type = BlockType; }
    public IReadOnlyList<string>? AllowedChildTypes => null;
    public bool AllowNestedComposites => true;
    public int MaxChildren => -1;
    public string CssClass
    {
        get => Properties.GetValueOrDefault("cssClass")?.ToString() ?? string.Empty;
        set => Properties["cssClass"] = value;
    }
}
```

FILE: GridBlock.cs
```csharp
public class GridBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "gridBlock";
    public GridBlock() { Type = BlockType; }
    public IReadOnlyList<string>? AllowedChildTypes => null;
    public bool AllowNestedComposites => false;
    public int MaxChildren => 12;
    public int Columns
    {
        get => int.TryParse(Properties.GetValueOrDefault("columns")?.ToString(),
                   out var c) ? c : 2;
        set => Properties["columns"] = value.ToString();
    }
}
```

## Task 3.3 — SearchMetadata

FILE: Aero.CMS.Core/Content/Models/SearchMetadata.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public class SearchMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ImageAlts { get; set; } = [];
    public DateTime LastIndexed { get; set; }
}
```

## Task 3.4 — ContentDocument

FILE: Aero.CMS.Core/Content/Models/ContentDocument.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public class ContentDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContentTypeAlias { get; set; } = string.Empty;
    public PublishingStatus Status { get; set; } = PublishingStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string? LanguageCode { get; set; }
    public Dictionary<string, object> Properties { get; set; } = [];
    public List<ContentBlock> Blocks { get; set; } = [];
    public string SearchText { get; set; } = string.Empty;
    public SearchMetadata Search { get; set; } = new();
}
```

## Task 3.5 — ContentRepository

FILE: Aero.CMS.Core/Content/Data/ContentRepository.cs
```csharp
namespace Aero.CMS.Core.Content.Data;

public interface IContentRepository : IRepository<ContentDocument>
{
    Task<ContentDocument?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<ContentDocument>> GetChildrenAsync(
        Guid parentId, PublishingStatus? statusFilter = null, CancellationToken ct = default);
    Task<List<ContentDocument>> GetByContentTypeAsync(
        string contentTypeAlias, CancellationToken ct = default);
}

public class ContentRepository(IDocumentStore store)
    : BaseRepository<ContentDocument>(store), IContentRepository
{
    // Implement all three methods. Each opens its own session.
}
```

TEST: Aero.CMS.Tests.Unit/Content/ContentBlockTests.cs
  - RichTextBlock.Type == "richTextBlock"
  - RichTextBlock.Html getter/setter round-trips
  - MarkdownBlock.Type == "markdownBlock"
  - ImageBlock.MediaId returns null when absent
  - ImageBlock.MediaId round-trips correctly
  - DivBlock implements ICompositeContentBlock
  - DivBlock.AllowNestedComposites is true
  - GridBlock.MaxChildren is 12
  - GridBlock.AllowNestedComposites is false
  - New block always has non-empty Guid Id
  - Children is empty by default

TEST: Aero.CMS.Tests.Unit/Content/ContentDocumentTests.cs
  - New ContentDocument has Status = Draft
  - New ContentDocument has empty Blocks list
  - New ContentDocument has empty Properties
  - SearchText is empty string by default
  - PublishedAt is null
  - ParentId is null

TEST: Aero.CMS.Tests.Integration/Content/ContentRepositoryTests.cs
  - SaveAsync then GetByIdAsync retrieves correctly
  - GetBySlugAsync returns correct document
  - GetBySlugAsync returns null for unknown slug
  - GetChildrenAsync returns all children by ParentId
  - GetChildrenAsync with statusFilter only returns matching status
  - GetChildrenAsync results ordered by SortOrder
  - GetByContentTypeAsync returns only matching alias
  - CRITICAL: Polymorphic block serialisation test:
    ContentDocument with Blocks containing RichTextBlock, MarkdownBlock,
    DivBlock (with RichTextBlock child) survives RavenDB round-trip.
    After load, each Blocks[n].GetType() must equal original concrete type.
    DivBlock.Children[0].GetType() must equal RichTextBlock.

PHASE 3 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Content"
  dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Content"
  Expected: All pass. Zero failures.

---

# PHASE 4 — Content Type Document

Goal: ContentTypeDocument — defines schema for page types.
Prerequisites: Phase 3 green.

## Task 4.1 — PropertyType Enum

FILE: Aero.CMS.Core/Content/Models/PropertyType.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public enum PropertyType
{
    Text, TextArea, RichText, Markdown, Number, Toggle,
    DatePicker, MediaPicker, ContentPicker, Tags, BlockList,
    DropdownList, ColourPicker, Custom
}
```

## Task 4.2 — ContentTypeProperty

FILE: Aero.CMS.Core/Content/Models/ContentTypeProperty.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public class ContentTypeProperty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public int SortOrder { get; set; }
    public string? TabAlias { get; set; }
    public Dictionary<string, object> Settings { get; set; } = [];
}
```

## Task 4.3 — ContentTypeDocument

FILE: Aero.CMS.Core/Content/Models/ContentTypeDocument.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public class ContentTypeDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool RequiresApproval { get; set; }
    public bool AllowAtRoot { get; set; }
    public List<string> AllowedChildContentTypes { get; set; } = [];
    public List<ContentTypeProperty> Properties { get; set; } = [];
}
```

## Task 4.4 — ContentTypeRepository

FILE: Aero.CMS.Core/Content/Data/ContentTypeRepository.cs
```csharp
namespace Aero.CMS.Core.Content.Data;

public interface IContentTypeRepository : IRepository<ContentTypeDocument>
{
    Task<ContentTypeDocument?> GetByAliasAsync(string alias, CancellationToken ct = default);
    Task<List<ContentTypeDocument>> GetAllAsync(CancellationToken ct = default);
}

public class ContentTypeRepository(IDocumentStore store)
    : BaseRepository<ContentTypeDocument>(store), IContentTypeRepository
{
    // Implement both methods
}
```

TEST: Aero.CMS.Tests.Unit/Content/ContentTypeDocumentTests.cs
  - New ContentTypeDocument has empty Properties list
  - RequiresApproval is false by default
  - New ContentTypeProperty has non-empty Guid Id
  - ContentTypeProperty.Settings is empty dictionary by default

TEST: Aero.CMS.Tests.Integration/Content/ContentTypeRepositoryTests.cs
  - GetByAliasAsync returns correct document
  - GetByAliasAsync returns null for unknown alias
  - GetAllAsync returns all saved documents
  - SaveAsync then GetByAlias retrieves with all Properties intact

PHASE 4 GATE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration
  Expected: All pass. Zero failures. (Full suite — phases 1-4)

---

# PHASE 5 — Publishing Workflow

Goal: Status transition logic with invariant enforcement.
Prerequisites: Phase 4 green.

## Task 5.1 — IPublishingWorkflow

FILE: Aero.CMS.Core/Content/Interfaces/IPublishingWorkflow.cs
```csharp
namespace Aero.CMS.Core.Content.Interfaces;

public interface IPublishingWorkflow
{
    Task<HandlerResult> SubmitForApprovalAsync(Guid contentId, string byUser, CancellationToken ct = default);
    Task<HandlerResult> ApproveAsync(Guid contentId, string byUser, CancellationToken ct = default);
    Task<HandlerResult> RejectAsync(Guid contentId, string byUser, string reason, CancellationToken ct = default);
    Task<HandlerResult> PublishAsync(Guid contentId, string byUser, CancellationToken ct = default);
    Task<HandlerResult> UnpublishAsync(Guid contentId, string byUser, CancellationToken ct = default);
    Task<HandlerResult> ExpireAsync(Guid contentId, string byUser, CancellationToken ct = default);
}
```

## Task 5.2 — PublishingWorkflow

FILE: Aero.CMS.Core/Content/Services/PublishingWorkflow.cs

Enforce these transitions exactly:

| From              | To             | Condition                                |
|-------------------|----------------|------------------------------------------|
| Draft             | PendingApproval| Always allowed                           |
| Draft             | Published      | Only if RequiresApproval == false        |
| PendingApproval   | Approved       | Always allowed                           |
| PendingApproval   | Draft          | Reject — returns to Draft                |
| Approved          | Published      | Always allowed                           |
| Published         | Draft          | Unpublish                                |
| Published         | Expired        | Always allowed                           |
| Any invalid       | —              | Return HandlerResult.Fail with message   |

PublishAsync: set PublishedAt = clock.UtcNow if not already set.
UnpublishAsync: Status = Draft. Do NOT clear PublishedAt.
ExpireAsync: Status = Expired.

```csharp
namespace Aero.CMS.Core.Content.Services;

public class PublishingWorkflow(
    IContentRepository contentRepo,
    IContentTypeRepository contentTypeRepo,
    ISystemClock clock) : IPublishingWorkflow
{
    // Full implementation
}
```

TEST: Aero.CMS.Tests.Unit/Content/PublishingWorkflowTests.cs
  NSubstitute for all three dependencies.

  EVERY transition must be tested:
  - Draft -> PendingApproval succeeds
  - Draft -> Published succeeds when RequiresApproval=false
  - Draft -> Published fails when RequiresApproval=true
  - PendingApproval -> Approved succeeds
  - PendingApproval -> Draft succeeds (reject)
  - Approved -> Published succeeds and sets PublishedAt
  - Published -> Draft succeeds, PublishedAt NOT cleared
  - Published -> Expired succeeds, Status=Expired
  - PublishAsync on already-Published fails
  - SubmitForApproval on Published fails
  - Any method with non-existent contentId fails
  - PublishAsync sets PublishedAt via ISystemClock
  - Reject error message appears in HandlerResult.Errors

PHASE 5 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Publishing"
  Expected: All pass. Zero failures.

---

# PHASE 6 — Content Finder Pipeline

Goal: Route transformer, content finder chain.
Prerequisites: Phase 5 green.

## Task 6.1 — ContentFinderContext

FILE: Aero.CMS.Core/Content/Models/ContentFinderContext.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public class ContentFinderContext
{
    public required string Slug { get; init; }
    public required HttpContext HttpContext { get; init; }
    public string? LanguageCode { get; set; }
    public bool IsPreview { get; set; }
    public string? PreviewToken { get; set; }
}
```

## Task 6.2 — IContentFinder

FILE: Aero.CMS.Core/Content/Interfaces/IContentFinder.cs
```csharp
namespace Aero.CMS.Core.Content.Interfaces;

public interface IContentFinder
{
    int Priority { get; }
    Task<ContentDocument?> FindAsync(ContentFinderContext context, CancellationToken ct = default);
}
```

## Task 6.3 — ContentFinderPipeline

FILE: Aero.CMS.Core/Content/Services/ContentFinderPipeline.cs
```csharp
namespace Aero.CMS.Core.Content.Services;

public class ContentFinderPipeline(IEnumerable<IContentFinder> finders)
{
    private readonly IReadOnlyList<IContentFinder> _finders =
        finders.OrderBy(f => f.Priority).ToList();

    public async Task<ContentDocument?> ExecuteAsync(
        ContentFinderContext context, CancellationToken ct = default)
    {
        foreach (var finder in _finders)
        {
            var result = await finder.FindAsync(context, ct);
            if (result is not null) return result;
        }
        return null;
    }
}
```

## Task 6.4 — DefaultContentFinder

FILE: Aero.CMS.Core/Content/ContentFinders/DefaultContentFinder.cs
```csharp
namespace Aero.CMS.Core.Content.ContentFinders;

public class DefaultContentFinder(IContentRepository repository) : IContentFinder
{
    public int Priority => 100;

    public async Task<ContentDocument?> FindAsync(
        ContentFinderContext context, CancellationToken ct = default)
    {
        var doc = await repository.GetBySlugAsync(context.Slug, ct);
        if (doc is null) return null;

        if (!context.IsPreview)
        {
            if (doc.Status != PublishingStatus.Published) return null;
            if (doc.PublishedAt > DateTime.UtcNow) return null;
            if (doc.ExpiresAt.HasValue && doc.ExpiresAt < DateTime.UtcNow) return null;
        }

        return doc;
    }
}
```

## Task 6.5 — AeroRouteValueTransformer

FILE: Aero.CMS.Routing/AeroRouteValueTransformer.cs

Add Microsoft.AspNetCore.App framework reference to Aero.CMS.Routing.

```csharp
namespace Aero.CMS.Routing;

public class AeroRouteValueTransformer(ContentFinderPipeline pipeline)
    : DynamicRouteValueTransformer
{
    public override async ValueTask<RouteValueDictionary> TransformAsync(
        HttpContext httpContext, RouteValueDictionary values)
    {
        var slug = httpContext.Request.Path.Value?.TrimStart('/') ?? string.Empty;
        if (string.IsNullOrEmpty(slug)) slug = "/";

        var context = new ContentFinderContext
        {
            Slug = slug,
            HttpContext = httpContext,
            IsPreview = httpContext.Request.Query.ContainsKey("preview"),
            PreviewToken = httpContext.Request.Query["preview"]
        };

        var content = await pipeline.ExecuteAsync(context);

        if (content is null)
        {
            values["controller"] = "AeroRender";
            values["action"] = "NotFound";
            return values;
        }

        httpContext.Items["AeroContent"] = content;
        values["controller"] = "AeroRender";
        values["action"] = "Index";
        return values;
    }
}
```

TEST: Aero.CMS.Tests.Unit/Content/ContentFinderPipelineTests.cs
  - Finders called in Priority order (lower first)
  - Returns first non-null result
  - Returns null when all finders return null
  - Stops after first success (later finders not called)
  NSubstitute for IContentFinder.

TEST: Aero.CMS.Tests.Unit/Content/DefaultContentFinderTests.cs
  - Returns doc when Published, PublishedAt in past, no ExpiresAt
  - Returns null when doc not found
  - Returns null for Draft in non-preview mode
  - Returns null when PublishedAt is future
  - Returns null when ExpiresAt is past
  - Returns doc in preview regardless of Status
  - Returns Draft doc in preview mode

PHASE 6 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~ContentFinder"
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Pipeline"
  Expected: All pass. Zero failures.

---

# PHASE 7 — Save Hook Pipeline

Goal: Cross-cutting before/after save hooks.
Prerequisites: Phase 6 green.

## Task 7.1 — ISaveHook interfaces

FILE: Aero.CMS.Core/Shared/Interfaces/ISaveHook.cs
```csharp
namespace Aero.CMS.Core.Shared.Interfaces;

public interface IBeforeSaveHook<T> where T : AuditableDocument
{
    int Priority { get; }
    Task ExecuteAsync(T entity, CancellationToken ct = default);
}

public interface IAfterSaveHook<T> where T : AuditableDocument
{
    int Priority { get; }
    Task ExecuteAsync(T entity, CancellationToken ct = default);
}
```

## Task 7.2 — SaveHookPipeline

FILE: Aero.CMS.Core/Shared/Services/SaveHookPipeline.cs
```csharp
namespace Aero.CMS.Core.Shared.Services;

public class SaveHookPipeline<T>(
    IEnumerable<IBeforeSaveHook<T>> beforeHooks,
    IEnumerable<IAfterSaveHook<T>> afterHooks)
    where T : AuditableDocument
{
    private readonly IReadOnlyList<IBeforeSaveHook<T>> _before =
        beforeHooks.OrderBy(h => h.Priority).ToList();
    private readonly IReadOnlyList<IAfterSaveHook<T>> _after =
        afterHooks.OrderBy(h => h.Priority).ToList();

    public async Task RunBeforeAsync(T entity, CancellationToken ct = default)
    {
        foreach (var hook in _before)
            await hook.ExecuteAsync(entity, ct);
    }

    public async Task RunAfterAsync(T entity, CancellationToken ct = default)
    {
        foreach (var hook in _after)
            await hook.ExecuteAsync(entity, ct);
    }
}
```

## Task 7.3 — Update ContentRepository

Modify ContentRepository constructor to also accept
SaveHookPipeline<ContentDocument>. In SaveAsync, call:
  1. pipeline.RunBeforeAsync(entity) before session.StoreAsync
  2. pipeline.RunAfterAsync(entity) after session.SaveChangesAsync

TEST: Aero.CMS.Tests.Unit/Shared/SaveHookPipelineTests.cs
  - Before hooks execute in Priority order
  - After hooks execute in Priority order
  - All registered before hooks called
  - All registered after hooks called
  - Hooks receive correct entity instance
  - Empty hook lists do not throw

TEST: Aero.CMS.Tests.Unit/Content/ContentRepositoryWithHooksTests.cs
  NSubstitute for hooks and IDocumentStore session.
  - Before hook called before save
  - After hook called after save
  - Both hooks receive same entity instance
  - Hook order matches Priority

PHASE 7 GATE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration
  Expected: Full suite green.

---

# PHASE 8 — Search Text Extraction

Goal: DFS block extractor, per-block strategies, SearchText populated on save.
Prerequisites: Phase 7 green.

## Task 8.1 — IBlockTextExtractor

FILE: Aero.CMS.Core/Content/Interfaces/IBlockTextExtractor.cs
```csharp
namespace Aero.CMS.Core.Content.Interfaces;

public interface IBlockTextExtractor
{
    string BlockType { get; }
    string? Extract(ContentBlock block);
}
```

## Task 8.2 — Concrete Extractors

Create in Aero.CMS.Core/Content/Search/Extractors/:

RichTextBlockExtractor.cs
  BlockType => "richTextBlock"
  Strip HTML: Regex.Replace(html, "<[^>]+>", " ")
  Return null for empty html property.
  DO NOT add an HTML parsing NuGet package.

MarkdownBlockExtractor.cs
  BlockType => "markdownBlock"
  Strip: # markers, ** bold markers, []() links
  Return null for empty markdown.

ImageBlockExtractor.cs
  BlockType => "imageBlock"
  Return Properties["alt"] as string. Null if absent.

HeroBlockExtractor.cs
  BlockType => "heroBlock"
  Return heading + "\n" + subtext. Null if both absent.

QuoteBlockExtractor.cs
  BlockType => "quoteBlock"
  Return quote + "\n" + attribution. Null if both absent.

## Task 8.3 — BlockTreeTextExtractor

FILE: Aero.CMS.Core/Content/Search/BlockTreeTextExtractor.cs
```csharp
namespace Aero.CMS.Core.Content.Search;

public class BlockTreeTextExtractor(IEnumerable<IBlockTextExtractor> extractors)
{
    private readonly Dictionary<string, IBlockTextExtractor> _map =
        extractors.ToDictionary(e => e.BlockType);

    public string ExtractAll(List<ContentBlock> blocks)
    {
        var sb = new StringBuilder();
        ExtractDfs(blocks, sb);
        return sb.ToString().Trim();
    }

    private void ExtractDfs(List<ContentBlock> blocks, StringBuilder sb)
    {
        foreach (var block in blocks)
        {
            if (_map.TryGetValue(block.Type, out var extractor))
            {
                var text = extractor.Extract(block);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                    sb.AppendLine(); // paragraph break = RavenDB chunk boundary
                }
            }
            if (block.Children.Count > 0)
                ExtractDfs(block.Children, sb);
        }
    }
}
```

## Task 8.4 — ContentSearchIndexerHook

FILE: Aero.CMS.Core/Content/Search/ContentSearchIndexerHook.cs
```csharp
namespace Aero.CMS.Core.Content.Search;

public class ContentSearchIndexerHook(
    BlockTreeTextExtractor extractor,
    ISystemClock clock) : IBeforeSaveHook<ContentDocument>
{
    public int Priority => 10;

    public Task ExecuteAsync(ContentDocument entity, CancellationToken ct = default)
    {
        entity.SearchText = extractor.ExtractAll(entity.Blocks);
        entity.Search.Title = entity.Properties
            .GetValueOrDefault("pageTitle")?.ToString() ?? entity.Name;
        entity.Search.LastIndexed = clock.UtcNow;
        return Task.CompletedTask;
    }
}
```

TEST: Aero.CMS.Tests.Unit/Content/BlockTextExtractorTests.cs
  RichTextBlockExtractor:
    - Strips HTML tags from simple markup
    - Returns null for empty html
    - Output contains no angle brackets

  MarkdownBlockExtractor:
    - Strips # heading markers
    - Strips ** bold markers
    - Returns null for empty markdown

  ImageBlockExtractor:
    - Returns alt text value
    - Returns null when alt absent

  HeroBlockExtractor:
    - Returns heading and subtext
    - Returns heading alone when subtext absent

  QuoteBlockExtractor:
    - Returns quote and attribution

TEST: Aero.CMS.Tests.Unit/Content/BlockTreeTextExtractorTests.cs
  - Flat list extracts from all blocks
  - DivBlock children recursively extracted
  - Nested DivBlock > DivBlock > RichText fully traversed
  - Unregistered block types skipped without error
  - Empty list returns empty string
  - Double newline appears between blocks
  - Order matches DFS (parent before children)

TEST: Aero.CMS.Tests.Unit/Content/ContentSearchIndexerHookTests.cs
  - SearchText populated from blocks
  - Search.Title set from "pageTitle" property
  - Search.Title falls back to entity.Name
  - Search.LastIndexed set from ISystemClock
  - Priority is 10

PHASE 8 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Search"
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Extractor"
  Expected: All pass. Zero failures.

---

# PHASE 9 — Identity & Auth

Goal: Raven-backed ASP.NET Identity, passkeys.
Prerequisites: Phase 8 green.

NuGet for Aero.CMS.Core: Microsoft.AspNetCore.Identity

## Task 9.1 — Identity Models

FILE: Aero.CMS.Core/Membership/Models/PasskeyCredential.cs
```csharp
namespace Aero.CMS.Core.Membership.Models;

public class PasskeyCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] CredentialId { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public uint SignCount { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
```

FILE: Aero.CMS.Core/Membership/Models/RefreshToken.cs
```csharp
namespace Aero.CMS.Core.Membership.Models;

public class RefreshToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

FILE: Aero.CMS.Core/Membership/Models/UserClaim.cs
```csharp
namespace Aero.CMS.Core.Membership.Models;

public class UserClaim
{
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}
```

FILE: Aero.CMS.Core/Membership/Models/CmsUser.cs
```csharp
namespace Aero.CMS.Core.Membership.Models;

public class CmsUser : AuditableDocument
{
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }
    public string? BanReason { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public int AccessFailedCount { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<UserClaim> Claims { get; set; } = [];
    public List<PasskeyCredential> Passkeys { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
```

FILE: Aero.CMS.Core/Membership/Models/CmsRole.cs
```csharp
namespace Aero.CMS.Core.Membership.Models;

public class CmsRole : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}
```

## Task 9.2 — Permissions

FILE: Aero.CMS.Core/Membership/Models/Permissions.cs
```csharp
namespace Aero.CMS.Core.Membership.Models;

public static class Permissions
{
    public const string ContentCreate  = "content.create";
    public const string ContentEdit    = "content.edit";
    public const string ContentApprove = "content.approve";
    public const string ContentPublish = "content.publish";
    public const string ContentDelete  = "content.delete";
    public const string MediaManage    = "media.manage";
    public const string UsersManage    = "users.manage";
    public const string SettingsManage = "settings.manage";
    public const string PluginsManage  = "plugins.manage";

    public static readonly string[] All = [
        ContentCreate, ContentEdit, ContentApprove, ContentPublish,
        ContentDelete, MediaManage, UsersManage, SettingsManage, PluginsManage
    ];
}
```

## Task 9.3 — RavenUserStore

FILE: Aero.CMS.Core/Membership/Stores/RavenUserStore.cs

Implement: IUserStore<CmsUser>, IUserPasswordStore<CmsUser>,
IUserEmailStore<CmsUser>, IUserRoleStore<CmsUser>,
IUserClaimStore<CmsUser>, IUserLockoutStore<CmsUser>

Each method opens and disposes its own RavenDB session.

## Task 9.4 — RavenRoleStore

FILE: Aero.CMS.Core/Membership/Stores/RavenRoleStore.cs
Implement: IRoleStore<CmsRole>

## Task 9.5 — BanService

FILE: Aero.CMS.Core/Membership/Services/BanService.cs
```csharp
namespace Aero.CMS.Core.Membership.Services;

public interface IBanService
{
    Task<HandlerResult> BanAsync(Guid userId, string reason, DateTime? until,
        string bannedBy, CancellationToken ct = default);
    Task<HandlerResult> UnbanAsync(Guid userId, string unbannedBy, CancellationToken ct = default);
    Task<bool> IsBannedAsync(Guid userId, CancellationToken ct = default);
}

public class BanService(IDocumentStore store, ISystemClock clock) : IBanService
{
    // IsBannedAsync: true if IsBanned && (BannedUntil == null || BannedUntil > clock.UtcNow)
    // BanAsync: sets IsBanned=true, BanReason, BannedUntil
    // UnbanAsync: sets IsBanned=false, clears BanReason and BannedUntil
}
```

TEST: Aero.CMS.Tests.Unit/Membership/BanServiceTests.cs
  - BanAsync sets IsBanned=true
  - BanAsync sets BanReason
  - BanAsync with null until = permanent ban
  - BanAsync with future DateTime = temporary ban
  - UnbanAsync sets IsBanned=false, clears reason and until
  - IsBannedAsync true for permanent ban
  - IsBannedAsync true for active temporary ban
  - IsBannedAsync false for expired temporary ban (BannedUntil in past)
  - IsBannedAsync false for unbanned user

TEST: Aero.CMS.Tests.Integration/Membership/RavenUserStoreTests.cs
  - CreateAsync saves user
  - FindByIdAsync retrieves correctly
  - FindByNameAsync retrieves by NormalizedUserName
  - FindByEmailAsync retrieves by NormalizedEmail
  - UpdateAsync persists changes
  - DeleteAsync removes user
  - AddToRoleAsync adds role to Roles list
  - RemoveFromRoleAsync removes role
  - IsInRoleAsync returns correct result
  - GetRolesAsync returns all roles
  - SetPasswordHashAsync / GetPasswordHashAsync round-trip
  - GetLockoutEndDateAsync / SetLockoutEndDateAsync round-trip
  - IncrementAccessFailedCountAsync increments counter
  - ResetAccessFailedCountAsync sets to zero

PHASE 9 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Membership"
  dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Membership"
  Expected: All pass. Zero failures.

---

# PHASE 10 — Block Registry

Goal: IBlockRegistry singleton — seam between content model and Blazor.
Prerequisites: Phase 9 green.

## Task 10.1 — IBlockRegistry

FILE: Aero.CMS.Core/Plugins/Interfaces/IBlockRegistry.cs
```csharp
namespace Aero.CMS.Core.Plugins.Interfaces;

public interface IBlockRegistry
{
    void Register<TBlock, TView>()
        where TBlock : ContentBlock
        where TView : IComponent;
    void Register(string blockTypeAlias, Type viewComponentType);
    Type? Resolve(string blockTypeAlias);
    IReadOnlyDictionary<string, Type> GetAll();
}
```

## Task 10.2 — BlockRegistry

FILE: Aero.CMS.Core/Plugins/BlockRegistry.cs
```csharp
namespace Aero.CMS.Core.Plugins;

public class BlockRegistry : IBlockRegistry
{
    private readonly Dictionary<string, Type> _registry = [];

    public void Register<TBlock, TView>()
        where TBlock : ContentBlock
        where TView : IComponent
    {
        var alias = (string)typeof(TBlock)
            .GetProperty("BlockType", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
        _registry[alias] = typeof(TView);
    }

    public void Register(string blockTypeAlias, Type viewComponentType)
        => _registry[blockTypeAlias] = viewComponentType;

    public Type? Resolve(string blockTypeAlias)
        => _registry.GetValueOrDefault(blockTypeAlias);

    public IReadOnlyDictionary<string, Type> GetAll()
        => _registry.AsReadOnly();
}
```

TEST: Aero.CMS.Tests.Unit/Plugins/BlockRegistryTests.cs
  - Register<TBlock,TView> uses TBlock.BlockType as alias
  - Resolve returns correct Type for registered alias
  - Resolve returns null for unregistered alias
  - Register(string, Type) overload registers correctly
  - GetAll returns all registered entries
  - Registering same alias twice overwrites previous
  - Empty registry returns empty dictionary

PHASE 10 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Plugins"
  Expected: All pass. Zero failures.

---

# PHASE 11 — IRichTextEditor Abstraction

Goal: Swappable RTE contract before any Blazor editor is built.
Prerequisites: Phase 10 green.

## Task 11.1 — RichTextEditorSettings

FILE: Aero.CMS.Core/Content/Models/RichTextEditorSettings.cs
```csharp
namespace Aero.CMS.Core.Content.Models;

public class RichTextEditorSettings
{
    public int MinHeight { get; set; } = 300;
    public bool EnableMedia { get; set; } = true;
    public bool EnableTables { get; set; } = true;
    public bool EnableCodeBlocks { get; set; } = true;
    public List<string> ToolbarItems { get; set; } = [];
}
```

## Task 11.2 — IRichTextEditor

FILE: Aero.CMS.Core/Content/Interfaces/IRichTextEditor.cs
```csharp
namespace Aero.CMS.Core.Content.Interfaces;

public interface IRichTextEditor
{
    string EditorAlias { get; }
    RenderFragment Render(
        string value,
        bool isEditing,
        EventCallback<string> onChanged,
        RichTextEditorSettings settings);
}
```

## Task 11.3 — NullRichTextEditor

FILE: Aero.CMS.Core/Content/Services/NullRichTextEditor.cs
```csharp
namespace Aero.CMS.Core.Content.Services;

/// Fallback — renders plain textarea. Used in tests and when no RTE registered.
public class NullRichTextEditor : IRichTextEditor
{
    public string EditorAlias => "null";
    public RenderFragment Render(string value, bool isEditing,
        EventCallback<string> onChanged, RichTextEditorSettings settings)
        => builder =>
        {
            builder.OpenElement(0, "textarea");
            builder.AddAttribute(1, "value", value);
            builder.CloseElement();
        };
}
```

TEST: Aero.CMS.Tests.Unit/Content/RichTextEditorTests.cs
  - NullRichTextEditor.EditorAlias == "null"
  - NullRichTextEditor.Render returns non-null RenderFragment
  - IRichTextEditor can be substituted with NSubstitute
  - RichTextEditorSettings defaults:
      MinHeight=300, EnableMedia=true, EnableTables=true, EnableCodeBlocks=true

PHASE 11 GATE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration
  Expected: Full suite green. (Phases 1-11)

---

# PHASE 12 — Markdown Subsystem

Goal: Markdig pipeline, blog import.
Prerequisites: Phase 11 green.

NuGet for Aero.CMS.Core: Markdig

## Task 12.1 — SlugHelper

FILE: Aero.CMS.Core/Extensions/SlugHelper.cs
```csharp
namespace Aero.CMS.Core.Extensions;

public static class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var slug = input.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}
```

## Task 12.2 — MarkdownRendererService

FILE: Aero.CMS.Core/Content/Services/MarkdownRendererService.cs

```csharp
namespace Aero.CMS.Core.Content.Services;

public class MarkdownRendererService
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    public string ToHtml(string markdown)
        => Markdown.ToHtml(markdown, _pipeline);

    public (string Body, Dictionary<string, string> Frontmatter)
        ParseWithFrontmatter(string raw)
    {
        var document = Markdown.Parse(raw, _pipeline);
        var frontmatterBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        var frontmatter = new Dictionary<string, string>();

        if (frontmatterBlock is not null)
        {
            var yaml = raw[..frontmatterBlock.Span.End].Trim('-').Trim();
            foreach (var line in yaml.Split('\n'))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                    frontmatter[parts[0].Trim()] = parts[1].Trim();
            }
            return (raw[(frontmatterBlock.Span.End + 1)..].TrimStart(), frontmatter);
        }
        return (raw, frontmatter);
    }
}
```

## Task 12.3 — MarkdownImportService

FILE: Aero.CMS.Core/Content/Services/MarkdownImportService.cs
```csharp
namespace Aero.CMS.Core.Content.Services;

public class MarkdownImportService(
    MarkdownRendererService renderer,
    IContentRepository repository)
{
    public async Task<HandlerResult<ContentDocument>> ImportAsync(
        string markdownContent,
        Guid blogIndexId,
        string importedBy,
        CancellationToken ct = default)
    {
        var (body, frontmatter) = renderer.ParseWithFrontmatter(markdownContent);
        var name = frontmatter.GetValueOrDefault("title", "Untitled");
        var slug = frontmatter.GetValueOrDefault("slug", SlugHelper.Generate(name));

        var doc = new ContentDocument
        {
            Name = name,
            Slug = slug,
            ContentTypeAlias = "blogPost",
            ParentId = blogIndexId,
            Status = PublishingStatus.Draft,
            CreatedBy = importedBy
        };

        doc.Properties["author"] = frontmatter.GetValueOrDefault("author", importedBy);
        doc.Properties["tags"] = frontmatter.GetValueOrDefault("tags", string.Empty);
        doc.Blocks.Add(new MarkdownBlock { Markdown = body });

        await repository.SaveAsync(doc, importedBy, ct);
        return HandlerResult<ContentDocument>.Ok(doc);
    }
}
```

TEST: Aero.CMS.Tests.Unit/Content/SlugHelperTests.cs
  - Lowercases input
  - Spaces become hyphens
  - Special chars removed
  - Multiple hyphens collapsed
  - Leading/trailing hyphens trimmed
  - Empty input returns empty string
  - "Hello World!" => "hello-world"
  - "My Blog Post #1" => "my-blog-post-1"

TEST: Aero.CMS.Tests.Unit/Content/MarkdownRendererServiceTests.cs
  - ToHtml converts paragraph to <p>
  - ToHtml converts # to <h1>
  - ToHtml converts **bold** to <strong>
  - ParseWithFrontmatter extracts title from YAML
  - ParseWithFrontmatter returns body without YAML
  - No frontmatter returns full content as body
  - No frontmatter returns empty dictionary

TEST: Aero.CMS.Tests.Unit/Content/MarkdownImportServiceTests.cs
  NSubstitute for IContentRepository.
  - Returns HandlerResult Success=true
  - ContentTypeAlias = "blogPost"
  - Status = Draft
  - ParentId = provided blogIndexId
  - One MarkdownBlock in Blocks
  - Title from frontmatter populates Name
  - Slug from frontmatter used when present
  - Slug generated from title when not in frontmatter
  - Author from frontmatter in Properties["author"]
  - SaveAsync called exactly once

PHASE 12 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Markdown"
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Slug"
  Expected: All pass. Zero failures.

---

# PHASE 13 — SEO Subsystem

Goal: ISeoCheck pipeline, redirect document, core checks.
Prerequisites: Phase 12 green.

## Task 13.1 — SEO Models

FILE: Aero.CMS.Core/Seo/Models/SeoCheckResult.cs
```csharp
namespace Aero.CMS.Core.Seo.Models;

public enum SeoCheckStatus { Pass, Warning, Fail, Info }

public class SeoCheckResultItem
{
    public string CheckAlias { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public SeoCheckStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SeoCheckResult
{
    public List<SeoCheckResultItem> Items { get; set; } = [];
    public int Score => Items.Count == 0 ? 0 :
        (int)((double)Items.Count(x => x.Status == SeoCheckStatus.Pass) / Items.Count * 100);
}
```

## Task 13.2 — ISeoCheck + SeoCheckContext

FILE: Aero.CMS.Core/Seo/Interfaces/ISeoCheck.cs
```csharp
namespace Aero.CMS.Core.Seo.Interfaces;

public class SeoCheckContext
{
    public required ContentDocument Content { get; init; }
    public string? RenderedHtml { get; set; }
    public string? PublicUrl { get; set; }
}

public interface ISeoCheck
{
    string CheckAlias { get; }
    string DisplayName { get; }
    Task<SeoCheckResultItem> RunAsync(SeoCheckContext context, CancellationToken ct = default);
}
```

## Task 13.3 — Core SEO Checks

Create in Aero.CMS.Core/Seo/Checks/:

PageTitleSeoCheck.cs
  Pass: Properties["pageTitle"] exists and length 10-60
  Warning: length > 60 or < 10
  Fail: absent

MetaDescriptionSeoCheck.cs
  Pass: Properties["metaDescription"] exists and length 50-160
  Warning: > 160
  Fail: absent

WordCountSeoCheck.cs
  Pass: SearchText.Split(' ').Length >= 300
  Warning: < 300 words
  Fail: SearchText empty

HeadingOneSeoCheck.cs
  Pass: RenderedHtml contains exactly one <h1>
  Warning: multiple <h1> tags
  Fail: no <h1>
  Info: RenderedHtml is null

## Task 13.4 — SeoRedirectDocument

FILE: Aero.CMS.Core/Seo/Models/SeoRedirectDocument.cs
```csharp
namespace Aero.CMS.Core.Seo.Models;

public class SeoRedirectDocument : AuditableDocument
{
    public string FromSlug { get; set; } = string.Empty;
    public string ToSlug { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 301;
    public bool IsActive { get; set; } = true;
}
```

## Task 13.5 — SeoRedirectRepository

FILE: Aero.CMS.Core/Seo/Data/SeoRedirectRepository.cs
```csharp
public interface ISeoRedirectRepository : IRepository<SeoRedirectDocument>
{
    Task<SeoRedirectDocument?> FindByFromSlugAsync(string fromSlug, CancellationToken ct = default);
}
```

TEST: Aero.CMS.Tests.Unit/Seo/SeoCheckTests.cs
  PageTitleSeoCheck:
    - Pass for title 10-60 chars
    - Fail when absent
    - Warning for > 60 chars
    - Warning for < 10 chars
  MetaDescriptionSeoCheck:
    - Pass for 50-160 chars
    - Fail when absent
    - Warning when > 160 chars
  WordCountSeoCheck:
    - Pass for >= 300 words
    - Warning for < 300 words
    - Fail for empty SearchText
  HeadingOneSeoCheck:
    - Pass for exactly one <h1>
    - Fail for no <h1>
    - Warning for multiple <h1>
    - Info when RenderedHtml is null

PHASE 13 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Seo"
  dotnet test Aero.CMS.Tests.Integration
  Expected: All pass. Zero failures.

---

# PHASE 14 — Media Domain

Goal: MediaDocument, IMediaProvider, DiskStorageProvider.
Prerequisites: Phase 13 green.

## Task 14.1 — MediaDocument

FILE: Aero.CMS.Core/Media/Models/MediaDocument.cs
```csharp
namespace Aero.CMS.Core.Media.Models;

public enum MediaType { Image, Video, Document, Audio, Other }

public class MediaDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public MediaType MediaType { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public Guid? ParentFolderId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

## Task 14.2 — IMediaProvider

FILE: Aero.CMS.Core/Media/Interfaces/IMediaProvider.cs
```csharp
namespace Aero.CMS.Core.Media.Interfaces;

public record MediaUploadResult(bool Success, string StorageKey, string? Error = null);

public interface IMediaProvider
{
    string ProviderAlias { get; }
    Task<MediaUploadResult> UploadAsync(
        Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default);
    string GetPublicUrl(string storageKey);
}
```

## Task 14.3 — DiskStorageProvider

FILE: Aero.CMS.Core/Media/Providers/DiskStorageProvider.cs
```csharp
namespace Aero.CMS.Core.Media.Providers;

public class DiskStorageProvider(IWebHostEnvironment env) : IMediaProvider
{
    public string ProviderAlias => "disk";
    private string BasePath => Path.Combine(env.WebRootPath, "media");

    public async Task<MediaUploadResult> UploadAsync(
        Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        Directory.CreateDirectory(BasePath);
        var key = $"{Guid.NewGuid()}/{fileName}";
        var fullPath = Path.Combine(BasePath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fileStream = File.Create(fullPath);
        await stream.CopyToAsync(fileStream, ct);
        return new MediaUploadResult(true, key);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(BasePath,
            storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public string GetPublicUrl(string storageKey)
        => $"/media/{storageKey.Replace(Path.DirectorySeparatorChar, '/')}";
}
```

TEST: Aero.CMS.Tests.Unit/Media/DiskStorageProviderTests.cs
  - ProviderAlias == "disk"
  - UploadAsync returns success with non-empty StorageKey
  - UploadAsync creates file at expected path
  - DeleteAsync removes the file
  - DeleteAsync on non-existent key does not throw
  - GetPublicUrl starts with "/media/"
  - GetPublicUrl uses forward slashes on any OS

PHASE 14 GATE:
  dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Media"
  Expected: All pass. Zero failures.

---

# PHASE 15 — Plugin System

Goal: ICmsPlugin, AssemblyLoadContext loader.
Prerequisites: Phase 14 green.

## Task 15.1 — ICmsPlugin

FILE: Aero.CMS.Core/Plugins/Interfaces/ICmsPlugin.cs
```csharp
namespace Aero.CMS.Core.Plugins.Interfaces;

public interface ICmsPlugin
{
    string Alias { get; }
    string Version { get; }
    string DisplayName { get; }
    void ConfigureServices(IServiceCollection services);
    void ConfigureBlocks(IBlockRegistry registry);
}
```

## Task 15.2 — PluginLoader

FILE: Aero.CMS.Core/Plugins/PluginLoader.cs
```csharp
namespace Aero.CMS.Core.Plugins;

public class PluginLoader
{
    private readonly List<ICmsPlugin> _loaded = [];
    public IReadOnlyList<ICmsPlugin> LoadedPlugins => _loaded.AsReadOnly();

    public IReadOnlyList<ICmsPlugin> LoadFromDirectory(string path)
    {
        if (!Directory.Exists(path)) return [];

        foreach (var dll in Directory.GetFiles(path, "*.dll"))
        {
            try
            {
                var context = new PluginLoadContext(dll);
                var assembly = context.LoadFromAssemblyPath(dll);
                var plugins = assembly.GetTypes()
                    .Where(t => typeof(ICmsPlugin).IsAssignableFrom(t)
                                && !t.IsAbstract
                                && t.GetConstructor(Type.EmptyTypes) != null)
                    .Select(t => (ICmsPlugin)Activator.CreateInstance(t)!)
                    .ToList();
                _loaded.AddRange(plugins);
            }
            catch { /* log and continue */ }
        }
        return _loaded.AsReadOnly();
    }
}

internal class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }
}
```

TEST: Aero.CMS.Tests.Unit/Plugins/PluginLoaderTests.cs
  - LoadFromDirectory with non-existent path returns empty list without throwing
  - LoadFromDirectory with empty directory returns empty list
  - LoadedPlugins starts empty

PHASE 15 GATE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration
  Expected: Full suite green. (Phases 1-15)

---

# PHASE 16 — DI Composition Root

Goal: Full AddAeroCmsCore registration. Integration test validates.
Prerequisites: Phase 15 green.

## Task 16.1 — Complete ServiceExtensions

FILE: Aero.CMS.Core/Extensions/ServiceExtensions.cs

Replace the stub from Phase 2 with the full registration:

```csharp
public static IServiceCollection AddAeroCmsCore(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<GlobalSettings>(configuration.GetSection("AeroCms"));

    // Infrastructure
    services.AddSingleton<IDocumentStore>(sp => {
        var settings = sp.GetRequiredService<IOptions<GlobalSettings>>().Value;
        return DocumentStoreFactory.Create(settings.RavenDb);
    });
    services.AddSingleton<ISystemClock, SystemClock>();
    services.AddSingleton<IKeyVaultService, EnvironmentKeyVaultService>();
    services.AddSingleton<IBlockRegistry, BlockRegistry>();

    // Repositories
    services.AddScoped<IContentRepository, ContentRepository>();
    services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
    services.AddScoped<ISeoRedirectRepository, SeoRedirectRepository>();

    // Save hook pipeline
    services.AddScoped<SaveHookPipeline<ContentDocument>>();
    services.AddScoped<IBeforeSaveHook<ContentDocument>, ContentSearchIndexerHook>();

    // Search extraction
    services.AddSingleton<BlockTreeTextExtractor>();
    services.AddSingleton<IBlockTextExtractor, RichTextBlockExtractor>();
    services.AddSingleton<IBlockTextExtractor, MarkdownBlockExtractor>();
    services.AddSingleton<IBlockTextExtractor, ImageBlockExtractor>();
    services.AddSingleton<IBlockTextExtractor, HeroBlockExtractor>();
    services.AddSingleton<IBlockTextExtractor, QuoteBlockExtractor>();

    // Content services
    services.AddScoped<IPublishingWorkflow, PublishingWorkflow>();
    services.AddScoped<ContentFinderPipeline>();
    services.AddScoped<IContentFinder, DefaultContentFinder>();
    services.AddScoped<MarkdownRendererService>();
    services.AddScoped<MarkdownImportService>();

    // Rich text editor (swappable)
    services.AddSingleton<IRichTextEditor, NullRichTextEditor>();

    // Media
    services.AddScoped<IMediaProvider, DiskStorageProvider>();

    // SEO
    services.AddScoped<ISeoCheck, PageTitleSeoCheck>();
    services.AddScoped<ISeoCheck, MetaDescriptionSeoCheck>();
    services.AddScoped<ISeoCheck, WordCountSeoCheck>();
    services.AddScoped<ISeoCheck, HeadingOneSeoCheck>();

    // Identity
    services.AddScoped<IBanService, BanService>();

    // Plugins
    services.AddSingleton<PluginLoader>();

    return services;
}
```

TEST: Aero.CMS.Tests.Integration/Infrastructure/CompositionRootTests.cs
  Build a ServiceProvider using AddAeroCmsCore with test configuration
  (use RavenDB.TestDriver URL).

  Verify resolution of:
  - IDocumentStore
  - IContentRepository
  - IPublishingWorkflow
  - ContentFinderPipeline
  - BlockTreeTextExtractor
  - IBlockRegistry
  - MarkdownRendererService
  - IBanService
  - ISystemClock
  - IKeyVaultService
  - IEnumerable<ISeoCheck> has 4 items
  - IEnumerable<IBlockTextExtractor> has 5 items
  - IEnumerable<IContentFinder> has 1 item
  - SaveHookPipeline<ContentDocument> resolves without error
  - IEnumerable<IBeforeSaveHook<ContentDocument>> has 1 item

PHASE 16 GATE — FINAL FULL SUITE:
  dotnet test Aero.CMS.Tests.Unit
  dotnet test Aero.CMS.Tests.Integration

  Expected:
  - Zero failures
  - Zero errors
  - Generate coverage: dotnet test --collect:"XPlat Code Coverage"
  - Aero.CMS.Core line coverage MUST be >= 80%

---

# AGENT CHECKLIST — BEFORE MARKING ANY PHASE COMPLETE

- [ ] dotnet build Aero.CMS.sln — 0 errors, 0 warnings
- [ ] All tests in phase gate command pass
- [ ] No test skipped, ignored, or marked [Skip]
- [ ] No TODO, throw new NotImplementedException(), or "implement later"
- [ ] No unlisted NuGet package added
- [ ] All file paths exact as specified
- [ ] All interface signatures exact as specified
- [ ] NSubstitute used for ALL mocks
- [ ] Shouldly used for ALL assertions

---

# TEST NAMING CONVENTION

All test methods must follow:
  MethodName_Condition_ExpectedResult

Examples:
  GetBySlugAsync_WithValidSlug_ReturnsDocument
  GetBySlugAsync_WithUnknownSlug_ReturnsNull
  PublishAsync_WhenRequiresApprovalAndNotApproved_ReturnsFail
  ExtractAll_WithNestedDivBlock_TraversesAllChildren
  IsBannedAsync_WithExpiredTemporaryBan_ReturnsFalse

---

# COVERAGE TARGETS

| Area                         | Minimum |
|------------------------------|---------|
| Aero.CMS.Core/Shared         | 90%     |
| Aero.CMS.Core/Content        | 85%     |
| Aero.CMS.Core/Membership     | 85%     |
| Aero.CMS.Core/Seo            | 80%     |
| Aero.CMS.Core/Media          | 80%     |
| Aero.CMS.Core/Plugins        | 75%     |
| Aero.CMS.Core overall        | 80%     |
