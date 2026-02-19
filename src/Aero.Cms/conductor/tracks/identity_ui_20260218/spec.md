# Track Specification: Identity & UI Integration

**Track ID:** identity_ui_20260218
**Phases:** 9-11
**Status:** Complete
**Dependency:** routing_search_20260218

## Overview

This track implements the membership system with RavenDB backing, block registry for component mapping, and rich text editor abstraction.

## Phase 9: Identity & Auth

### Goal
Raven-backed ASP.NET Identity, passkeys.

### NuGet Package
- Microsoft.AspNetCore.Identity (Aero.CMS.Core)

### Deliverables

#### Identity Models
- PasskeyCredential (Id, CredentialId, PublicKey, SignCount, DeviceName, CreatedAt, LastUsedAt)
- RefreshToken (Token, ExpiresAt, IsRevoked, CreatedByIp, CreatedAt)
- UserClaim (ClaimType, ClaimValue)
- CmsUser (extends AuditableDocument with Identity properties, Roles, Claims, Passkeys, RefreshTokens)
- CmsRole (extends AuditableDocument with Name, NormalizedName, Permissions)

#### Permissions Static Class
- ContentCreate, ContentEdit, ContentApprove, ContentPublish, ContentDelete
- MediaManage, UsersManage, SettingsManage, PluginsManage

#### RavenUserStore
- IUserStore, IUserPasswordStore, IUserEmailStore, IUserRoleStore, IUserClaimStore, IUserLockoutStore

#### RavenRoleStore
- IRoleStore<CmsRole>

#### BanService
- IBanService with BanAsync, UnbanAsync, IsBannedAsync

### Phase 9 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Membership"
dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Membership"
```

## Phase 10: Block Registry

### Goal
IBlockRegistry singleton — seam between content model and Blazor.

### Deliverables

#### IBlockRegistry Interface
- Register<TBlock, TView>()
- Register(string blockTypeAlias, Type viewComponentType)
- Resolve(string blockTypeAlias)
- GetAll()

#### BlockRegistry Implementation
- Uses reflection to get BlockType from TBlock
- Dictionary<string, Type> storage

### Phase 10 Gate
```bash
dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Plugins"
```

## Phase 11: IRichTextEditor Abstraction

### Goal
Swappable RTE contract before any Blazor editor is built.

### Deliverables

#### RichTextEditorSettings
- MinHeight (default 300)
- EnableMedia, EnableTables, EnableCodeBlocks (all true by default)
- ToolbarItems list

#### IRichTextEditor Interface
- EditorAlias property
- Render method returning RenderFragment

#### NullRichTextEditor
- Fallback implementation rendering plain textarea
- EditorAlias = "null"

### Phase 11 Gate
```bash
dotnet test Aero.CMS.Tests.Unit
dotnet test Aero.CMS.Tests.Integration
```
Full suite green (phases 1-11)

## Dependencies

- Track: routing_search_20260218 (Phase 6-8 complete)

## Success Criteria

- ASP.NET Identity stores work with RavenDB
- Passkey credential storage functional
- Block registry maps types correctly
- Rich text editor abstraction allows swappable implementations
