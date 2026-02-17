# Aero .NET Framework

<p align="center">
  <img src="./assets/Aero2.png" alt="Aero Logo" width="150" height="150" />
</p>

<p align="center">
  <strong>A comprehensive, modular .NET web framework implementing Clean Architecture principles</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#contributing">Contributing</a>
</p>

---

## Overview

Aero is a production-ready .NET framework designed for building scalable, maintainable web applications. Built on **.NET 9.0**, it implements **Clean Architecture** with a strong emphasis on **Command Query Separation (CQS)**, **decorator patterns**, and **generic repository abstractions**. Aero provides a complete toolkit for modern web development while maintaining flexibility and extensibility.

### Key Philosophy

- **Thin Controllers**: MVC Controllers should be small and focused, delegating business logic to commands and queries
- **Cross-Cutting Concerns**: Logging, caching, validation, and timing handled transparently via decorators
- **Database Agnostic**: Support for multiple database technologies simultaneously
- **Modular Design**: Use only what you need, extend what you require

---

## Features

### Core Architecture Patterns

- **Command Query Separation (CQS)**: Clear separation between read and write operations
  - Commands: `ICommand<T>`, `IAsyncCommand<T>` for operations
  - Queries: `IQueryHandler<TResult>`, `IAsyncQueryHandler<TParam, TResult>`
  - Base implementations: `AbstractAsyncCommandHandler<T>`

- **Generic Repository Pattern**: Unified data access across all database implementations
  - Interface: `IRepository<T, TKey>` with generic key constraints
  - Entities implement `IEntity<TKey>` with built-in audit fields (CreatedOn, ModifiedOn, CreatedBy, ModifiedBy)

- **Decorator Pattern**: Cross-cutting concerns without polluting business logic
  - `LoggingCommandDecorator<T>` - Automatic operation logging
  - `CachingRepository<T, TKey>` - Transparent data caching
  - `TimingCommandDecorator<T>` - Performance monitoring
  - `ValidationCommandHandlerDecorator<T>` - Input validation
  - `ExceptionCommandHandlerDecorator<T>` - Error handling
  - `RetryCommandHandlerDecorator<T>` - Resilience patterns

### Multi-Database Support

Aero supports multiple databases simultaneously, allowing you to use the best tool for each job:

- **PostgreSQL** (primary) - Entity Framework Core integration
- **RavenDB** - Document store with event sourcing support
- **Marten** - PostgreSQL-based document store
- **Elasticsearch** - Search and analytics
- **LiteDB** - Embedded database for local scenarios
- **DynamoDB** - AWS NoSQL operations

### AI Integration

Built-in support for Large Language Model (LLM) operations via `Aero.Core.Ai`:
- Microsoft SemanticKernel integration
- AI usage logging and tracking
- Extensible provider architecture

### Blockchain & Cryptography

Comprehensive Solana blockchain integration:
- **Solnet Libraries**: Full suite for Solana operations
  - `Solnet.Wallet` - Wallet and key management
  - `Solnet.Rpc` - RPC client
  - `Solnet.Programs` - Program interactions
  - `Solnet.Metaplex` - NFT and metadata
  - `Solnet.Pyth` - Price oracle
  - `Solnet.Raydium` - DEX operations
- Abstracted crypto interfaces for extensibility

### Authentication & Authorization

- **JWT Authentication** - Token-based security
- **OpenIddict** - Full OAuth2/OIDC implementation
- **Passkey Support** - WebAuthn/FIDO2 integration
- **Social Providers** - Google, Facebook, Microsoft, Twitter, Apple, Coinbase OAuth

### Caching

- **FusionCache** with Redis backplane
- Distributed caching across multiple servers
- Local in-memory caching with Redis synchronization

### Real-Time Communication

- **SignalR** integration for real-time web functionality
- Scalable hub architecture

### Background Processing

- Integration with TickerQ for job processing
- PostgreSQL-backed operational store
- Dashboard for job monitoring

---

## Architecture

### Project Structure

