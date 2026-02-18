# C# Code Styleguide for Aero

## General Principles

- **Readability first.** Code is read far more often than written.
- **Descriptive & Explicit.** Names should immediately communicate purpose.
- **Clean Architecture.** Follow separation of concerns religiously.
- **Async throughout.** All I/O operations should be asynchronous.

---

## Naming Conventions

### PascalCase

Use for:
- Namespaces
- Classes
- Records
- Structs
- Interfaces (with `I` prefix)
- Public properties
- Public methods
- Constants

```csharp
public class UserRepository : IRepository<User>
{
    public const int MaxPageSize = 100;
    public string ConnectionString { get; set; }
    public async Task<User> GetByIdAsync(Guid id) { }
}
```

### camelCase

Use for:
- Private fields (with `_` prefix)
- Local variables
- Parameters

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User { Name = request.Name };
        return await _userRepository.InsertAsync(user);
    }
}
```

### Avoid Abbreviations

```csharp
// Bad
public class AuthSvc { }
private readonly IRepo<T> _repo;

// Good
public class AuthenticationService { }
private readonly IRepository<T> _repository;
```

---

## File Organization

### One Type Per File

Each public class, record, interface, or enum should be in its own file.

### File Naming

File name must match the type name exactly.

```
User.cs              -> public class User
IUserRepository.cs   -> public interface IUserRepository
CreateUserCommand.cs -> public class CreateUserCommand
```

### File Header (Optional)

```csharp
// ReSharper disable once CheckNamespace
namespace Aero.Services.Users;
```

---

## Code Structure

### Using Statements

- Place at top of file
- System namespaces first
- Third-party namespaces second
- Project namespaces last
- Use file-scoped namespaces (C# 10+)

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;

namespace Aero.Services.Users;

public class UserService
{
}
```

### Member Ordering

Within a class, order members as follows:

1. Constants
2. Static fields
3. Instance fields
4. Constructors
5. Properties
6. Public methods
7. Protected methods
8. Private methods
9. Event handlers

```csharp
public class UserService
{
    private const int DefaultPageSize = 20;
    private static readonly DateTime Epoch = new(1970, 1, 1);
    
    private readonly IUserRepository _userRepository;
    
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public string ServiceName { get; } = "UserService";
    
    public async Task<User> GetByIdAsync(Guid id) { }
    
    protected virtual void OnUserCreated(User user) { }
    
    private bool ValidateUser(User user) { }
}
```

---

## Async/Await

### All I/O Must Be Async

```csharp
// Bad
public User GetUser(Guid id) 
    => _userRepository.Find(id);

// Good
public async Task<User> GetUserAsync(Guid id) 
    => await _userRepository.FindAsync(id);
```

### Use Async Suffix

All async methods should end with `Async`.

```csharp
public async Task<User> CreateUserAsync(CreateUserRequest request);
public async Task<bool> DeleteUserAsync(Guid id);
```

### ConfigureAwait

In library code, use `ConfigureAwait(false)`:

```csharp
public async Task<User> GetUserAsync(Guid id)
{
    return await _userRepository.FindAsync(id).ConfigureAwait(false);
}
```

In application code (ASP.NET Core), it's optional but recommended.

---

## Nullable Reference Types

Enable nullable reference types in all projects.

```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

### Annotate Appropriately

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }  // Can be null
    public IReadOnlyList<Role> Roles { get; set; } = Array.Empty<Role>();
}
```

### Guard Against Null

```csharp
public async Task<User> GetUserAsync(Guid id)
{
    var user = await _userRepository.FindAsync(id)
        ?? throw new NotFoundException($"User with id {id} not found");
    
    return user;
}
```

---

## Records and Primary Constructors

### Prefer Records for DTOs

```csharp
// Good - immutable DTO
public record CreateUserRequest(string Name, string Email);

// Good - record with additional properties
public record UserDto(Guid Id, string Name)
{
    public string? Email { get; init; }
}
```

### Use Primary Constructors (C# 12+)

```csharp
public class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger)
{
    public async Task<User> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Getting user {UserId}", id);
        return await userRepository.FindAsync(id);
    }
}
```

---

## Pattern Matching

### Use Modern Pattern Matching

```csharp
// Good
public string GetStatusMessage(UserStatus status) => status switch
{
    UserStatus.Active => "User is active",
    UserStatus.Inactive => "User is inactive",
    UserStatus.Suspended => "User is suspended",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};

// Property patterns
public decimal CalculateDiscount(Order order) => order switch
{
    { Total: > 1000 } => 0.10m,
    { Total: > 500 } => 0.05m,
    { Customer.IsPremium: true } => 0.15m,
    _ => 0m
};
```

