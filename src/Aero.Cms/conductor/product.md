# Aero CMS - Product Guide

## Initial Concept

A .NET 10 ASP.NET CMS combining Blazor Server, Razor Pages, and MVC with RavenDB persistence, designed for AI agent spec-driven development.

## Product Vision

Aero CMS is a modern, modular content management system built on .NET 10 that enables developers to build content-rich applications with:
- **Block-based content editing** with a flexible block hierarchy
- **Publishing workflow** with approval states and versioning
- **SEO optimization** built into the content lifecycle
- **Plugin extensibility** via AssemblyLoadContext
- **Passkey authentication** for modern security

## Target Users

1. **Content Authors** - Create and manage content through intuitive block-based editing
2. **Content Editors** - Review, approve, and publish content through workflow states
3. **Developers** - Extend the CMS with custom blocks, plugins, and integrations
4. **Site Administrators** - Manage users, permissions, and system configuration

## Core Features

### Content Management
- Document-based content storage with RavenDB
- Flexible content type definitions
- Block-based content composition (RichText, Markdown, Image, Hero, Quote, Grid, Div)
- Hierarchical content organization (parent/child relationships)
- Multi-language support

### Publishing Workflow
- Draft → PendingApproval → Approved → Published → Expired
- Approval requirements per content type
- Scheduled publishing with expiration

### Media Management
- Pluggable media providers (Disk, Cloud)
- Image metadata tracking
- Folder organization

### SEO
- Built-in SEO checks (title, description, word count, heading structure)
- Redirect management (301/302)
- Search text extraction from content blocks

### Identity & Security
- ASP.NET Identity with RavenDB backing
- Passkey/FIDO2 authentication
- Role-based permissions
- User ban management

### Extensibility
- Plugin system with hot-loading
- Block registry for component mapping
- Save hook pipeline for cross-cutting concerns
- Rich text editor abstraction

## Success Metrics

- 80%+ code coverage across all core modules
- All unit and integration tests passing
- Clean architecture with no circular dependencies
- Type-safe polymorphic serialization