```
Aero/
├── Core/                          # Domain and application core
│   ├── Aero.Core                 # Entities, algorithms, encryption
│   ├── Aero.Core.Ai            # AI/SemanticKernel integration
│   ├── Aero.Common             # Commands, queries, decorators, utilities
│   └── Aero.Models             # DTOs and view models
│
├── Persistence/                   # Data access implementations
│   ├── Aero.Persistence.Core   # Core abstractions
│   ├── Aero.Persistence        # Base implementations
│   ├── Aero.EfCore            # Entity Framework (PostgreSQL)
│   ├── Aero.RavenDB           # RavenDB document store
│   ├── Aero.Marten            # Marten PostgreSQL document store
│   ├── Aero.Elastic           # Elasticsearch integration
│   └── Aero.RavenDB.ES        # Event sourcing with RavenDB
│
├── Web/                          # Web infrastructure
│   ├── Aero.Web.Core          # Core web abstractions
│   ├── Aero.Web               # MVC, middleware, JWT auth
│   └── Aero.Components        # Blazor components
│
├── Services/                     # Business logic
│   ├── Aero.Services          # Core services, feature toggles
│   └── Aero.Cms               # Content management system
│
├── Infrastructure/               # Cross-cutting infrastructure
│   ├── Aero.Caching           # FusionCache + Redis
│   ├── Aero.Auth              # Authentication & authorization
│   ├── Aero.Validators        # FluentValidation implementations
│   ├── Aero.Events            # Event sourcing and domain events
│   ├── Aero.Actors            # Actor model implementation
│   ├── Aero.SignalR           # Real-time communication
│   └── Aero.Cloudflare        # Cloudflare integration
│
├── Crypto/                       # Blockchain & cryptography
│   ├── Electra.Crypto.Core    # Core cryptographic abstractions
│   ├── Electra.Crypto.Base    # Base implementations
│   └── Electra.Crypto.Solana  # Solana blockchain integration
│
└── Tests/                        # Comprehensive test suite
```

### Key Design Principles

1. **Generic Type Safety**: Entities use `IEntity<TKey>` with `IEquatable<TKey>` constraints
2. **Async/Await Throughout**: All repository and service operations are asynchronous
3. **Modular Registration**: Services register via extension methods for clean composition
4. **Environment Configuration**: Support for multiple environments with appropriate service registration
5. **Clean Abstractions**: Heavy use of interfaces and dependency inversion
6. **Cross-Cutting Concerns**: Decorators handle logging, caching, validation, timing automatically

### Dependency Injection Setup

```csharp
// Core services
builder.Services.AddAeroDefaults(builder.Configuration);

// API-specific setup
builder.Services.AddDefaultApi(builder.Configuration);

// Database-specific
builder.Services.AddEntityFrameworkStores<AeroDbContext>();
builder.Services.AddRavenDbStores();
builder.Services.AddMartenStores();
```

