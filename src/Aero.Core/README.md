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
----------


## Overview
Command/Query patterns, decorator implementations, functional programming utilities, and shared components for the Aero framework.
### 

These classes and interfaces provide the behavioral patterns and cross-cutting infrastructure that power the Aero framework's architecture. It implements Command Query Separation (CQS), decorator patterns for cross-cutting concerns, and functional programming primitives.

## Key Components

### Command Pattern

The command pattern provides a structured way to encapsulate operations:

```csharp
// Simple command
public interface IAsyncCommand
{
    Task ExecuteAsync();
}

// Command with parameter
public interface IAsyncCommand<in T>
{
    Task ExecuteAsync(T parameter);
}

// Command with parameter and return value
public interface IAsyncCommand<in T, TReturn>
{
    Task<TReturn> ExecuteAsync(T parameter);
}
```

### Base Command Handlers

```csharp
// Simple async handler
public abstract class AbstractAsyncCommandHandler : IAsyncCommand
{
    public abstract Task ExecuteAsync();
}

// Handler with parameter
public abstract class AbstractAsyncCommandHandler<T> : IAsyncCommand<T>
{
    public abstract Task ExecuteAsync(T param);
}

// Handler with parameter and return
public abstract class AbstractAsyncCommandHandler<T, TReturn> : IAsyncCommand<T, TReturn>
{
    public abstract Task<TReturn> ExecuteAsync(T param);
}
```

### Decorators

Decorators add cross-cutting concerns without modifying business logic:

#### Logging Decorator

```csharp
public class LoggingCommandDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    private readonly ILogger log;
    private readonly IAsyncCommand<TCommand, TReturn> decorated;

    public async Task<TReturn> ExecuteAsync(TCommand param)
    {
        var type = decorated.GetType();
        log.LogInformation($"Starting Execute on {type}");
        var result = await decorated.ExecuteAsync(param);
        log.LogInformation($"Finished Execute() on {type}");
        return result;
    }
}
```

#### Timing Decorator

```csharp
public class TimingCommandDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    public async Task<TReturn> ExecuteAsync(TCommand param)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await decorated.ExecuteAsync(param);
        stopwatch.Stop();
        log.LogInformation($"{type} took {stopwatch.ElapsedMilliseconds}ms");
        return result;
    }
}
```

#### Retry Decorator

```csharp
public class RetryCommandHandlerDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    // Implements retry logic with exponential backoff
    // Configurable retry count and delay
}
```

#### Exception Handling Decorator

```csharp
public class ExceptionCommandHandlerDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    // Wraps execution in try-catch
    // Provides structured error handling
}
```

#### CPU-Bound Decorator

```csharp
public class CpuBoundCommandHandlerDecorator<TCommand, TReturn> : IAsyncCommand<TCommand, TReturn>
{
    // Executes on ThreadPool for CPU-intensive operations
    // Prevents blocking of async context
}
```

### Functional Programming

#### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public string Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

#### Validation Pipeline

```csharp
public class ValidationPipeline<T>
{
    private readonly List<Func<T, ValidationOutcome>> _validators = new();
    
    public ValidationPipeline<T> AddValidator(Func<T, ValidationOutcome> validator)
    {
        _validators.Add(validator);
        return this;
    }
    
    public ValidationOutcome Validate(T input)
    {
        foreach (var validator in _validators)
        {
            var outcome = validator(input);
            if (!outcome.IsValid)
                return outcome;
        }
        return ValidationOutcome.Success();
    }
}
```

### Extension Methods

Aero.Common provides extensive extension methods:

#### String Extensions
```csharp
// Null/empty checks with meaning
if (name.IsNullOrWhiteSpace()) { }

// Conversion helpers
var guid = "abc".ToGuid();
var bytes = "text".ToByteArray();

// Formatting
var slug = "Hello World".ToSlug();
var camelCase = "hello world".ToCamelCase();
```

#### Object Extensions
```csharp
// Deep cloning
var clone = original.DeepClone();

// Property mapping
var dto = entity.MapTo<CustomerDto>();

// JSON operations
var json = obj.ToJson();
var obj = json.FromJson<MyType>();
```

#### Exception Extensions
```csharp
try
{
    // operation
}
catch (Exception ex)
{
    var fullMessage = ex.GetFullMessage();
    var stackTrace = ex.GetFullStackTrace();
}
```

### Utility Classes

