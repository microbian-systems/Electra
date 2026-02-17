# Aero.Models

Data Transfer Objects (DTOs), View Models, and API Request/Response models for the Aero framework.

## Overview

`Aero.Models` contains all the data contracts used for communication between layers and across API boundaries. It separates internal domain models from external-facing data contracts.

## Key Components

### Base DTO Classes

```csharp
public abstract class BaseDto
{
    public string Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
}

public abstract class CreateRequest
{
    // Base class for creation requests
}

public abstract class UpdateRequest
{
    public string Id { get; set; }
}

public abstract class ListResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore => (Page * PageSize) < TotalCount;
}
```

### Entity DTOs

```csharp
public class ProductDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Sku { get; set; }
    public string CategoryId { get; set; }
    public string CategoryName { get; set; }
    public bool IsActive { get; set; }
    public int StockQuantity { get; set; }
    public List<ProductImageDto> Images { get; set; }
}

public class ProductImageDto
{
    public string Url { get; set; }
    public string AltText { get; set; }
    public bool IsPrimary { get; set; }
}
```

### Request Models

```csharp
public class CreateProductRequest : CreateRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    [Required]
    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required]
    public string CategoryId { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}

public class UpdateProductRequest : UpdateRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0.01, 1000000)]
    public decimal? Price { get; set; }

    public bool? IsActive { get; set; }
}
```

### Response Models

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
    public List<string> ValidationErrors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ApiResponse<T> ErrorResponse(string error) => new()
    {
        Success = false,
        Error = error
    };
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

### Auth Models

```csharp
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public UserDto User { get; set; }
}

public class UserDto : BaseDto
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
}
```

### Search & Filter Models

```csharp
public class SearchRequest
{
    public string? Query { get; set; }
    public List<FilterCriteria> Filters { get; set; } = new();
    public List<SortCriteria> Sort { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class FilterCriteria
{
    public string Field { get; set; }
    public string Operator { get; set; } // eq, neq, gt, lt, contains, etc.
    public string Value { get; set; }
}

public class SortCriteria
{
    public string Field { get; set; }
    public bool Descending { get; set; }
}
```

## Mapping Configuration

```csharp
public static class MappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<Product, ProductDto>
            .NewConfig()
            .Map(dest => dest.CategoryName, src => src.Category.Name);

        TypeAdapterConfig<CreateProductRequest, Product>
            .NewConfig()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedOn)
            .Ignore(dest => dest.ModifiedOn);
    }
}
```

## Related Packages

- `Aero.Core` - Entity definitions to map from/to
- `Aero.Web` - API controllers that use these models
- `Aero.Validators` - Validation for request models
