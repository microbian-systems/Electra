# Specification: MerakiUI Full Blazor Conversion

## Overview
This track follows up on the successful pilot conversion of MerakiUI components. The goal is to convert **all remaining** component categories from the MerakiUI source library into reusable Blazor components within the `Electra.MerakiUI` Razor Class Library.

## Functional Requirements
- **Comprehensive Conversion:** Implement all remaining component categories from the MerakiUI source, including but not limited to:
  - Cards
  - Dropdowns
  - Footers
  - Forms (Complex/Grouped)
  - Heroes
  - Sections
  - Sidebars
  - Tables
  - Modals
  - Tabs
- **Blazor Integration:**
  - Components must support standard parameters via `MerakiComponentBase`.
  - Input-related components must support `@bind-Value` data binding.
  - Interactive components (Dropdowns, Modals, Tabs) must use the 4-file scoped TypeScript pattern for state management and DOM manipulation where Blazor state is insufficient or less performant.
- **Pattern Adherence:** Maintain the strict 4-file structure for every component:
  - `.razor`: Markup
  - `.razor.cs`: Code-behind
  - `.razor.css`: Scoped Styles
  - `.razor.ts`: Scoped TypeScript

## Non-Functional Requirements
- **Consistency:** Visuals must match the MerakiUI Tailwind CSS originals exactly.
- **Maintainability:** Use idiomatic C# and Blazor patterns.
- **Performance:** Efficient rendering and minimal JS interop where possible.

## Acceptance Criteria
- All MerakiUI component categories are represented in `Electra.MerakiUI`.
- Every component builds successfully and passes `Bunit` tests for basic rendering.
- Documentation in `src/Electra.MerakiUI/README.md` is updated if new patterns emerge.
- Sample usage is verified for complex interactive components.

## Out of Scope
- Creating new UI designs not present in the original MerakiUI library.
- Implementing backend logic beyond component state and binding.
