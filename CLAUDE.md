# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

**Build and Clean:**
```bash
# Build entire solution
dotnet build src/Electra.sln

# Clean solution and restore packages  
./src/clean.ps1

# Clean bin/obj folders only
./src/rm-bin-obj.sh

# Build release and create NuGet packages
./src/pack.sh
```

**Testing:**
```bash
# Run all tests
dotnet test src/Electra.sln

# Run specific test project
dotnet test src/Electra.Core.Tests/
dotnet test src/Electra.Validators.Tests/
dotnet test src/Electra.Crypto.Solana.Tests/
```

**Development Environment:**
```bash
# Start development services (PostgreSQL, Redis, Elasticsearch, etc.)
docker-compose up -d

# Run database migrations (paths may need updates)
./src/run-migrations.ps1
./src/update-database.ps1
```

## Architecture Overview

This is a .NET 9.0 modular web framework implementing **Clean Architecture** with the following key patterns:

### Core Architectural Patterns

**Command Query Separation (CQS):**
- Commands: `ICommand<T>`, `IAsyncCommand<T>` for operations
- Queries: `IQueryHandler<TResult>`, `IAsyncQueryHandler<TParam, TResult>`
- Base implementations: `AbstractAsyncCommandHandler<T>`

**Generic Repository Pattern:**
- Multiple implementations: Entity Framework, DynamoDB, Elasticsearch, LiteDB
- Interface: `IRepository<T, TKey>` with generic key constraints
- Entities implement `IEntity<TKey>` with built-in audit fields

**Decorator Pattern for Cross-Cutting Concerns:**
- Logging: `LoggingCommandDecorator<T>`
- Caching: `CachingRepository<T, TKey>` 
- Timing: `TimingCommandDecorator<T>`
- Validation: `ValidationCommandHandlerDecorator<T>`
- Exception handling: `ExceptionCommandHandlerDecorator<T>`
- Retry logic: `RetryCommandHandlerDecorator<T>`

### Project Structure

**Core Projects:**
- `Electra.Core` - Core domain entities, algorithms (including Shamir's Secret Sharing), encryption
- `Electra.Common` - Command/Query patterns, decorators, shared utilities
- `Electra.Models` - DTOs, view models, API request/response models
- `Electra.Persistence` - Repository implementations for multiple databases
- `Electra.Services` - Business logic, feature toggles, user management

**Infrastructure:**
- `Electra.Web` - Web framework extensions, middleware, JWT authentication
- `Electra.Caching` - Foundatio-based caching with decorators
- `Electra.Auth` - OpenIddict authentication with passkey support
- `Electra.Validators` - FluentValidation implementations

**Specialized:**
- `Electra.Crypto.Solana` - Solana blockchain integration using Solnet libraries
- `Electra.SignalR` - Real-time communication hub
- `Electra.Components` - Blazor components

**Testing:**
- Test projects use XUnit with mocking libraries (FakeItEasy, NSubstitute)
- Code coverage via coverlet.collector

### Dependency Injection Setup

Use extension methods for clean service registration:
```csharp
// Core services
builder.Services.AddElectraDefaults(builder.Configuration);

// API-specific setup
builder.Services.AddDefaultApi(builder.Configuration);
```

### Database Support

The framework supports multiple databases simultaneously:
- **PostgreSQL** (primary) - Entity Framework
- **Redis** - Caching and sessions
- **Elasticsearch** - Search and logging
- **DynamoDB** - NoSQL operations
- **LiteDB** - Embedded database option

### Solnet Integration

The project includes comprehensive Solana blockchain support through multiple Solnet submodules:
- `Solnet.Wallet` - Wallet and key management
- `Solnet.Rpc` - RPC client for Solana
- `Solnet.Programs` - Program interactions
- `Solnet.Metaplex` - NFT and metadata operations
- `Solnet.Pyth` - Price oracle integration
- `Solnet.Raydium` - DEX operations

### Development Infrastructure

The `docker-compose.yml` provides a comprehensive development environment including:
- PostgreSQL, Redis, Elasticsearch cluster
- Message queues (Kafka, RabbitMQ)
- Monitoring (Seq, Jaeger, Kibana)
- Multiple database options for testing

### Key Design Principles

1. **Generic Type Safety** - Entities use `IEntity<TKey>` with `IEquatable<TKey>` constraints
2. **Async/Await Throughout** - All repository and service operations are asynchronous
3. **Modular Registration** - Services register via extension methods for clean composition
4. **Environment Configuration** - Support for multiple environments with appropriate service registration
5. **Clean Abstractions** - Heavy use of interfaces and dependency inversion
6. **Cross-Cutting Concerns** - Decorators handle logging, caching, validation, timing automatically

### Common Gotchas

- Some PowerShell scripts may reference outdated paths (Microbians vs Electra)
- The solution includes git submodules for Solnet libraries
- Authentication supports both JWT and OpenIddict with passkey integration
- Repository decorators are automatically applied through DI configuration