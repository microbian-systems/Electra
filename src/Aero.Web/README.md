# Aero.Web

Web framework extensions, middleware, and utilities for building ASP.NET Core applications with the Aero framework.

## Overview

`Aero.Web` provides a comprehensive set of extensions, middleware, controllers, and utilities for building ASP.NET Core web applications. It integrates with the Aero core patterns and provides out-of-the-box solutions for common web development needs.

## Key Components

### Middleware

#### AeroDefaults Middleware

```csharp
public static class WebApplicationExtensions
{
    public static WebApplication UseAeroDefaults(this WebApplication app)
    {
        // Security headers
        app.UseSecurityHeaders();
        
        // Error handling
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        
        // HTTPS redirection
        app.UseHttpsRedirection();
        
        // Static files
        app.UseStaticFiles();
        
        // Routing
        app.UseRouting();
        
        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();
        
        // Session
        app.UseSession();
        
        // MiniProfiler (in development)
        if (app.Environment.IsDevelopment())
        {
            app.UseMiniProfiler();
        }
        
        return app;
    }
}
```

#### Security Headers Middleware

```csharp
app.UseSecurityHeaders(policy =>
{
    policy.AddDefaultSecurityHeaders();
    policy.AddContentSecurityPolicy(builder =>
    {
        builder.AddDefaultSrc().Self();
        builder.AddScriptSrc().Self().UnsafeInline();
        builder.AddStyleSrc().Self().UnsafeInline();
    });
    policy.AddStrictTransportSecurityMaxAgeIncludeSubDomains();
});
```

### Controllers

#### Base Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public abstract class AeroApiController : ControllerBase
{
    protected readonly ILogger Logger;

    protected AeroApiController(ILogger logger)
    {
        Logger = logger;
    }

    protected ActionResult<T> OkOrNotFound<T>(T? value)
    {
        if (value == null)
            return NotFound();
        return Ok(value);
    }

    protected IActionResult Error(string message, int statusCode = 500)
    {
        return StatusCode(statusCode, new { Error = message });
    }
}
```

#### CRUD Controller

```csharp
public abstract class CrudController<T, TDto> : AeroApiController
    where T : Entity, new()
{
    protected readonly IGenericRepository<T> Repository;

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<T>>> GetAll()
    {
        var items = await Repository.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<T>> GetById(string id)
    {
        var item = await Repository.FindByIdAsync(id);
        return OkOrNotFound(item);
    }

    [HttpPost]
    public virtual async Task<ActionResult<T>> Create([FromBody] TDto dto)
    {
        var entity = dto.MapTo<T>();
        var created = await Repository.InsertAsync(entity);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public virtual async Task<ActionResult<T>> Update(string id, [FromBody] TDto dto)
    {
        var existing = await Repository.FindByIdAsync(id);
        if (existing == null)
            return NotFound();

        dto.MapTo(existing);
        var updated = await Repository.UpdateAsync(existing);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(string id)
    {
        await Repository.DeleteAsync(id);
        return NoContent();
    }
}
```

### Authentication

#### JWT Authentication

```csharp
public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration config)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]))
                };
            });

        return services;
    }
}
```

#### Current User Service

```csharp
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?
        .FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?
        .Identity?.IsAuthenticated ?? false;

    // ... other properties
}
```

### API Versioning

```csharp
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddAeroApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new MediaTypeApiVersionReader());
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
```

### OpenAPI / Swagger

```csharp
public static class OpenApiExtensions
{
    public static IServiceCollection AddAeroOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Aero API",
                Version = "v1",
                Description = "Aero Framework API"
            });

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseAeroSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Aero API V1");
            options.RoutePrefix = "swagger";
        });

        // Add Scalar API reference
        app.MapScalarApiReference();

        return app;
    }
}
```

### Model Binding & Validation

```csharp
// Custom model binder
public class EntityModelBinder<T> : IModelBinder where T : Entity
{
    private readonly IGenericRepository<T> _repository;

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var id = bindingContext.ValueProvider.GetValue("id").FirstValue;
        if (string.IsNullOrEmpty(id))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var entity = await _repository.FindByIdAsync(id);
        if (entity == null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        bindingContext.Result = ModelBindingResult.Success(entity);
    }
}

// Validation filter
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray());

            context.Result = new BadRequestObjectResult(new { Errors = errors });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

### Response Formatting

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public List<string>? ValidationErrors { get; set; }
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

// Result filter
public class ApiResponseFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            if (objectResult.Value is not ApiResponse<object>)
            {
                context.Result = new ObjectResult(
                    ApiResponse<object>.SuccessResponse(objectResult.Value))
                {
                    StatusCode = objectResult.StatusCode
                };
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
```

## Configuration

### Default API Setup

```csharp
public static class ServiceExtensions
{
    public static IServiceCollection AddDefaultApi(
        this IServiceCollection services, 
        IConfiguration config)
    {
        // Controllers
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<ApiResponseFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

        // API versioning
        services.AddAeroApiVersioning();

        // OpenAPI
        services.AddAeroOpenApi();

        // JWT Auth
        services.AddJwtAuthentication(config);

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("Default", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // HttpContext accessor
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
```

## Best Practices

1. **Use Base Controllers** - Inherit from AeroApiController for consistency
2. **Apply Filters** - Use ValidationFilter and ApiResponseFilter globally
3. **Version APIs** - Always version your APIs from the start
4. **Secure by Default** - Require authentication unless explicitly public
5. **Use DTOs** - Don't expose domain entities directly
6. **Document APIs** - Use OpenAPI/Swagger with XML comments

## Related Packages

- `Aero.Web.Core` - Core web abstractions
- `Aero.Auth` - Authentication implementation
- `Aero.Validators` - Validation filters
- `Aero.Caching` - Response caching
