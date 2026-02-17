# Aero.Core

Core domain entities, algorithms, and foundational components for the Aero web application platform.

## Overview

`Aero.Core` is the foundational package containing essential domain abstractions, entity definitions, cryptographic utilities, and core algorithms used throughout the Aero framework. It has minimal external dependencies and serves as the base layer for all other Aero packages.

## Key Components

### Entities

Base entity definitions with audit trail support:

```csharp
// String-keyed entity (default)
public class Product : Entity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Custom key type
public class Order : Entity<Guid>
{
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}
```

All entities automatically include:
- `Id` - Primary key
- `CreatedOn` - Creation timestamp
- `ModifiedOn` - Last modification timestamp
- `CreatedBy` - Creator identifier
- `ModifiedBy` - Last modifier identifier

### Repository Pattern

Abstract base classes for implementing repositories:

```csharp
public abstract class GenericRepository<T, TKey> : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    // Provides synchronous wrappers around async methods
    // Abstract methods for database-specific implementations
}
```

### Cryptographic Utilities

- **Shamir's Secret Sharing** - Secure secret distribution using SecretSharingDotNet
- **Encryption Extensions** - Utility methods for common encryption operations

### Algorithms

- Data structure implementations
- Tree structures and traversal algorithms
- Collection utilities

## Interfaces

### IEntity<TKey>

The base interface for all persistable entities:

```csharp
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; set; }
    DateTimeOffset CreatedOn { get; set; }
    DateTimeOffset? ModifiedOn { get; set; }
    string CreatedBy { get; set; }
    string ModifiedBy { get; set; }
}
```

### Repository Interfaces

```csharp
// Read-only operations
public interface IReadOnlyRepository<T, TKey>
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> FindByIdAsync(TKey id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}

// Write operations
public interface IWriteOnlyRepository<T, TKey>
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    Task<T> InsertAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<T> UpsertAsync(T entity);
    Task DeleteAsync(TKey id);
    Task DeleteAsync(T entity);
}

// Combined interface
public interface IGenericRepository<T, TKey> 
    : IReadOnlyRepository<T, TKey>, IWriteOnlyRepository<T, TKey>
    where T : IEntity<TKey>, new() 
    where TKey : IEquatable<TKey>
{
}
```

## Dependencies

| Package | Purpose |
|---------|---------|
| LanguageExt.Core | Functional programming primitives |
| Microsoft.Extensions.Hosting.Abstractions | Hosting abstractions |
| Microsoft.Extensions.Logging | Logging abstractions |
| SecretSharingDotNet | Shamir's Secret Sharing implementation |
| Serilog | Structured logging |
| SnowflakeGuid | Distributed ID generation |
| System.Text.Json | JSON serialization |
| ThrowGuard | Guard clause utilities |
| WindowsAzure.Storage | Azure Storage integration |
| Microsoft.Extensions.Configuration | Configuration abstractions |
| Microsoft.Extensions.Identity.Stores | Identity store interfaces |

## Usage

### Creating a Custom Entity

```csharp
using Aero.Core.Entities;

public class Customer : Entity
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    
    // Custom business logic
    public string FullName => $"{FirstName} {LastName}";
    public int Age => DateTime.Now.Year - DateOfBirth.Year;
}
```

### Implementing a Repository

```csharp
public class CustomerRepository : GenericRepository<Customer, string>
{
    public CustomerRepository(ILogger<CustomerRepository> log) : base(log) { }

    public override async Task<IEnumerable<Customer>> GetAllAsync()
    {
        // Database-specific implementation
    }

    public override async Task<Customer> FindByIdAsync(string id)
    {
        // Database-specific implementation
    }

    // ... implement other abstract methods
}
```

## Best Practices

1. **Always inherit from Entity or Entity<TKey>** - Ensures consistent audit fields
2. **Use async methods** - All repository operations should be async
3. **Implement IEquatable<TKey>** - Required for entity key types
4. **Keep entities POCO** - Avoid business logic in entities, use domain services

## Related Packages

- `Aero.Persistence.Core` - Repository interfaces and abstractions
- `Aero.EfCore` - Entity Framework implementation
- `Aero.RavenDB` - RavenDB implementation
- `Aero.Marten` - Marten implementation