---

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for infrastructure services)
- [PgAdmin](https://www.pgadmin.org/download/) (optional, for PostgreSQL management)

### Installation

1. **Clone the repository** (with submodules for Solnet):
   ```bash
   git clone --recursive <repo-url>
   cd Aero
   ```

2. **Start development services**:
   ```bash
   docker-compose -f src/docker-compose.yml up -d
   ```
   This starts PostgreSQL, Redis, Elasticsearch, and other infrastructure services.

3. **Build the solution**:
   ```bash
   dotnet build src/Aero.sln
   ```

4. **Run database migrations** (if applicable):
   ```bash
   ./src/run-migrations.ps1
   ```

### Creating Your First Application

```csharp
// Program.cs
using Aero.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Aero services
builder.Services.AddAeroDefaults(builder.Configuration);
builder.Services.AddDefaultApi(builder.Configuration);

// Add your specific services
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure middleware
app.UseAeroDefaults();
app.MapControllers();
app.MapRazorPages();

app.Run();
```

### Defining an Entity

```csharp
using Aero.Core.Entities;

public class Product : Entity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
}
```

### Creating a Repository

```csharp
// Using Entity Framework
public class ProductRepository : GenericEntityFrameworkRepository<Product>
{
    public ProductRepository(AeroDbContext context, ILogger<ProductRepository> log) 
        : base(context, log) { }
}

// Or using RavenDB
public class ProductRepository : RavenDbRepositoryBase<Product>
{
    public ProductRepository(IAsyncDocumentSession session) 
        : base(session) { }
}
```

### Creating a Command

```csharp
public class CreateProductCommand : IAsyncCommand<CreateProductRequest, Product>
{
    private readonly IGenericRepository<Product> _repository;

    public CreateProductCommand(IGenericRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<Product> ExecuteAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            Description = request.Description
        };

        return await _repository.InsertAsync(product);
    }
}
```

---

## Documentation

### Individual Project Documentation

Each project contains its own detailed README.md with specific documentation:

| Project | Description |
|---------|-------------|
| [Aero.Core](./src/Aero.Core/README.md) | Core domain entities, algorithms, and utilities |
| [Aero.Common](./src/Aero.Common/README.md) | Command/Query patterns, decorators, shared utilities |
| [Aero.Persistence.Core](./src/Aero.Persistence.Core/README.md) | Core persistence abstractions and interfaces |
| [Aero.Persistence](./src/Aero.Persistence/README.md) | Base repository implementations |
| [Aero.EfCore](./src/Aero.EfCore/README.md) | Entity Framework Core integration |
| [Aero.RavenDB](./src/Aero.RavenDB/README.md) | RavenDB document store integration |
| [Aero.Marten](./src/Aero.Marten/README.md) | Marten PostgreSQL document store |
| [Aero.Caching](./src/Aero.Caching/README.md) | FusionCache with Redis backplane |
| [Aero.Web](./src/Aero.Web/README.md) | Web framework extensions and middleware |
| [Aero.Web.Core](./src/Aero.Web.Core/README.md) | Core web abstractions |
| [Aero.Auth](./src/Aero.Auth/README.md) | Authentication and authorization |
| [Aero.Validators](./src/Aero.Validators/README.md) | FluentValidation implementations |
| [Aero.SignalR](./src/Aero.SignalR/README.md) | Real-time communication |
| [Aero.Core.Ai](./src/Aero.Core.Ai/README.md) | AI integration with SemanticKernel |
| [Electra.Crypto.Solana](./src/Electra.Crypto.Solana/README.md) | Solana blockchain integration |

### Build Commands

```bash
# Build entire solution
dotnet build src/Aero.sln

# Clean solution and restore packages
./src/clean.ps1

# Build release and create NuGet packages
./src/pack.sh
```

### Testing

```bash
# Run all tests
dotnet test src/Aero.sln

# Run specific test project
dotnet test src/Aero.Core.Tests/
dotnet test src/Aero.Validators.Tests/
dotnet test src/Electra.Crypto.Solana.Tests/
```

---

## Development Infrastructure

The included `docker-compose.yml` provides a comprehensive development environment:

### Databases
- PostgreSQL (primary database)
- Redis (caching and sessions)
- Elasticsearch cluster (search and logging)
- RethinkDB, Cassandra, Riak (additional options)

### Message Queues
- Kafka
- RabbitMQ
- Zookeeper

### Monitoring & Observability
- Seq (structured logging)
- Jaeger (distributed tracing)
- Kibana (Elasticsearch visualization)
- Elastic APM (application performance monitoring)
- ELK Stack (Logstash, various Beats)

### Security
- Vault (secrets management)

---

## Contributing

### Development Guidelines

1. **Git Flow**: Use feature branches, create PRs to `develop` branch
2. **Thin Controllers**: Keep MVC controllers small, delegate to commands/queries
3. **Sub-module Awareness**: This repo has sub-modules (Aero). Push changes from both main repo and sub-modules
4. **Follow Best Practices**:
   - [SOLID Principles](https://www.digitalocean.com/community/conceptual_articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)
   - [DRY - Don't Repeat Yourself](https://en.wikipedia.org/wiki/Don%27t_repeat_yourself)
   - Clean Code principles

### Getting Started

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is proprietary software. All rights reserved.

&copy; 2024 made with ❤️ [Microbian Systems](https://microbians.io/)

---

## Support

For support, questions, or feature requests:
- Visit [Microbian Systems](https://microbians.io/)
- Create an issue in the repository
- Contact the development team

---

<p align="center">
  <strong>Built for scale. Designed for maintainability. Powered by .NET.</strong>
</p>