#### AzureBlobStorageClient

```csharp
public class AzureBlobStorageClient : IBlobStorageClient
{
    public Task UploadAsync(string container, string blobName, Stream content);
    public Task<Stream> DownloadAsync(string container, string blobName);
    public Task DeleteAsync(string container, string blobName);
    public Task<bool> ExistsAsync(string container, string blobName);
}
```

#### ExternalProcess

```csharp
public class ExternalProcess
{
    public static Task<ProcessResult> RunAsync(string command, string arguments);
    public static Task<ProcessResult> RunAsync(string command, string arguments, TimeSpan timeout);
}
```

#### DynamicSearchQuery

```csharp
public class DynamicSearchQuery
{
    public string SearchTerm { get; set; }
    public List<FilterCriteria> Filters { get; set; }
    public List<SortCriteria> Sorting { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

## Usage Examples

### Creating a Command

```csharp
public class CreateOrderCommand : AbstractAsyncCommandHandler<CreateOrderRequest, Order>
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Product> _productRepository;

    public CreateOrderCommand(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Product> productRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public override async Task<Order> ExecuteAsync(CreateOrderRequest request)
    {
        // Validate products exist
        foreach (var item in request.Items)
        {
            var product = await _productRepository.FindByIdAsync(item.ProductId);
            if (product == null)
                throw new NotFoundException($"Product {item.ProductId} not found");
        }

        // Create order
        var order = new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items,
            Total = request.Items.Sum(i => i.Quantity * i.UnitPrice),
            OrderDate = DateTime.UtcNow
        };

        return await _orderRepository.InsertAsync(order);
    }
}
```

### Registering Decorators

```csharp
// In Program.cs or Startup.cs
services.AddScoped<IAsyncCommand<CreateOrderRequest, Order>, CreateOrderCommand>();

// Add decorators (order matters!)
services.Decorate<IAsyncCommand<CreateOrderRequest, Order>, 
    ValidationCommandHandlerDecorator<CreateOrderRequest, Order>>();
services.Decorate<IAsyncCommand<CreateOrderRequest, Order>, 
    LoggingCommandHandlerDecorator<CreateOrderRequest, Order>>();
services.Decorate<IAsyncCommand<CreateOrderRequest, Order>, 
    TimingCommandHandlerDecorator<CreateOrderRequest, Order>>();
services.Decorate<IAsyncCommand<CreateOrderRequest, Order>, 
    ExceptionCommandHandlerDecorator<CreateOrderRequest, Order>>();
```

### Using Result Pattern

```csharp
public class UpdateInventoryCommand : IAsyncCommand<UpdateInventoryRequest, Result<Inventory>>
{
    public async Task<Result<Inventory>> ExecuteAsync(UpdateInventoryRequest request)
    {
        var inventory = await _repository.FindByIdAsync(request.ProductId);
        if (inventory == null)
            return Result<Inventory>.Failure($"Product {request.ProductId} not found");

        if (inventory.Quantity + request.QuantityChange < 0)
            return Result<Inventory>.Failure("Insufficient inventory");

        inventory.Quantity += request.QuantityChange;
        await _repository.UpdateAsync(inventory);

        return Result<Inventory>.Success(inventory);
    }
}

// Usage
var result = await _command.ExecuteAsync(request);
if (result.IsSuccess)
    return Ok(result.Value);
else
    return BadRequest(result.Error);
```

## Best Practices

1. **Use Commands for Business Operations** - Encapsulate business logic in commands
2. **Apply Decorators Judiciously** - Don't over-decorate; consider performance impact
3. **Order Decorators Carefully** - Logging should typically be outermost
4. **Use Result Pattern for Expected Failures** - Don't throw exceptions for business rule violations
5. **Keep Commands Focused** - Single Responsibility Principle
1. **Always inherit from Entity or Entity<TKey>** - Ensures consistent audit fields
2. **Use async methods** - All repository operations should be async
3. **Implement IEquatable<TKey>** - Required for entity key types
4. **Keep entities POCO** - Avoid business logic in entities, use domain services

## Related Packages

- `Aero.Validators` - Validation decorators and rules
- `Aero.Persistence` - Repositories used by commands
- `Aero.Persistence.Core` - Repository interfaces and abstractions
- `Aero.EfCore` - Entity Framework implementation
- `Aero.RavenDB` - RavenDB implementation
- `Aero.Marten` - Marten implementation