---

## Dependency Injection

### Constructor Injection

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly ICacheService _cache;

    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _logger = logger;
        _cache = cache;
    }
}
```

### Extension Methods for DI Registration

```csharp
public static class UserServiceExtensions
{
    public static IServiceCollection AddUserServices(
        this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
```

---

## Commands and Queries (CQS)

### Command Pattern

```csharp
public interface ICommand<TRequest, TResult>
{
    Task<TResult> ExecuteAsync(TRequest request);
}

public class CreateUserCommand : ICommand<CreateUserRequest, User>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommand(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> ExecuteAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email
        };

        return await _userRepository.InsertAsync(user);
    }
}
```

### Query Pattern

```csharp
public interface IQueryHandler<TResult>
{
    Task<TResult> HandleAsync();
}

public interface IQueryHandler<TParam, TResult>
{
    Task<TResult> HandleAsync(TParam parameter);
}

public class GetUserByIdQuery : IQueryHandler<Guid, User?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQuery(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> HandleAsync(Guid userId)
    {
        return await _userRepository.FindAsync(userId);
    }
}
```

---

## Repository Pattern

### Generic Repository Interface

```csharp
public interface IRepository<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
    Task<T?> FindAsync(TKey id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> InsertAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(TKey id);
}
```

### Entity Base Class

```csharp
public abstract class Entity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
```

---

## Error Handling

### Use Specific Exception Types

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = new ReadOnlyDictionary<string, string[]>(errors);
    }
}
```

### Throw Expressions

```csharp
public async Task<User> GetUserAsync(Guid id)
{
    var user = await _userRepository.FindAsync(id)
        ?? throw new NotFoundException($"User {id} not found");

    return user;
}
```

---

## Logging

### Use ILogger with Structured Logging

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Creating user with email {Email}", request.Email);
        
        var user = await _userRepository.InsertAsync(new User { ... });
        
        _logger.LogInformation("User {UserId} created successfully", user.Id);
        
        return user;
    }
}
```

### Log Levels

- **Trace** - Detailed diagnostic information
- **Debug** - Debugging information
- **Information** - General flow information
- **Warning** - Unexpected but handled situations
- **Error** - Errors that don't stop execution
- **Critical** - Fatal errors

---

## Testing

### Test Naming

```
MethodName_Scenario_ExpectedResult
```

```csharp
[Fact]
public async Task CreateUserAsync_WithValidRequest_ReturnsUser()
{
    // Arrange
    var request = new CreateUserRequest("John Doe", "john@example.com");
    
    // Act
    var result = await _service.CreateUserAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("John Doe", result.Name);
}
```

### Use Bogus for Test Data

```csharp
private readonly Faker<CreateUserRequest> _requestFaker = new Faker<CreateUserRequest>()
    .RuleFor(x => x.Name, f => f.Name.FullName())
    .RuleFor(x => x.Email, f => f.Internet.Email());

[Fact]
public async Task CreateUserAsync_WithValidRequest_ReturnsUser()
{
    var request = _requestFaker.Generate();
    // ...
}
```

---

## Comments

### When to Comment

- **Why**, not **what** (code should explain what)
- Complex algorithms or business rules
- Public API documentation (XML comments)
- Workarounds or hacks with TODO/FIXME

```csharp
// BAD - explains what (obvious from code)
// Increment the counter
counter++;

// GOOD - explains why
// RavenDB requires IDs to be prefixed with collection name
var documentId = $"users/{Guid.NewGuid()}";

/// <summary>
/// Creates a new user in the system.
/// </summary>
/// <param name="request">The user creation request.</param>
/// <returns>The created user with assigned ID.</returns>
public async Task<User> CreateUserAsync(CreateUserRequest request)
```

---

## Code Quality

### Enable Analyzers

```xml
<PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### ReSharper/Rider Annotations

Use annotations for better static analysis:

```csharp
[NotNull]
public async Task<User> GetUserAsync([NotNull] Guid id)
{
    // ...
}
```

---

## Summary

| Category | Standard |
|----------|----------|
| Naming | PascalCase (public), camelCase (private), descriptive |
| Async | Async suffix, ConfigureAwait(false) in libs |
| Nullability | Enabled, explicit annotations |
| Records | Use for DTOs and immutable data |
| DI | Constructor injection, extension methods |
| Logging | Structured, use placeholders |
| Testing | MethodName_Scenario_ExpectedResult |
