# Technology Stack: Electra

## Core Environment
- **Language:** C#
- **Runtime:** .NET 10.0+
- **Project Type:** Modular Library Collection / Microservices

## Frameworks & Libraries
- **Web API:** ASP.NET Core
  - **Authentication & Identity:**
    - OpenIddict (OpenID Connect/OAuth2)
    - ASP.NET Core Identity
    - WebAuthn.Net (Passkeys)
- **UI & Frontend:**
  - Blazor (Server & WASM)
  - Electra.MerakiUI (Tailwind CSS + AlpineJS)
  - TypeScript
- **Persistence & Data:**
  - Entity Framework Core (PostgreSQL)  - Marten (Event Sourcing / Document DB on Postgres)
  - RavenDB (NoSQL Document Store)
- **Utilities & Logic:**
  - Serilog (Structured Logging)
  - SnowflakeGuid (Distributed ID Generation)
  - SecretSharingDotNet (Shamir's Secret Sharing)
  - ThrowGuard (Guard Clauses)

## Infrastructure & DevOps
- **Containerization:** Docker & Docker Compose
- **Cloud Services:**
  - Azure Blob Storage (Cloud Storage)
  - Cloudflare (DNS/External IP Management)
- **Communication:**
  - SignalR (Real-time Web)
  - Electra.Actors (Actor Model)
