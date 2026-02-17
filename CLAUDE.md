# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

**Build and Clean:**
```bash
# Build entire solution
dotnet build src/Aero.sln

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
dotnet test src/Aero.sln

# Run specific test project
dotnet test src/Aero.Core.Tests/
dotnet test src/Aero.Validators.Tests/
dotnet test src/Aero.Crypto.Solana.Tests/
dotnet test src/Aero.Crypto.Base.Tests/
dotnet test src/Aero.SendGrid.Tests/
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
- `Aero.Core` - Core domain entities, algorithms (including Shamir's Secret Sharing), encryption
- `Aero.Core.Ai` - AI integration using Microsoft.SemanticKernel for LLM operations
- `Aero.Common` - Command/Query patterns, decorators, shared utilities
- `Aero.Models` - DTOs, view models, API request/response models
- `Aero.Persistence` - Repository implementations for multiple databases
- `Aero.Persistence.Core` - Core persistence abstractions and interfaces
- `Aero.Persistence.Marten` - Marten (PostgreSQL) document store implementation
- `Aero.Services` - Business logic, feature toggles, user management

**Infrastructure:**
- `Aero.Web` - Web framework extensions, middleware, JWT authentication
- `Aero.Web.Core` - Core web abstractions and utilities
- `Aero.Web.BlogEngine` - Blog engine implementation
- `Aero.Caching` - FusionCache-based caching with Redis backplane
- `Aero.Auth` - OpenIddict authentication with passkey support
- `Aero.Validators` - FluentValidation implementations
- `Aero.Events` - Event sourcing and domain events

**Specialized:**
- `Aero.Crypto.Core` - Core cryptographic abstractions and interfaces
- `Aero.Crypto.Base` - Base cryptographic implementations and utilities
- `Aero.Crypto.Solana` - Solana blockchain integration using Solnet libraries
- `Aero.SignalR` - Real-time communication hub
- `Aero.Components` - Blazor components
- `Aero.Actors` - Actor model implementation
- `Aero.Workflows` - Workflow orchestration

**Testing:**
- Test projects use XUnit with mocking libraries (FakeItEasy, NSubstitute)
- Code coverage via coverlet.collector

### Dependency Injection Setup

Use extension methods for clean service registration:
```csharp
// Core services
builder.Services.AddAeroDefaults(builder.Configuration);

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

The `src/docker-compose.yml` provides a comprehensive development environment including:
- **Databases**: PostgreSQL, Redis, Elasticsearch cluster, RethinkDB, Cassandra, Riak
- **Message queues**: Kafka, RabbitMQ, Zookeeper
- **Monitoring & Logging**: Seq, Jaeger, Kibana, ELK Stack (Logstash, various Beats)
- **Security**: Vault for secrets management
- **APM**: Application Performance Monitoring with Elastic APM

### Key Design Principles

1. **Generic Type Safety** - Entities use `IEntity<TKey>` with `IEquatable<TKey>` constraints
2. **Async/Await Throughout** - All repository and service operations are asynchronous
3. **Modular Registration** - Services register via extension methods for clean composition
4. **Environment Configuration** - Support for multiple environments with appropriate service registration
5. **Clean Abstractions** - Heavy use of interfaces and dependency inversion
6. **Cross-Cutting Concerns** - Decorators handle logging, caching, validation, timing automatically

### Recent Architecture Changes

**AI Integration:**
- New `Aero.Core.Ai` project implements Microsoft.SemanticKernel integration
- Supports LLM operations and AI-powered features
- Includes usage logging through `AiUsageLog` entity

**Caching Evolution:**
- Migrated from Foundatio to FusionCache with Redis backplane
- Improved performance and reliability with distributed caching
- Maintains decorator pattern for transparent caching

**Cryptography Modularization:**
- Split crypto functionality into `Aero.Crypto.Core` and `Aero.Crypto.Base`
- Enhanced separation of concerns for cryptographic operations
- Maintains Solana-specific implementations in dedicated project

### Common Gotchas

- Some PowerShell scripts may reference outdated paths (Microbians vs Aero)
- The solution includes git submodules for Solnet libraries
- Authentication supports both JWT and OpenIddict with passkey integration
- Repository decorators are automatically applied through DI configuration
- The docker-compose.yml is located in `src/` directory, not root