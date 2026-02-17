# Aero.Validators

FluentValidation-based validation for the Aero framework.

## Overview

`Aero.Validators` provides comprehensive validation support using FluentValidation. It includes validators for common scenarios, validation decorators for the command pattern, and integration with ASP.NET Core model validation.

## Key Components

### Base Validators

```csharp
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator()
    {
        // Common validation rules
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;
    }

    protected IRuleBuilderOptions<T, string> MustBeValidEmail(
        Expression<Func<T, string>> expression)
    {
        return RuleFor(expression)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }

    protected IRuleBuilderOptions<T, string> MustBeStrongPassword(
        Expression<Func<T, string>> expression)
    {
        return RuleFor(expression)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain a number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain a special character");
    }

    protected IRuleBuilderOptions<T, string> MustBeValidUrl(
        Expression<Func<T, string>> expression)
    {
        return RuleFor(expression)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(expression.Compile()(x)))
            .WithMessage("Invalid URL format");
    }
}
```

### Entity Validators

```csharp
public class CreateProductRequestValidator : BaseValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero")
            .LessThan(1000000).WithMessage("Price must be less than 1,000,000");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required")
            .Matches(@"^[A-Z0-9-]+$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens");
    }
}

public class UpdateUserRequestValidator : BaseValidator<UpdateUserRequest>
{
    private readonly IAeroUserRepository _userRepository;

    public UpdateUserRequestValidator(IAeroUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores")
            .MustAsync(BeUniqueUsername).WithMessage("Username is already taken");

        MustBeValidEmail(x => x.Email)
            .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format");
    }

    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.FindByUsernameAsync(username);
        return existing == null;
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.FindByEmailAsync(email);
        return existing == null;
    }
}
```

### Validation Decorators

```csharp
public class ValidationCommandHandlerDecorator<TCommand, TResult> : IAsyncCommand<TCommand, TResult>
{
    private readonly IAsyncCommand<TCommand, TResult> _inner;
    private readonly IValidator<TCommand> _validator;
    private readonly ILogger<ValidationCommandHandlerDecorator<TCommand, TResult>> _logger;

    public ValidationCommandHandlerDecorator(
        IAsyncCommand<TCommand, TResult> inner,
        IValidator<TCommand> validator,
        ILogger<ValidationCommandHandlerDecorator<TCommand, TResult>> logger)
    {
        _inner = inner;
        _validator = validator;
        _logger = logger;
    }

    public async Task<TResult> ExecuteAsync(TCommand command)
    {
        var validationResult = await _validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();

            _logger.LogWarning("Validation failed for {CommandType}: {Errors}",
                typeof(TCommand).Name,
                string.Join(", ", errors.Select(e => $"{e.Property}: {e.Message}")));

            throw new ValidationException(validationResult.Errors);
        }

        return await _inner.ExecuteAsync(command);
    }
}

public class ValidationError
{
    public string Property { get; set; }
    public string Message { get; set; }

    public ValidationError(string property, string message)
    {
        Property = property;
        Message = message;
    }
}

public class ValidationException : Exception
{
    public List<ValidationFailure> Errors { get; }

    public ValidationException(List<ValidationFailure> errors) 
        : base("Validation failed")
    {
        Errors = errors;
    }
}
```

### Validation Pipeline Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

## ASP.NET Core Integration

### Automatic Registration

```csharp
public static class ValidationExtensions
{
    public static IServiceCollection AddAeroValidators(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();

        // Register all validators from assembly
        services.AddValidatorsFromAssemblyContaining<Program>();

        // Add validation behavior for MediatR/commands
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
```

### Validation Filter

```csharp
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

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails
            {
                Title = "Validation Failed",
                Status = 400,
                Errors = errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value ?? Array.Empty<string>())
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

### Global Exception Handler

```csharp
public class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        var problemDetails = new ValidationProblemDetails
        {
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest,
            Errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray())
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
```

## Usage Examples

### Validating Commands

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item")
            .Must(items => items.Count <= 100)
            .WithMessage("Order cannot contain more than 100 items");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required")
            .SetValidator(new AddressValidator());
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty().Matches(@"^\d{5}(-\d{4})?$");
        RuleFor(x => x.Country).NotEmpty().Length(2);
    }
}
```

### Conditional Validation

```csharp
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .When(x => x.Type == ProductType.Physical);

        RuleFor(x => x.DownloadUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .When(x => x.Type == ProductType.Digital);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Type == ProductType.Physical);
    }

    private bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
```

### Custom Validators

```csharp
public class CreditCardValidator : PropertyValidator<string, string>
{
    public override string Name => "CreditCardValidator";

    public override bool IsValid(ValidationContext<string> context, string value)
    {
        // Luhn algorithm implementation
        var sum = 0;
        var alternate = false;

        for (var i = value.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(value[i]))
                return false;

            var n = int.Parse(value[i].ToString());
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    protected override string GetDefaultMessageTemplate(string errorCode)
    {
        return "{PropertyName} is not a valid credit card number";
    }
}

// Usage
RuleFor(x => x.CreditCardNumber).SetValidator(new CreditCardValidator());
```

## Configuration

```csharp
// Program.cs
builder.Services.AddAeroValidators();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.AddJsonOptions(options =>
{
    // Don't include null values in validation errors
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add exception handler
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();
```

## Best Practices

1. **Separate Validators** - One validator per request/command
2. **Use Async Validation** - For database checks, use MustAsync
3. **Cascade Mode** - Use Stop to fail fast on first error
4. **Custom Messages** - Provide clear, user-friendly error messages
5. **Reusable Rules** - Create extension methods for common patterns
6. **Test Validators** - Unit test validators with many scenarios

## Related Packages

- `Aero.Common` - Validation decorators
- `Aero.Web` - Validation filters
- `Aero.Web.Core` - Exception handling
