# Specification: MerakiUI Showcase Migration

## Overview
Migrate the MerakiUI component showcase page from the main web project to the `Electra.MerakiUI` Razor Class Library (RCL). This page will serve as a living documentation and demonstration area for all converted MerakiUI components.

## Functional Requirements
- **Migration:** Move and adapt `meraki-showcase.cshtml` from `src/microbians.io.web/microbians.io.web/Pages/` to `src/Electra.MerakiUI/Areas/Demo/Pages/`.
- **Content Overwrite:** Replace the placeholder `MerakiShowcase.cshtml` in the target directory with the migrated content.
- **Categorized Sections:** Organize the showcase page into sections based on component categories (e.g., Alerts, Avatars, Blog, Navbars).
- **Demo Content:** For each component category, provide:
  - **Live Instance:** A functional, rendered instance of the component.
  - **Razor Snippet:** The exact code needed to use the component.
  - **Parameter Reference:** A brief description of the primary parameters.
- **Namespace Adjustment:** Update model and using directives to point to `Electra.MerakiUI`.

## Non-Functional Requirements
- **Consistency:** The page layout should follow the established "Technical & Dark" visual identity of Electra.
- **Responsiveness:** Ensure the showcase page works well on mobile and desktop.

## Acceptance Criteria
- The showcase page is accessible at its new route within the RCL.
- All component categories implemented in Batch 1, 2, and 3 are clearly demoed.
- Code snippets match the actual component parameters.
- The project builds successfully without missing reference errors.

## Out of Scope
- Adding new UI components not previously created.
- Implementing full backend logic for interactive form demos (logic is limited to component state).
