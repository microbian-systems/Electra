# Aero CMS - Product Guidelines

## Code Quality Standards

### Testing Requirements
- **Unit Tests:** NSubstitute for mocking, Shouldly for assertions
- **Integration Tests:** RavenDB.TestDriver for persistence tests
- **Coverage Targets:**
  - Core/Shared: 90%
  - Content: 85%
  - Membership: 85%
  - SEO: 80%
  - Media: 80%
  - Plugins: 75%
  - Overall: 80%

### Naming Conventions
- **Test Methods:** `MethodName_Condition_ExpectedResult`
- **Interfaces:** `I` prefix (e.g., `IContentRepository`)
- **Implementations:** No suffix (e.g., `ContentRepository`)
- **Async Methods:** `Async` suffix (e.g., `GetByIdAsync`)

### Code Style
- No comments unless explicitly requested
- File-scoped namespaces
- Expression-bodied members where appropriate
- Primary constructors for dependency injection

## Development Workflow

### Phase Gates (Hard Stops)
- Run `dotnet test` before advancing to next phase
- All tests must pass - zero failures permitted
- No `NotImplementedException` or TODO placeholders

### Strict Adherence Rules
1. Never proceed if current phase tests fail
2. Never infer or invent interfaces - use exact signatures
3. Never skip a TEST block
4. Every file path is exact - no renaming or restructuring
5. Do not add unrequested NuGet packages
6. Do not implement future phases

### Mocking & Assertions
- **Mocking:** NSubstitute ONLY (no Moq, no FakeItEasy)
- **Assertions:** Shouldly ONLY
- **Test Data:** AutoFixture, AutoFixture.AutoNSubstitute, Bogus

## Architecture Principles

### ID Strategy
- `Guid` throughout all entities
- RavenDB IdentityPartsSeparator: `/`

### Persistence
- RavenDB CRUD operations
- Revisions enabled for versioning (default: 50 revisions)

### Dependency Injection
- Extension methods for service registration
- `AddAeroCmsCore()` as composition root
- Singleton for stateless services
- Scoped for repository/services

### Cross-Cutting Concerns
- Decorator pattern for logging, caching, timing
- Save hook pipeline for before/after operations
- Content finder pipeline for routing
