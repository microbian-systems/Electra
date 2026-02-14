# AGENTS.md - ZauberCMS.Web

## Project Overview

**ZauberCMS.Web** is the main web application that hosts the ZauberCMS content management system. This is a .NET 10.0 Blazor Server application that demonstrates and tests the ZauberCMS framework.

### ⚠️ IMPORTANT NOTES

1. **Original Source Code**: The original ZauberCMS source code can be found at:
   - `d:/proj/microbians/microbians.io/Zauber`
   - **DO NOT MODIFY** the original code in that location

2. **Database Migration**: This project has been converted from **Entity Framework Core** to **RavenDB** document store.
   - All EF Core migrations have been removed
   - RavenDB.Client and RavenDB.Embedded (v7.2.0) are now used
   - The project uses `Electra.Persistence.RavenDB` for RavenDB integration

3. **Current Status**: ⚠️ **The site is currently not loading properly** - this is a known issue that needs investigation.

## Project Structure

### Referenced Projects

| Project | Description | Path |
|---------|-------------|------|
| `ZauberCMS` | Main ZauberCMS NuGet package (Razor Class Library) | `../ZauberCMS/ZauberCMS.csproj` |
| `Electra.Persistence.RavenDB` | RavenDB persistence layer | `../Electra.Persistence.RavenDB/Electra.Persistence.RavenDB.csproj` |
| `Electra.Web.Core` | Core web abstractions | `../Electra.Web.Core/Electra.Web.Core.csproj` |
| `Electra.Web` | Web framework extensions | `../Electra.Web/Electra.Web.csproj` |

### ZauberCMS Project Hierarchy

```
ZauberCMS.Web (Web Application)
├── ZauberCMS (RCL - Main Package)
│   ├── ZauberCMS.Components (RCL - UI Components)
│   │   └── ZauberCMS.Core (Core Models/Services)
│   └── ZauberCMS.Routing (RCL - Routing)
│       └── ZauberCMS.Components
└── Electra.* Projects (Infrastructure)
```

### All ZauberCMS Projects

1. **ZauberCMS.Core** (`../ZauberCMS.Core/`)
   - Core domain models, interfaces, and services
   - Content, Media, Membership, SEO, Language management
   - Data access interfaces (ISeedData, etc.)
   - **Dependencies**: RavenDB.Client, RavenDB.Embedded, Electra.* projects

2. **ZauberCMS.Components** (`../ZauberCMS.Components/`)
   - Razor components for the admin interface
   - Content editors, media managers, user management
   - Rich text editor (RTE) integration
   - **Dependencies**: ZauberCMS.Core

3. **ZauberCMS.Routing** (`../ZauberCMS.Routing/`)
   - Dynamic routing for CMS content
   - View components for rendering content
   - **Dependencies**: ZauberCMS.Components

4. **ZauberCMS** (`../ZauberCMS/`)
   - Main NuGet package (Razor Class Library)
   - Combines Components and Routing
   - Package version: 4.1.0
   - **Dependencies**: ZauberCMS.Components, ZauberCMS.Routing

## Key Architecture Changes

### Database Layer Migration

#### Before (EF Core)
- Multiple DbContext implementations (PostgreSQL, SQLite, SQL Server)
- Entity Framework migrations in `Migrations/` folders
- DbContext factories and design-time contexts

#### After (RavenDB)
- `IDocumentSession` and `IAsyncDocumentSession` for data access
- Document store configuration via `Electra.Persistence.RavenDB`
- RavenDB indexes for querying
- **Removed files** (marked as None in csproj):
  - `ContentDbMapping.cs`
  - `ContentPropertyValueDbMapping.cs`
  - `ContentTypeDbMapping.cs`
  - `DomainDbMapping.cs`
  - `UnpublishedContentDbMapping.cs`
  - `MediaDbMapping.cs`
  - `LanguageDbMapping.cs`
  - `AuditDbMapper.cs`
  - All EF Core mapping files moved to `Morgue/` folders

### Seeding Data

The application uses `ISeedData` interface for initial data:

```csharp
public interface ISeedData
{
    void Initialise(IDocumentSession store);
}
```

