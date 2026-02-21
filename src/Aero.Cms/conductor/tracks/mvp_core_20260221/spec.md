# Track Specification: MVP Core Implementation

## Overview
This track implements the "Minimum Viable Product" (MVP) for Aero CMS. The goal is a functioning system where a user can boot the app, create a site and pages, build layouts using sections and columns, and render those pages publicly. It bridges the architectural foundation (Phases 0-16) to a usable application.

## Functional Requirements
- **Site Management**: Create and configure a default `SiteDocument` on startup (Bootstrap).
- **Page Management**: CRUD operations for pages with custom slugs.
- **Layout System**: 
    - `SectionBlock` for horizontal rows (Full, Two-Column, Three-Column, Sidebar).
    - `ColumnBlock` for vertical layout cells.
- **Content Blocks**: Support for RichText, Image, Hero, Quote, Markdown, and HTML blocks.
- **Admin UI**:
    - Page listing and creation.
    - Site settings editor.
    - A "Block Canvas" for visual page building.
    - Inline block editing.
    - **Drag & Drop**: Implement reordering of sections and blocks using `BlazorSortable` (SortableJS wrapper).
- **Public Rendering**: 
    - Route matching for page slugs.
    - Dynamic rendering of block trees.
    - Public vs. Admin view modes for components.
- **Media**: Implementation of `DiskStorageProvider` for local file storage and `NullProvider` for fallback/testing.

## Non-Functional Requirements
- **No Authentication**: The `/admin` area is open and accessible without login (per MVP spec).
- **Persistence**: All data stored in RavenDB using existing repository patterns.
- **Styling**: Tailwind CSS + Vanilla CSS for both Admin and Public views.
- **Performance**: Debounced autosave (800ms) in the page editor.

## Acceptance Criteria
- App boots and seeds a default site and "page" content type.
- User can create a page at `/` or any slug.
- User can add sections with various layouts.
- User can add/edit/delete/reorder blocks within sections.
- Reordering sections and blocks works via drag-and-drop (BlazorSortable).
- Public URL renders exactly what was built in the admin canvas.
- Media uploaded via `DiskStorageProvider` is served from `wwwroot/media`.
- 100% test pass rate for Unit and Integration tests.

## Out of Scope
- User Authentication and Roles.
- Publishing Workflow gates (all pages are live).
- Advanced Media Picker (simple URL/Alt fields for now).
- Rich Text Editor swap (using textarea for MVP).
- Multi-site routing (one default site).
