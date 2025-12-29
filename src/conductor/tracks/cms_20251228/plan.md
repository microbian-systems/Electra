# Track Plan: Dynamic ASP.NET Core CMS

## Phase 1: Core Runtime
Implement the foundational routing and rendering engine.

- [~] Task: Define RavenDB document models (Site, Page, Block)
- [ ] Task: Implement `Pages_BySiteAndUrl` RavenDB index
- [ ] Task: Implement Site Resolution middleware/service
- [ ] Task: Implement Page Routing logic (URL -> PageDocument)
- [ ] Task: Create `PageRenderContext` and basic Block Rendering Loop
- [ ] Task: Conductor - User Manual Verification 'Core Runtime' (Protocol in workflow.md)

## Phase 2: Block System
Build the strong-typed block rendering and discovery system.

- [ ] Task: Implement Block Schema definitions and Validation logic
- [ ] Task: Create Assembly Scanning mechanism for Block discovery
- [ ] Task: Implement core WebAwesome blocks (Hero, Rich Text, Image, etc.)
- [ ] Task: Create Razor partials for core blocks
- [ ] Task: Conductor - User Manual Verification 'Block System' (Protocol in workflow.md)

## Phase 3: Output Caching
Implement performance optimization strategies.

- [ ] Task: Define `CmsOptions` configuration
- [ ] Task: Implement ETag generation logic (Page/Site versioning)
- [ ] Task: Create Output Caching Middleware with toggle support
- [ ] Task: Configure CDN-friendly response headers
- [ ] Task: Conductor - User Manual Verification 'Output Caching' (Protocol in workflow.md)

## Phase 4: Editor UI
Develop the back-office administration interface.

- [ ] Task: Scaffold Razor Pages Admin area
- [ ] Task: Implement Tree-based Page Navigation
- [ ] Task: Build Page Editor with Metadata and Block Picker
- [ ] Task: Implement Drag-and-Drop Block Ordering (AlpineJS/HTMX)
- [ ] Task: Implement Draft/Preview/Publish workflow logic
- [ ] Task: Conductor - User Manual Verification 'Editor UI' (Protocol in workflow.md)

## Phase 5: RBAC & Workflows
Add security and content governance.

- [ ] Task: Define Roles (Admin, Editor, Author, etc.) and Permissions
- [ ] Task: Implement User/Role management in RavenDB
- [ ] Task: Implement Content Approval Workflows (Draft -> Review -> Publish)
- [ ] Task: Add hooks for optional Elsa workflow integration
- [ ] Task: Conductor - User Manual Verification 'RBAC & Workflows' (Protocol in workflow.md)

## Phase 6: Blazor Integration
Enable modern interactivity and live previews.

- [ ] Task: Implement Blazor Server rendering for Editor Live Preview
- [ ] Task: Create shared contracts for Blazor/Razor block rendering
- [ ] Task: Implement optional WASM block support
- [ ] Task: Conductor - User Manual Verification 'Blazor Integration' (Protocol in workflow.md)