**Current Seeders**:
- `DefaultContentSeeder` (in `SeedData/DefaultContentSeeder.cs`)
  - Creates content types: Website, HomePage, Blog, BlogPost, TextPage, ContactPage
  - Creates element types: RichTextEditor, Quote, Image, FAQ, FAQItem
  - Creates default content: Home, Blog, About Us, Contact pages
  - Creates sample blog posts

**Seeding Execution**: `ZauberSetup.cs` lines 336-342

### Content Types

The web application expects these content types (defined in `SeedData/DefaultContentSeeder.cs`):

| Content Type | View Component | Purpose |
|--------------|----------------|---------|
| `Website` | - | Root container for site |
| `HomePage` | `HomeView.razor` | Home page |
| `Blog` | `BlogView.razor` | Blog listing page |
| `BlogPost` | `BlogPageView.razor` | Individual blog post |
| `TextPage` | `TextPageView.razor` | Generic text page |
| `ContactPage` | `ContactView.razor` | Contact page with form |

### Element Types (for BlockLists)

- `RichTextEditor` - Rich text content blocks
- `Quote` - Quote/citation blocks
- `Image` - Image blocks
- `FAQ` - FAQ accordion blocks
- `FAQItem` - Individual FAQ items

## Build & Development Commands

```bash
# Build the web application
dotnet build ZauberCMS.Web/ZauberCMS.Web.csproj

# Run the application
dotnet run --project ZauberCMS.Web

# Build entire solution
dotnet build Electra.sln
```

## Configuration

### appsettings.json

Key configuration sections:
- `Zauber:DatabaseProvider` - Set to "RavenDB" (was "PostgreSQL"/"SQLite"/"SqlServer")
- `Zauber:RedisConnectionString` - For distributed caching
- `AdminUser:Email` - Default admin email
- `AdminUser:Password` - Default admin password

### RavenDB Connection

Configured via `Electra.Persistence.RavenDB`:
- Default: Embedded RavenDB for development
- Can be configured for external RavenDB server

## Known Issues

### Site Not Loading
The application currently has issues loading the site after the RavenDB migration. Potential causes:

1. **Routing Issues**:
   - Content finder pipeline may not resolve URLs correctly
   - Check `DefaultContentFinder.cs` in ZauberCMS.Core

2. **Content Resolution**:
   - Verify `ISeedData` implementations are running
   - Check if content is being created in RavenDB
   - Review `GetContentFromRequestAsync` in ContentService

3. **View Resolution**:
   - View component names must match fully qualified names
   - Check `ViewComponent` property on Content entities

4. **RavenDB Issues**:
   - Indexes may not be created properly
   - Check `RegisterRavenIndexes()` in startup

### Debugging Tips

