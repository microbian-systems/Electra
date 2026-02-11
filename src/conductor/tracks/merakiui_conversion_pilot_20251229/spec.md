# Track Specification: MerakiUI Blazor Conversion (Phase 1 - Pilot)

## Overview
This track involves a 1-to-1 conversion of UI components from the `MerakiUI` library (Tailwind CSS/AlpineJS) into Blazor Razor components within the `Electra.MerakiUI` project. This is the first phase (Pilot), establishing the patterns and converting a core set of components.

## Functional Requirements
- **Component Pattern:** Every component must follow a strict file structure:
  - `Category/ComponentName.razor` (Markup)
  - `Category/ComponentName.razor.cs` (Code-behind logic)
  - `Category/ComponentName.razor.css` (Scoped CSS)
  - `Category/ComponentName.razor.ts` (Scoped TypeScript, auto-compiling to .js)
- **Standard Parameters:** All components must expose:
  - `string? Class` (Appended to the root element's class list)
  - `string? Id` (Mapped to the root element's HTML ID)
  - `RenderFragment? ChildContent` (Where applicable for nesting)
- **Variation Handling:** 
  - Most variations from the source will be treated as distinct components to match the MerakiUI conceptual structure.
  - **Exceptions:** `Alerts` (and similar simple CSS-only switches) will use an enum or string parameter to toggle between styles (e.g., Success, Warning, Info).
- **Cleanup:** Source HTML files from MerakiUI must be stripped of `<html>`, `<head>`, and `<body>` tags during conversion.

## Non-Functional Requirements
- **Technology Alignment:** Components must utilize existing project assets for Tailwind CSS and AlpineJS.
- **Maintainability:** Use C# code-behind files (`.razor.cs`) for all logic to keep the `.razor` files focused on markup.

## Acceptance Criteria
- [ ] Pilot components (Alerts, Buttons, Inputs, Navbars) are successfully migrated from `MerakiUI/components/` to `src/Electra.MerakiUI/`.
- [ ] Each component follows the 4-file structure (Razor, C#, CSS, TS).
- [ ] Components compile without errors in the `Electra.MerakiUI` project.
- [ ] TypeScript files correctly target their respective components and are set up for compilation.
- [ ] Standard parameters (`Class`, `Id`, `ChildContent`) are implemented and functional.

## Out of Scope
- Implementation of complex JS interactivity (this will be handled in a later phase).
- Migration of the remaining ~25 component categories (to be handled in subsequent tracks).
