# Track Plan: Identity & UI Integration

**Track ID:** identity_ui_20260218
**Phases:** 9-11

---

## Phase 9: Identity & Auth

- [x] Task: Add Microsoft.AspNetCore.Identity NuGet to Aero.CMS.Core
    - [x] Add package reference to project file

- [x] Task: Create PasskeyCredential model
    - [x] Create file: Aero.CMS.Core/Membership/Models/PasskeyCredential.cs
    - [x] Properties: Id, CredentialId (byte[]), PublicKey (byte[]), SignCount, DeviceName, CreatedAt, LastUsedAt

- [x] Task: Create RefreshToken model
    - [x] Create file: Aero.CMS.Core/Membership/Models/RefreshToken.cs
    - [x] Properties: Token, ExpiresAt, IsRevoked, CreatedByIp, CreatedAt

- [x] Task: Create UserClaim model
    - [x] Create file: Aero.CMS.Core/Membership/Models/UserClaim.cs
    - [x] Properties: ClaimType, ClaimValue

- [x] Task: Create CmsUser model
    - [x] Create file: Aero.CMS.Core/Membership/Models/CmsUser.cs
    - [x] Extend AuditableDocument
    - [x] Identity properties: UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp
    - [x] Lockout properties: LockoutEnd, LockoutEnabled, AccessFailedCount
    - [x] Ban properties: IsBanned, BannedUntil, BanReason
    - [x] Collections: Roles, Claims, Passkeys, RefreshTokens

- [x] Task: Create CmsRole model
    - [x] Create file: Aero.CMS.Core/Membership/Models/CmsRole.cs
    - [x] Extend AuditableDocument
    - [x] Properties: Name, NormalizedName, Permissions list

- [x] Task: Create Permissions static class
    - [x] Create file: Aero.CMS.Core/Membership/Models/Permissions.cs
    - [x] Constants for all permissions
    - [x] All[] array

- [x] Task: Create RavenUserStore
    - [x] Create file: Aero.CMS.Core/Membership/Stores/RavenUserStore.cs
    - [x] Implement IUserStore<CmsUser>
    - [x] Implement IUserPasswordStore<CmsUser>
    - [x] Implement IUserEmailStore<CmsUser>
    - [x] Implement IUserRoleStore<CmsUser>
    - [x] Implement IUserClaimStore<CmsUser>
    - [x] Implement IUserLockoutStore<CmsUser>
    - [x] Each method opens/disposes its own session

- [x] Task: Create RavenRoleStore
    - [x] Create file: Aero.CMS.Core/Membership/Stores/RavenRoleStore.cs
    - [x] Implement IRoleStore<CmsRole>

- [x] Task: Create IBanService interface
    - [x] Create file: Aero.CMS.Core/Membership/Services/BanService.cs
    - [x] Methods: BanAsync, UnbanAsync, IsBannedAsync

- [x] Task: Create BanService implementation
    - [x] Implement IsBannedAsync: true if IsBanned && (BannedUntil == null || BannedUntil > UtcNow)
    - [x] Implement BanAsync: set IsBanned, BanReason, BannedUntil
    - [x] Implement UnbanAsync: clear ban properties

- [x] Task: Write BanService unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Membership/BanServiceTests.cs
    - [x] Test: BanAsync sets IsBanned=true
    - [x] Test: BanAsync with null until = permanent ban
    - [x] Test: BanAsync with future DateTime = temporary ban
    - [x] Test: UnbanAsync clears properties
    - [x] Test: IsBannedAsync true for permanent ban
    - [x] Test: IsBannedAsync true for active temporary ban
    - [x] Test: IsBannedAsync false for expired temporary ban

- [x] Task: Write RavenUserStore integration tests
    - [x] Create file: Aero.CMS.Tests.Integration/Membership/RavenUserStoreTests.cs
    - [x] Test: CreateAsync saves user
    - [x] Test: FindByIdAsync retrieves correctly
    - [x] Test: FindByNameAsync retrieves by NormalizedUserName
    - [x] Test: FindByEmailAsync retrieves by NormalizedEmail
    - [x] Test: UpdateAsync persists changes
    - [x] Test: DeleteAsync removes user
    - [x] Test: AddToRoleAsync/RemoveFromRoleAsync/IsInRoleAsync/GetRolesAsync
    - [x] Test: Password hash round-trip
    - [x] Test: Lockout round-trip
    - [x] Test: AccessFailedCount increment/reset

- [x] Task: Verify Phase 9 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Membership"`
    - [x] Run `dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Membership"`
    - [x] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 9: Identity & Auth' (Protocol in workflow.md)

---

## Phase 10: Block Registry

- [x] Task: Create IBlockRegistry interface
    - [x] Create file: Aero.CMS.Core/Plugins/Interfaces/IBlockRegistry.cs
    - [x] Methods: Register<TBlock,TView>(), Register(string, Type), Resolve(string), GetAll()

- [x] Task: Create BlockRegistry implementation
    - [x] Create file: Aero.CMS.Core/Plugins/BlockRegistry.cs
    - [x] Use reflection to get BlockType static property
    - [x] Store in Dictionary<string, Type>

- [x] Task: Write BlockRegistry unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Plugins/BlockRegistryTests.cs
    - [x] Test: Register<TBlock,TView> uses BlockType as alias
    - [x] Test: Resolve returns correct Type
    - [x] Test: Resolve returns null for unregistered alias
    - [x] Test: Register(string, Type) overload works
    - [x] Test: GetAll returns all entries
    - [x] Test: Re-registering overwrites previous

- [x] Task: Verify Phase 10 gate
    - [x] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Plugins"`
    - [x] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 10: Block Registry' (Protocol in workflow.md)

---

## Phase 11: IRichTextEditor Abstraction

- [ ] Task: Create RichTextEditorSettings
    - [ ] Create file: Aero.CMS.Core/Content/Models/RichTextEditorSettings.cs
    - [ ] Properties: MinHeight (300), EnableMedia (true), EnableTables (true), EnableCodeBlocks (true), ToolbarItems

- [ ] Task: Create IRichTextEditor interface
    - [ ] Create file: Aero.CMS.Core/Content/Interfaces/IRichTextEditor.cs
    - [ ] EditorAlias property
    - [ ] Render method with value, isEditing, onChanged callback, settings

- [ ] Task: Create NullRichTextEditor
    - [ ] Create file: Aero.CMS.Core/Content/Services/NullRichTextEditor.cs
    - [ ] EditorAlias = "null"
    - [ ] Render returns RenderFragment with textarea

- [ ] Task: Write RichTextEditor unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/RichTextEditorTests.cs
    - [ ] Test: NullRichTextEditor.EditorAlias == "null"
    - [ ] Test: NullRichTextEditor.Render returns non-null RenderFragment
    - [ ] Test: IRichTextEditor can be substituted
    - [ ] Test: RichTextEditorSettings defaults

- [ ] Task: Verify Phase 11 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] Confirm full suite green (phases 1-11)

- [ ] Task: Conductor - User Manual Verification 'Phase 11: IRichTextEditor Abstraction' (Protocol in workflow.md)
