# Aero.Web.Core

Core web abstractions and utilities for the Aero framework.

## Overview

`Aero.Web.Core` provides foundational web abstractions, extension methods, and utilities that are shared across the Aero web stack. It contains lightweight interfaces and base classes without heavy framework dependencies.

## Key Components

### HTTP Abstractions

#### HttpClient Extensions

```csharp
public static class HttpClientExtensions
{
    public static async Task<T?> GetJsonAsync<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public static async Task<HttpResponseMessage> PostJsonAsync<T>(
        this HttpClient client, string url, T data)
    {
        return await client.PostAsJsonAsync(url, data);
    }

    public static async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        this HttpClient client, string url, TRequest data)
    {
        var response = await client.PostAsJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }
}
```

#### HttpContext Extensions

```csharp
public static class HttpContextExtensions
{
    public static string? GetUserId(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetUserEmail(this HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated ?? false;
    }

    public static string GetClientIpAddress(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static string GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers["User-Agent"].ToString();
    }
}
```

### URL & Routing Utilities

```csharp
public static class UrlUtilities
{
    public static string Slugify(this string text)
    {
        return text.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace("--", "-");
    }

    public static string ToAbsoluteUrl(this string relativeUrl, HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{relativeUrl}";
    }

    public static bool IsValidSlug(this string slug)
    {
        return Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }
}
```

### Content Negotiation

```csharp
public static class ContentNegotiation
{
    public static string? GetPreferredMediaType(this HttpRequest request)
    {
        var acceptHeader = request.Headers["Accept"].ToString();
        
        if (string.IsNullOrEmpty(acceptHeader))
            return "application/json";

        var mediaTypes = acceptHeader.Split(',')
            .Select(mt => mt.Trim().Split(';')[0])
            .ToList();

        return mediaTypes.FirstOrDefault() ?? "application/json";
    }

    public static bool AcceptsJson(this HttpRequest request)
    {
        var accept = request.Headers["Accept"].ToString();
        return accept.Contains("application/json");
    }

    public static bool AcceptsXml(this HttpRequest request)
    {
        var accept = request.Headers["Accept"].ToString();
        return accept.Contains("application/xml") || accept.Contains("text/xml");
    }
}
```

### Cookie Utilities

```csharp
public static class CookieExtensions
{
    public static void SetSecureCookie(
        this HttpResponse response, 
        string name, 
        string value, 
        TimeSpan? expires = null)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires.HasValue 
                ? DateTimeOffset.UtcNow.Add(expires.Value) 
                : (DateTimeOffset?)null
        };

        response.Cookies.Append(name, value, options);
    }

    public static string? GetCookie(this HttpRequest request, string name)
    {
        return request.Cookies[name];
    }

    public static void DeleteCookie(this HttpResponse response, string name)
    {
        response.Cookies.Delete(name);
    }
}
```

### Query String Utilities

```csharp
public static class QueryStringExtensions
{
    public static Dictionary<string, string> ToDictionary(this QueryString queryString)
    {
        var result = new Dictionary<string, string>();
        
        if (!queryString.HasValue)
            return result;

        var query = queryString.Value.TrimStart('?');
        var pairs = query.Split('&');

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
        }

        return result;
    }

    public static QueryString AddOrUpdate(
        this QueryString queryString, 
        string key, 
        string value)
    {
        var dict = queryString.ToDictionary();
        dict[key] = value;
        
        var pairs = dict.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
        return QueryString.Create(dict);
    }
}
```

### File Upload Handling

```csharp
public static class FileUploadExtensions
{
    public static async Task<byte[]> ReadAllBytesAsync(this IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public static bool IsImage(this IFormFile file)
    {
        var imageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        return imageTypes.Contains(file.ContentType);
    }

    public static bool IsValidSize(this IFormFile file, long maxSizeBytes)
    {
        return file.Length <= maxSizeBytes;
    }

    public static string GetFileExtension(this IFormFile file)
    {
        return Path.GetExtension(file.FileName).ToLowerInvariant();
    }
}
```

### Security Utilities

```csharp
public static class SecurityExtensions
{
    public static string SanitizeHtml(this string html)
    {
        // Basic HTML sanitization
        return html.Replace("<script", "&lt;script")
                   .Replace("</script>", "&lt;/script&gt;");
    }

    public static string ToAntiForgeryToken(this HttpContext context)
    {
        return context.RequestServices
            .GetRequiredService<IAntiforgery>()
            .GetAndStoreTokens(context)
            .RequestToken ?? string.Empty;
    }

    public static bool ValidateAntiForgeryToken(this HttpContext context, string token)
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            antiforgery.ValidateRequestAsync(context).Wait();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### Response Helpers

```csharp
public static class ResponseHelpers
{
    public static IResult Html(string html)
    {
        return Results.Content(html, "text/html");
    }

    public static IResult JavaScript(string script)
    {
        return Results.Content(script, "application/javascript");
    }

    public static IResult Css(string css)
    {
        return Results.Content(css, "text/css");
    }

    public static IResult PlainText(string text)
    {
        return Results.Content(text, "text/plain");
    }

    public static IResult Xml(string xml)
    {
        return Results.Content(xml, "application/xml");
    }

    public static IResult Csv(string csv, string fileName = "export.csv")
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        return Results.File(bytes, "text/csv", fileName);
    }
}
```

### View Model Base Classes

```csharp
public abstract class ViewModelBase
{
    public string PageTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();
}

public class BreadcrumbItem
{
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsActive { get; set; }
}

public abstract class FormViewModel : ViewModelBase
{
    public List<string> Errors { get; set; } = new();
    public bool HasErrors => Errors.Any();
    public bool IsSuccess { get; set; }
    public string SuccessMessage { get; set; } = string.Empty;
}
```

## Usage Examples

### Building URLs

```csharp
// In a controller
var slug = product.Name.Slugify();
var productUrl = $"/products/{slug}";

// Generate absolute URL
var absoluteUrl = productUrl.ToAbsoluteUrl(Request);
```

### Working with Cookies

```csharp
// Set a secure cookie
Response.SetSecureCookie("user_pref", value, TimeSpan.FromDays(30));

// Read cookie
var preference = Request.GetCookie("user_pref");

// Delete cookie
Response.DeleteCookie("user_pref");
```

### File Upload Validation

```csharp
[HttpPost("upload")]
public async Task<IActionResult> UploadImage(IFormFile file)
{
    if (!file.IsImage())
        return BadRequest("Only image files are allowed");

    if (!file.IsValidSize(5 * 1024 * 1024)) // 5MB
        return BadRequest("File size exceeds 5MB limit");

    var bytes = await file.ReadAllBytesAsync();
    // Process image...
}
```

## Best Practices

1. **Use Extension Methods** - Extend HttpRequest/Response functionality
2. **Sanitize Input** - Always sanitize user input
3. **Secure Cookies** - Use HttpOnly, Secure, SameSite flags
4. **Handle Nulls** - Extension methods should handle null gracefully
5. **Async Methods** - Prefer async for I/O operations

## Related Packages

- `Aero.Web` - Full web framework implementation
- `Aero.Auth` - Authentication utilities
