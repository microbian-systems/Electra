# Electra Blog System

A complete ASP.NET Core blogging system with Entity Framework Core, Markdown support, and comprehensive API endpoints.

## Features

- **Entity Framework Core** integration with SQLite, SQL Server, and PostgreSQL support
- **Markdown to HTML conversion** using Markdig library
- **Full CRUD operations** for blog posts
- **Pagination** and search functionality
- **Tag and author filtering**
- **Featured posts** support
- **View count tracking**
- **SEO-friendly slugs**
- **Comprehensive API** with RESTful endpoints

## Quick Start

### 1. Add Blog Services

```csharp
// In Program.cs or Startup.cs
services.AddBlogServices("Data Source=blogs.db");

// Or with SQL Server
services.AddBlogServicesWithSqlServer(connectionString);

// Or with PostgreSQL
services.AddBlogServicesWithPostgreSQL(connectionString);
```

### 2. Add Controllers

```csharp
// In Program.cs
app.MapControllers();
```

### 3. Create Database

```bash
dotnet ef migrations add InitialCreate --context BlogDbContext
dotnet ef database update --context BlogDbContext
```

## Usage Examples

### Creating a Blog Post

```csharp
var blog = new Blog
{
    Title = "My First Blog Post",
    Description = "This is a sample blog post",
    Content = "# Hello World\n\nThis is **markdown** content!",
    ContentType = ContentType.Markdown,
    Tags = ["tech", "blogging"],
    Authors = ["John Doe"],
    IsPublished = true,
    IsDraft = false
};

var createdBlog = await blogService.AddBlogAsync(blog);
```

### Getting Latest Blogs

```csharp
var latestBlogs = await blogService.GetLatestBlogsAsync(5);
```

### Converting Markdown to HTML

```csharp
var blog = await blogService.GetBlogByIdAsync(blogId);
var htmlContent = await blogService.GetContentAsHtmlAsync(blog);
```

### Paginated Blog Retrieval

```csharp
var paginatedBlogs = await blogService.GetPaginatedBlogsAsync(
    pageNumber: 1, 
    pageSize: 10, 
    publishedOnly: true
);
```

## API Endpoints

### GET /api/blog
- **Description**: Get paginated blog posts
- **Parameters**: 
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)
  - `publishedOnly` (bool, default: true)

### GET /api/blog/latest
- **Description**: Get latest blog posts
- **Parameters**: `count` (int, default: 5)

### GET /api/blog/featured
- **Description**: Get featured blog posts
- **Parameters**: `count` (int, default: 5)

### GET /api/blog/{id}
- **Description**: Get blog post by ID
- **Parameters**: `id` (Guid)

### GET /api/blog/slug/{slug}
- **Description**: Get blog post by slug
- **Parameters**: `slug` (string)

### GET /api/blog/{id}/html
- **Description**: Get blog content as HTML
- **Parameters**: `id` (Guid)

### GET /api/blog/search
- **Description**: Search blog posts
- **Parameters**: 
  - `searchTerm` (string)
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)

### GET /api/blog/tag/{tag}
- **Description**: Get blogs by tag
- **Parameters**: 
  - `tag` (string)
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)

### GET /api/blog/author/{author}
- **Description**: Get blogs by author
- **Parameters**: 
  - `author` (string)
  - `pageNumber` (int, default: 1)
  - `pageSize` (int, default: 10)

### GET /api/blog/tags
- **Description**: Get all unique tags

### POST /api/blog
- **Description**: Create new blog post
- **Body**: Blog object

### PUT /api/blog/{id}
- **Description**: Update blog post
- **Parameters**: `id` (Guid)
- **Body**: Blog object

### DELETE /api/blog/{id}
- **Description**: Delete blog post
- **Parameters**: `id` (Guid)

## Database Schema

### Blog Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| Title | string(200) | Blog title |
| Description | string(500) | Blog description |
| Content | string | Raw content |
| ContentType | enum | Content format (Markdown, HTML, JSON) |
| Tags | string[] | Associated tags |
| Authors | string[] | Blog authors |
| SvgData | string? | Inline SVG image data |
| ImageUrl | string? | External image URL |
| IsPublished | bool | Publication status |
| IsDraft | bool | Draft status |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |
| Slug | string(250) | SEO-friendly URL slug |
| ViewCount | int | Number of views |
| IsFeatured | bool | Featured post flag |

## Configuration

### Custom Markdown Pipeline

The service uses Markdig with advanced extensions:

```csharp
var markdownPipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseBootstrap()
    .UseSyntaxHighlighting()
    .Build();
```

### Database Providers

- **SQLite**: Default, file-based database
- **SQL Server**: Enterprise database solution
- **PostgreSQL**: Open-source database
- **In-Memory**: For testing purposes

## Testing

The system includes support for in-memory testing:

```csharp
services.AddBlogServicesInMemory("TestDb");
```

## Dependencies

- **Entity Framework Core** (7.0+)
- **Markdig** (latest)
- **Microsoft.Extensions.Logging** (7.0+)
- **System.Text.Json** (7.0+)

## License

This project is part of the Electra framework and follows the same licensing terms.