1. Check RavenDB Studio (usually at http://localhost:8080 when using embedded)
2. Verify documents are being created:
   - ContentTypes
   - Content (pages)
   - ElementTypes
3. Check logs for routing/content resolution errors
4. Verify `ZauberRouteValueTransformer` is registered

## File Locations

### Key Files in ZauberCMS.Web

| File | Purpose |
|------|---------|
| `Program.cs` | Application entry point, DI configuration |
| `App.razor` | Root Blazor component |
| `_Imports.razor` | Global using statements |
| `SeedData/DefaultContentSeeder.cs` | Default content seeding |
| `Pages/*.razor` | Content view components |
| `Layouts/*.razor` | Layout components |
| `ContentBlocks/*.razor` | Element type views |
| `wwwroot/assets/` | Static assets (images, CSS) |

### Pages Directory

- `HomeView.razor` - Home page view
- `BlogView.razor` - Blog listing view
- `BlogPageView.razor` - Individual blog post view
- `TextPageView.razor` - Generic text page view
- `ContactView.razor` - Contact page view

## Best Practices Applied

1. **Clean Architecture**: Separation of concerns with Core/Components/Routing layers
2. **CQRS Pattern**: Commands and queries for data operations
3. **Repository Pattern**: Abstracted data access through interfaces
4. **Dependency Injection**: Extensive use of DI for services
5. **Async/Await**: All database operations are asynchronous
6. **Document Database**: Proper RavenDB document modeling

## Migration Notes

### EF Core to RavenDB Changes

1. **Removed**:
   - All `DbContext` classes
   - EF Core migrations
   - Entity configuration classes (DbMapping files)
   - SQL-specific query logic

2. **Added**:
   - `IDocumentSession` usage throughout
   - RavenDB-specific indexes
   - Document store configuration

3. **Modified**:
   - All repositories now use RavenDB queries
   - `ISeedData` interface uses `IDocumentSession` instead of `DbContext`
   - Identity stores now use RavenDB

### UserRoles Entity Removal

**Status**: ✅ **REMOVED**

The `UserRole` entity (junction table between CmsUser and CmsRole) has been **removed** as it was redundant with RavenDB's document modeling approach.

#### Before (EF Core Pattern)
```csharp
// Junction table pattern - necessary for relational databases
public class UserRole
{
    public string UserId { get; set; }
    public CmsUser User { get; set; }
    public string RoleId { get; set; }
    public CmsRole Role { get; set; }
}

public class CmsUser
{
    public List<UserRole> UserRoles { get; set; }  // Many-to-many via junction
}

public class CmsRole
{
    public List<UserRole> UserRoles { get; set; }  // Many-to-many via junction
}
```

#### After (RavenDB Pattern)
```csharp
// Roles are now embedded or referenced directly
public class ElectraUser : IdentityUser<string>
{
    public List<ElectraRole> Roles { get; set; }  // Direct reference
}

// CmsRole now inherits from ElectraRole
public class CmsRole : ElectraRole
{
    // UserRoles list removed - no longer needed
}
```

#### Rationale

1. **Document Database Philosophy**: RavenDB handles many-to-many relationships differently:
   - Roles can be embedded in user documents (for read-heavy scenarios)
   - Or referenced by ID with RavenDB's `Include()` pattern for efficient loading

2. **Identity Framework Integration**: 
   - `ElectraUser` extends ASP.NET Core Identity
   - Identity manages roles through its built-in `UserManager` and `RoleManager`
   - No need for custom junction tables

3. **Simplified Model**:
   - `CmsRole` now inherits directly from `ElectraRole` (Identity Role)
   - UI-specific role data moved to `CmsRoleUI` document
   - Permission-based access (ContentRole, MediaRole) use direct references

#### Files Affected

- `ZauberCMS.Core/Membership/Models/UserRole.cs` - **Emptied** (placeholder only)
- `ZauberCMS.Core/Membership/Models/CmsUser.cs` - **Removed** (replaced by ElectraUser)
- `ZauberCMS.Core/Membership/Models/CmsRole.cs` - Modified (removed UserRoles property)
- `ZauberCMS.Core/Membership/Mapping/UserRoleDbMapping.cs` - **Removed**

## User Profile Architecture Analysis

### Overview

The codebase maintains **two separate user profile systems** that serve different purposes:

1. **ElectraUserProfile** - Core user profile for the Electra framework
2. **CmsUserProfile** - CMS-specific extended profile for ZauberCMS

### ElectraUserProfile

**Location**: `Electra.Models/Entities/ElectraUserProfile.cs`

**Purpose**: Core user profile data for the Electra identity framework

**Key Properties**:
| Property | Type | Description |
|----------|------|-------------|
| `Userid` | `string` | Foreign key to ElectraUser (Identity table) |
| `Username` | `string` | User's display name |
| `Website` | `string?` | Personal website URL |
| `SocialMedia` | `Dictionary<SocialMediaType, string>` | Social media links |
| `Headline` | `string` | User headline/tagline |
| `Location` | `string` | User location |
| `Bio` | `string?` | User biography |
| `ImageUrl` | `string?` | Profile image URL or base64 |
| `Address` | `AddressModel?` | Physical address |

**Characteristics**:
- Extends `Entity` base class
- Uses data annotations (`[JsonPropertyName]`, `[MinLength]`, `[MaxLength]`, `[Url]`)
- Designed for general user profile management
- Repository: `IElectraUserProfileRepository` in `Electra.Persistence.Core`
- Service: `ElectraUserProfileService` in `Electra.Services`

### CmsUserProfile

**Location**: `ZauberCMS.Core/Membership/Models/CmsUserProfile.cs`

**Purpose**: CMS-specific user profile for ZauberCMS content management features

**Key Properties**:
| Property | Type | Description |
|----------|------|-------------|
| `UserId` | `string` | Reference to ElectraUser document ID |
| `User` | `ElectraUser?` | Navigation property (loaded via RavenDB Include) |
| `PropertyData` | `List<UserPropertyValue>` | Dynamic CMS properties |
| `ExtendedData` | `Dictionary<string, object>` | Custom extended data |
| `ProfileImageId` | `string?` | Reference to media library image |

**Characteristics**:
- Extends `Entity` base class
- Implements `IHasPropertyValues` interface (for CMS property system)
- Designed for CMS-specific user data
- Uses RavenDB document reference pattern (`UserId` + `User` navigation)
- Service: `ElectraUserProfileService` in `ZauberCMS.Core` (note: same name, different namespace!)

### UserPropertyValue (CMS)

**Location**: `ZauberCMS.Core/Membership/Models/UserPropertyValue.cs`

Used by `CmsUserProfile` for dynamic property storage:
- `Id`, `Alias`, `Value`, `ContentTypePropertyId`
- `UserId` - Reference to user
- Implements `IPropertyValue` interface

### Key Differences

| Aspect | ElectraUserProfile | CmsUserProfile |
|--------|-------------------|----------------|
| **Namespace** | `Electra.Models.Entities` | `ZauberCMS.Core.Membership.Models` |
| **Purpose** | Core identity/profile | CMS content management |
| **Data Structure** | Fixed properties | Dynamic property system |
| **User Reference** | `Userid` property | `UserId` + `User` navigation |
| **Property System** | Static properties | `PropertyData` list with aliases |
| **Media Integration** | `ImageUrl` string | `ProfileImageId` (media library) |
| **Social Media** | Built-in dictionary | Via extended properties |
| **Annotations** | Heavy use of data annotations | Clean, minimal annotations |
| **Interface** | None | `IHasPropertyValues` |

### Services Comparison

#### ElectraUserProfileService (Electra.Services)
```csharp
public class ElectraUserProfileService : UserProfileService<ElectraUserProfile>
// Uses: IUserRepository, IElectraUserProfileRepository
// Generic repository pattern
// Methods: GetById, GetByEmail, InsertAsync, UpdateAsync, UpsertAsync, DeleteAsync
```

#### ElectraUserProfileService (ZauberCMS.Core) ⚠️ NAME COLLISION
```csharp
public class ElectraUserProfileService : IElectraUserProfileService
// Uses: IAsyncDocumentSession (RavenDB)
// Document database pattern
// Methods: GetProfileAsync, GetUserWithProfileAsync, GetOrCreateProfileAsync, SaveProfileAsync
// Also manages: CmsRoleUI (role UI configuration)
```

**⚠️ Important**: There are TWO services with the same name `ElectraUserProfileService`:
1. `Electra.Services.ElectraUserProfileService` - Works with `ElectraUserProfile`
2. `ZauberCMS.Core.Membership.Services.ElectraUserProfileService` - Works with `CmsUserProfile`

### Integration Pattern

**ElectraUser** contains an embedded `Profile` property of type `ElectraUserProfile`:
```csharp
public class ElectraUser : IdentityUser<string>, IElectraUser
{
    public ElectraUserProfile Profile { get; set; }  // Embedded profile
    // ... other properties
}
```

**CmsUserProfile** is a separate document with reference to ElectraUser:
```csharp
public class CmsUserProfile : Entity, IHasPropertyValues
{
    public string UserId { get; set; }  // Document reference
    [JsonIgnore]
    public ElectraUser? User { get; set; }  // Loaded via Include()
    // ... CMS-specific properties
}
```

### Recommendations

1. **Consolidation Opportunity**: Consider merging `CmsUserProfile` functionality into `ElectraUserProfile` or making `CmsUserProfile` inherit from `ElectraUserProfile`

2. **Naming Clarification**: Rename one of the `ElectraUserProfileService` classes to avoid confusion:
   - `ZauberCMS.Core.Membership.Services.CmsUserProfileService` (suggested)

3. **Data Flow**: 
   - Core identity data → `ElectraUser` + `ElectraUserProfile`
   - CMS-specific data → `CmsUserProfile`
   - Consider which system "owns" the profile image (currently both have image fields)

4. **RavenDB Optimization**:
   - `CmsUserProfile` already uses proper RavenDB patterns (document references, Include())
   - `ElectraUserProfile` is embedded in `ElectraUser` (good for atomic access)

## References

- Original Project: https://github.com/YodasMyDad/ZauberCMS
- RavenDB Documentation: https://ravendb.net/docs
- Blazor Documentation: https://docs.microsoft.com/aspnet/core/blazor
