using Electra.Web.BlogEngine.Entities;
using Electra.Web.BlogEngine.Enums;
using Electra.Web.BlogEngine.Extensions;
using Electra.Web.BlogEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Web.BlogEngine.Examples;

/// <summary>
/// Example demonstrating how to integrate the blog system into an ASP.NET Core application
/// </summary>
public static class BlogIntegrationExample
{
    /// <summary>
    /// Example of configuring blog services in Program.cs
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Option 1: SQLite (default)
        //services.AddBlogServices("Data Source=blogs.db");

        // Option 2: SQL Server
        // services.AddBlogServicesWithSqlServer("Server=.;Database=BlogDb;Trusted_Connection=true;");

        // Option 3: PostgreSQL
        // services.AddBlogServicesWithPostgreSQL("Host=localhost;Database=blogdb;Username=postgres;Password=password");

        // Option 4: In-Memory (for testing)
        // services.AddBlogServicesInMemory("TestBlogDb");
    }

    /// <summary>
    /// Example of using the blog service
    /// </summary>
    public static async Task BlogServiceExample(IBlogService blogService)
    {
        // Create a new blog post
        var newBlog = new BlogEntry
        {
            Title = "Getting Started with ASP.NET Core",
            Description = "A comprehensive guide to building web applications with ASP.NET Core",
            Content = @"# Getting Started with ASP.NET Core

ASP.NET Core is a cross-platform, high-performance framework for building modern web applications.

## Key Features

- **Cross-platform**: Runs on Windows, macOS, and Linux
- **High performance**: Optimized for speed and scalability
- **Open source**: Available on GitHub with community contributions

## Installation

```bash
dotnet new webapi -n MyWebApi
cd MyWebApi
dotnet run
```

This will create a new web API project and start the development server.",
            ContentType = ContentType.Markdown,
            Tags = ["aspnetcore", "tutorial", "web-development"],
            Authors = ["John Developer"],
            IsPublished = true,
            IsDraft = false,
            IsFeatured = true
        };

        
        // Add the blog post
        var createdBlog = await blogService.AddBlogAsync(newBlog);

        await createdBlog.IfSomeAsync(async blog =>
        {
            Console.WriteLine($"Created blog with ID: {blog.Id}");

            // Get latest blogs
            var latestBlogs = await blogService.GetLatestBlogsAsync(5);
            Console.WriteLine($"Retrieved {latestBlogs.Count()} latest blogs");

            // Get paginated blogs
            var paginatedBlogs = await blogService.GetPaginatedBlogsAsync();
            Console.WriteLine(
                $"Page 1 contains {paginatedBlogs.Items.Count()} blogs out of {paginatedBlogs.TotalCount} total");

            // Convert markdown to HTML
            var htmlContent = await blogService.GetContentAsHtmlAsync(blog);
            Console.WriteLine($"HTML content length: {htmlContent.Length} characters");

            // Search blogs
            var searchResults = await blogService.SearchBlogsAsync("ASP.NET");
            Console.WriteLine($"Found {searchResults.TotalCount} blogs matching 'ASP.NET'");

            // Get blogs by tag
            var taggedBlogs = await blogService.GetBlogsByTagAsync("tutorial");
            Console.WriteLine($"Found {taggedBlogs.TotalCount} blogs tagged with 'tutorial'");

            // Get all tags
            var allTags = await blogService.GetAllTagsAsync();
            Console.WriteLine($"Available tags: {string.Join(", ", allTags)}");

            // Update the blog
            blog.ViewCount = 100;
            blog.Description = "Updated description";
            var updatedBlog = await blogService.UpdateBlogAsync(blog);
            Console.WriteLine($"Updated blog, new view count: {blog.ViewCount}");
        });
    }
    
    /// <summary>
    /// Example of seeding sample data
    /// </summary>
    public static async Task SeedSampleData(IBlogService blogService)
    {
        var sampleBlogs = new[]
        {
            new BlogEntry
            {
                Title = "Introduction to Entity Framework Core",
                Description = "Learn the basics of Entity Framework Core ORM",
                Content = @"# Entity Framework Core Basics

Entity Framework Core is a lightweight, extensible ORM for .NET applications.

## Key Concepts

- **DbContext**: The main class for database operations
- **DbSet**: Represents a table in the database
- **Migrations**: Version control for your database schema",
                ContentType = ContentType.Markdown,
                Tags = ["entityframework", "orm", "database"],
                Authors = ["Jane Database"],
                IsPublished = true,
                IsDraft = false
            },
            new BlogEntry
            {
                Title = "Building RESTful APIs",
                Description = "Best practices for creating REST APIs with ASP.NET Core",
                Content = @"# RESTful API Design

REST (Representational State Transfer) is an architectural style for web services.

## HTTP Methods

- **GET**: Retrieve data
- **POST**: Create new resources
- **PUT**: Update existing resources
- **DELETE**: Remove resources",
                ContentType = ContentType.Markdown,
                Tags = ["api", "rest", "web-services"],
                Authors = ["Bob API"],
                IsPublished = true,
                IsDraft = false,
                IsFeatured = true
            },
            new BlogEntry
            {
                Title = "Advanced C# Features",
                Description = "Exploring modern C# language features",
                Content = @"# Modern C# Features

C# continues to evolve with new features that improve developer productivity.

## Recent Additions

- **Records**: Immutable reference types
- **Pattern Matching**: Enhanced switch expressions
- **Nullable Reference Types**: Better null safety",
                ContentType = ContentType.Markdown,
                Tags = ["csharp", "programming", "language-features"],
                Authors = ["Alice Coder"],
                IsPublished = true,
                IsDraft = false
            }
        };

        foreach (var blog in sampleBlogs)
        {
            await blogService.AddBlogAsync(blog);
        }

        Console.WriteLine($"Seeded {sampleBlogs.Length} sample blog posts");
    }
}