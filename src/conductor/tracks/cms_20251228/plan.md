# Track Plan: Dynamic ASP.NET Core CMS

## Phase 1: Core Runtime
Implement the foundational routing and rendering engine.

- [x] Task: Define RavenDB document models (Site, Page, Block)
- [x] Task: Implement `Pages_BySiteAndUrl` RavenDB index
- [x] Task: Implement Site Resolution middleware/service
- [x] Task: Implement Page Routing logic (URL -> PageDocument)
- [x] Task: Create `PageRenderContext` and basic Block Rendering Loop
- [x] Task: Conductor - User Manual Verification 'Core Runtime' (Protocol in workflow.md)

## Phase 2: Block System
Build the strong-typed block rendering and discovery system.

- [x] Task: Implement Block Schema definitions and Validation logic
- [x] Task: Create Assembly Scanning mechanism for Block discovery
- [x] Task: Implement core WebAwesome blocks (Hero, Rich Text, Image, etc.)
- [x] Task: Create Razor partials for core blocks
- [x] Task: Conductor - User Manual Verification 'Block System' (Protocol in workflow.md)

## Phase 3: Output Caching
Implement performance optimization strategies.

- [x] Task: Define `CmsOptions` configuration
- [x] Task: Implement ETag generation logic (Page/Site versioning)
- [x] Task: Create Output Caching Middleware with toggle support
- [x] Task: Configure CDN-friendly response headers
- [x] Task: Conductor - User Manual Verification 'Output Caching' (Protocol in workflow.md)

## Phase 4: Editor UI
Develop the back-office administration interface.

- [x] Task: Scaffold Razor Pages Admin area
- [x] Task: Implement Tree-based Page Navigation
- [x] Task: Build Page Editor with Metadata and Block Picker
- [x] Task: Implement Drag-and-Drop Block Ordering (AlpineJS/HTMX)
- [x] Task: Implement Draft/Preview/Publish workflow logic
- [x] Task: Conductor - User Manual Verification 'Editor UI' (Protocol in workflow.md)

## Phase 5: RBAC & Workflows
Add security and content governance.

- [x] Task: Define Roles (Admin, Editor, Author, etc.) and Permissions
- [x] Task: Implement User/Role management in RavenDB
- [x] Task: Implement Content Approval Workflows (Draft -> Review -> Publish)
- [x] Task: Add hooks for optional Elsa workflow integration
- [x] Task: Conductor - User Manual Verification 'RBAC & Workflows' (Protocol in workflow.md)

## Phase 6: Blazor Integration
Enable modern interactivity and live previews.

- [x] Task: Implement Blazor Server rendering for Editor Live Preview
- [x] Task: Create shared contracts for Blazor/Razor block rendering
- [x] Task: Implement optional WASM block support
- [x] Task: Conductor - User Manual Verification 'Blazor Integration' (Protocol in workflow.md)
