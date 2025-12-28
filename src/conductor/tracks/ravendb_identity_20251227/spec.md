# Track Spec: RavenDB Identity Implementation

## Overview
This track focuses on completing and validating the RavenDB-backed implementation of ASP.NET Core Identity within the `Electra.Persistence.RavenDB` project. The goal is to provide a fully functional alternative to the PostgreSQL provider, supporting modern authentication features like Passkeys (WebAuthn).

## Goals
- Complete the `UserStore` and `RoleStore` implementations to support all standard ASP.NET Core Identity interfaces.
- Implement built-in Passkey (WebAuthn) support using the latest ASP.NET Core 9/10 patterns.
- Optimize RavenDB querying and indexing for Identity operations.
- Ensure seamless integration with the `Electra.Auth` microservice.

## Functional Requirements
- **Core Stores:**
  - Implement/Refine `IUserStore<TUser>`, `IRoleStore<TRole>`, and associated stores (Password, Email, Phone, Lockout, TwoFactor, Login, Claim).
  - Implement `IUserWebAuthnCredentialStore` for native Passkey support.
- **Data Persistence:**
  - Ensure all Identity entities (Users, Roles, Claims, Logins, Tokens) are correctly persisted to RavenDB.
  - Implement efficient RavenDB indexes for common lookups (e.g., by Username, Email, ProviderKey).
- **Security:**
  - Maintain parity with standard Identity security behaviors (Password hashing, security timestamps).
  - Support for multi-factor authentication (MFA).

## Technical Requirements
- Target Framework: .NET 9.0/10.0.
- Database: RavenDB (latest stable).
- Adhere to `Electra` coding standards and modular architecture.

## Acceptance Criteria
- [ ] All standard ASP.NET Core Identity unit tests for stores pass when targeting RavenDB.
- [ ] Passkey registration and authentication work successfully with the RavenDB provider.
- [ ] Integration tests in `Electra.Auth` pass when switched to RavenDB.
- [ ] Code coverage for new implementation reaches >80%.

## Out of Scope
- Migrating existing PostgreSQL data to RavenDB.
- UI changes in `Electra.Auth` (unless required for provider compatibility).
