# Track Plan: Identity & UI Integration

**Track ID:** identity_ui_20260218
**Phases:** 9-11

---

## Phase 9: Identity & Auth

- [ ] Task: Add Microsoft.AspNetCore.Identity NuGet to Aero.CMS.Core
    - [ ] Add package reference to project file

- [ ] Task: Create PasskeyCredential model
    - [ ] Create file: Aero.CMS.Core/Membership/Models/PasskeyCredential.cs
    - [ ] Properties: Id, CredentialId (byte[]), PublicKey (byte[]), SignCount, DeviceName, CreatedAt, LastUsedAt

- [ ] Task: Create RefreshToken model
    - [ ] Create file: Aero.CMS.Core/Membership/Models/RefreshToken.cs
    - [ ] Properties: Token, ExpiresAt, IsRevoked, CreatedByIp, CreatedAt

- [ ] Task: Create UserClaim model
    - [ ] Create file: Aero.CMS.Core/Membership/Models/UserClaim.cs
    - [ ] Properties: ClaimType, ClaimValue

- [ ] Task: Create CmsUser model
    - [ ] Create file: Aero.CMS.Core/Membership/Models/CmsUser.cs
    - [ ] Extend AuditableDocument
    - [ ] Identity properties: UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp
    - [ ] Lockout properties: LockoutEnd, LockoutEnabled, AccessFailedCount
    - [ ] Ban properties: IsBanned, BannedUntil, BanReason
    - [ ] Collections: Roles, Claims, Passkeys, RefreshTokens

- [ ] Task: Create CmsRole model
    - [ ] Create file: Aero.CMS.Core/Membership/Models/CmsRole.cs
    - [ ] Extend AuditableDocument
    - [ ] Properties: Name, NormalizedName, Permissions list

- [ ] Task: Create Permissions static class
    - [ ] Create file: Aero.CMS.Core/Membership/Models/Permissions.cs
    - [ ] Constants for all permissions
    - [ ] All[] array

- [ ] Task: Create RavenUserStore
    - [ ] Create file: Aero.CMS.Core/Membership/Stores/RavenUserStore.cs
    - [ ] Implement IUserStore<CmsUser>
    - [ ] Implement IUserPasswordStore<CmsUser>
    - [ ] Implement IUserEmailStore<CmsUser>
    - [ ] Implement IUserRoleStore<CmsUser>
    - [ ] Implement IUserClaimStore<CmsUser>
    - [ ] Implement IUserLockoutStore<CmsUser>
    - [ ] Each method opens/disposes its own session

- [ ] Task: Create RavenRoleStore
    - [ ] Create file: Aero.CMS.Core/Membership/Stores/RavenRoleStore.cs
    - [ ] Implement IRoleStore<CmsRole>

- [ ] Task: Create IBanService interface
    - [ ] Create file: Aero.CMS.Core/Membership/Services/BanService.cs
    - [ ] Methods: BanAsync, UnbanAsync, IsBannedAsync

- [ ] Task: Create BanService implementation
    - [ ] Implement IsBannedAsync: true if IsBanned && (BannedUntil == null || BannedUntil > UtcNow)
    - [ ] Implement BanAsync: set IsBanned, BanReason, BannedUntil
    - [ ] Implement UnbanAsync: clear ban properties

- [ ] Task: Write BanService unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Membership/BanServiceTests.cs
    - [ ] Test: BanAsync sets IsBanned=true
    - [ ] Test: BanAsync with null until = permanent ban
    - [ ] Test: BanAsync with future DateTime = temporary ban
    - [ ] Test: UnbanAsync clears properties
    - [ ] Test: IsBannedAsync true for permanent ban
    - [ ] Test: IsBannedAsync true for active temporary ban
    - [ ] Test: IsBannedAsync false for expired temporary ban

- [ ] Task: Write RavenUserStore integration tests
    - [ ] Create file: Aero.CMS.Tests.Integration/Membership/RavenUserStoreTests.cs
    - [ ] Test: CreateAsync saves user
    - [ ] Test: FindByIdAsync retrieves correctly
    - [ ] Test: FindByNameAsync retrieves by NormalizedUserName
    - [ ] Test: FindByEmailAsync retrieves by NormalizedEmail
    - [ ] Test: UpdateAsync persists changes
    - [ ] Test: DeleteAsync removes user
    - [ ] Test: AddToRoleAsync/RemoveFromRoleAsync/IsInRoleAsync/GetRolesAsync
    - [ ] Test: Password hash round-trip
    - [ ] Test: Lockout round-trip
    - [ ] Test: AccessFailedCount increment/reset

- [ ] Task: Verify Phase 9 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Membership"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~Membership"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 9: Identity & Auth' (Protocol in workflow.md)

---

## Phase 10: Block Registry

- [ ] Task: Create IBlockRegistry interface
    - [ ] Create file: Aero.CMS.Core/Plugins/Interfaces/IBlockRegistry.cs
    - [ ] Methods: Register<TBlock,TView>(), Register(string, Type), Resolve(string), GetAll()

- [ ] Task: Create BlockRegistry implementation
    - [ ] Create file: Aero.CMS.Core/Plugins/BlockRegistry.cs
    - [ ] Use reflection to get BlockType static property
    - [ ] Store in Dictionary<string, Type>

- [ ] Task: Write BlockRegistry unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Plugins/BlockRegistryTests.cs
    - [ ] Test: Register<TBlock,TView> uses BlockType as alias
    - [ ] Test: Resolve returns correct Type
    - [ ] Test: Resolve returns null for unregistered alias
    - [ ] Test: Register(string, Type) overload works
    - [ ] Test: GetAll returns all entries
    - [ ] Test: Re-registering overwrites previous

- [ ] Task: Verify Phase 10 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~Plugins"`
    - [ ] Confirm all pass, zero failures

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
