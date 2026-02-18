# Aero Technology Stack

## Language & Runtime

- **C# 14** with **.NET 10**
- Modern language features: records, pattern matching, nullable reference types

---

## Databases

- **RavenDB** (primary) - Document database for rapid development
- **PostgreSQL** - Relational database via Entity Framework Core
- **Redis** - Caching, sessions, distributed state

---

## Web & Frontend

- **ASP.NET Core** - Web framework foundation
- **Blazor** - Server and WebAssembly components
- **Preact + HTMX** - Lightweight frontend layer (WARP stack)
- **SignalR** - Real-time communication

---

## Architecture & Messaging

- **Wolverine** - Message bus and mediator (WARP stack)
- **Command Query Separation (CQS)** - Clear read/write separation
- **Generic Repository Pattern** - Database-agnostic data access
- **Decorator Pattern** - Cross-cutting concerns

---

## Authentication

- **ASP.NET Core Identity** - User management and authentication
- **JWT tokens** - Stateless authentication
- **Passkey/WebAuthn** - Passwordless authentication support

---

## Caching

- **FusionCache** - Hybrid caching with Redis backplane
- Distributed caching across multiple servers

---

## Validation

- **FluentValidation** - Strongly-typed validation rules

---

## Testing

- **XUnit** - Testing framework
- **Bogus** - Fake data generation
- **Humanizer** - Human-readable test output
- **Moq** - Mocking library
- **FakeItEasy, NSubstitute** - Additional mocking options
- **Coverlet** - Code coverage

---

## Infrastructure & Observability

- **.NET Aspire** - Cloud-native orchestration
- **OpenObserver** - Telemetry and observability
- **Docker / Docker Compose** - Containerization
- **Elasticsearch** - Search and logging

---

## Development Tools

- **Entity Framework Core** - ORM for relational databases
- **Marten** - PostgreSQL document store (optional)
- **LiteDB** - Embedded database (optional)

---

## The WARP Stack

Aero is built on and integrates seamlessly with the WARP stack:

| Component | Technology | Purpose |
|-----------|------------|---------|
| **W** | Wolverine | Message bus, mediator, outbox |
| **A** | AspNet Core / Aero | Web framework, application accelerator |
| **R** | RavenDB | Primary document database |
| **P** | Preact + HTMX | Lightweight, reactive frontend |
