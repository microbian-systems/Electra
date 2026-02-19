# C# Code Style Guide - Aero CMS

## General Principles

- No comments unless explicitly requested by user
- Clean, readable, self-documenting code
- Follow existing patterns in the codebase

## Naming Conventions

### Pascal Case
- Classes, records, structs
- Interfaces (with `I` prefix)
- Public properties and methods
- Constants

### camelCase
- Private fields
- Local variables
- Parameters

### Example
```csharp
public interface IContentRepository
{
    Task<ContentDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public class ContentRepository : IContentRepository
{
    private readonly IDocumentStore _store;
    
    public ContentRepository(IDocumentStore store)
    {
        _store = store;
    }
}
```

## File Organization

### File-scoped Namespaces
```csharp
namespace Aero.CMS.Core.Content.Models;

public class ContentDocument : AuditableDocument
{
    // ...
}
```

### One Type Per File
- File name matches type name
- Nested types in same file only if small and closely related

## Modern C# Features

### Primary Constructors (C# 12+)
```csharp
public class ContentRepository(IDocumentStore store) : IContentRepository
{
    public async Task<ContentDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var session = store.OpenAsyncSession();
        return await session.LoadAsync<ContentDocument>(id.ToString(), ct);
    }
}
```

### Collection Expressions
```csharp
public List<ContentBlock> Blocks { get; set; } = [];
public Dictionary<string, object> Properties { get; set; } = [];
```

### Required Properties
```csharp
public class ContentFinderContext
{
    public required string Slug { get; init; }
    public required HttpContext HttpContext { get; init; }
}
```

### Record Types for DTOs
```csharp
public record MediaUploadResult(bool Success, string StorageKey, string? Error = null);
```

## Dependency Injection

### Constructor Injection
```csharp
public class PublishingWorkflow(
    IContentRepository contentRepo,
    IContentTypeRepository contentTypeRepo,
    ISystemClock clock) : IPublishingWorkflow
{
    // ...
}
```

### Service Registration via Extension Methods
```csharp
public static IServiceCollection AddAeroCmsCore(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddScoped<IContentRepository, ContentRepository>();
    return services;
}
```

## Async Patterns

### Async Suffix
- All async methods must have `Async` suffix
- `GetByIdAsync`, `SaveAsync`, `DeleteAsync`

### CancellationToken
- All async methods should accept `CancellationToken ct = default`
- Pass cancellation token through call chain

### Dispose Pattern for Sessions
```csharp
using var session = Store.OpenAsyncSession();
// ... operations
await session.SaveChangesAsync(ct);
```

## Error Handling

### Result Types
- Use `HandlerResult` for operation outcomes
- Never throw exceptions for business logic failures
```csharp
public static HandlerResult Ok() => new() { Success = true };
public static HandlerResult Fail(string error) => new() { Success = false, Errors = [error] };
```

## Testing Conventions

### Test Naming
```csharp
[Fact]
public void GetByIdAsync_WithValidId_ReturnsDocument()
[Fact]
public void GetByIdAsync_WithUnknownId_ReturnsNull()
```

### Test Structure
- Arrange, Act, Assert pattern
- Use NSubstitute for mocks
- Use Shouldly for assertions
```csharp
[Fact]
public void SaveAsync_NewEntity_SetsCreatedAt()
{
    // Arrange
    var doc = new ContentDocument { Name = "Test" };
    
    // Act
    _repository.SaveAsync(doc, "user");
    
    // Assert
    doc.CreatedAt.ShouldNotBe(default);
}
```
