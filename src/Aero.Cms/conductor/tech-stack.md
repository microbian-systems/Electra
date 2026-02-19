# Aero CMS - Technology Stack

## Runtime & Framework

| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 10 LTS |
| Web Framework | ASP.NET Core | 10.x |
| UI Framework | Blazor Server | 10.x |
| Additional Views | Razor Pages + MVC | 10.x |

## Projects Structure

| Project | Type | Purpose |
|---------|------|---------|
| Aero.CMS.Core | Class Library | Domain models, interfaces, services, repositories |
| Aero.CMS.Components | Razor Class Library | Blazor admin UI components |
| Aero.CMS.Routing | Class Library | MVC shell, route transformer, content finder |
| Aero.CMS.Web | Blazor Server | Public site, content views, block views |
| Aero.CMS.Tests.Unit | xUnit | Unit tests |
| Aero.CMS.Tests.Integration | xUnit | Integration tests |

## Data Layer

| Component | Technology | Notes |
|-----------|------------|-------|
| Database | RavenDB | Document store with revisions |
| Client | RavenDB.Client | Official .NET client |
| Testing | RavenDB.TestDriver | Embedded for integration tests |

## Testing Stack

| Component | Library | Purpose |
|-----------|---------|---------|
| Test Framework | xUnit | Test runner |
| Mocking | NSubstitute | Mock objects (ONLY) |
| Assertions | Shouldly | Fluent assertions (ONLY) |
| Test Data | AutoFixture | Object generation |
| Test Data | AutoFixture.AutoNSubstitute | NSubstitute integration |
| Test Data | Bogus | Fake data generation |
| Coverage | coverlet.collector | Code coverage |

## Content Processing

| Component | Library | Purpose |
|-----------|---------|---------|
| Markdown | Markdig | Markdown parsing with extensions |
| HTML Stripping | Regex | No external HTML parser |

## Authentication

| Component | Library | Purpose |
|-----------|---------|---------|
| Identity | Microsoft.AspNetCore.Identity | User/role management |
| Passkeys | Custom | FIDO2 WebAuthn support |

## Key NuGet Packages

### Aero.CMS.Core
- RavenDB.Client
- Microsoft.Extensions.Options
- Microsoft.Extensions.DependencyInjection.Abstractions
- Markdig
- Microsoft.AspNetCore.Identity

### Aero.CMS.Routing
- Microsoft.AspNetCore.App (framework reference)

### Test Projects
- xunit
- xunit.runner.visualstudio
- Shouldly
- NSubstitute
- AutoFixture
- AutoFixture.AutoNSubstitute
- Bogus
- RavenDB.TestDriver
- Microsoft.NET.Test.Sdk
- coverlet.collector
