# Implementation Plan: MerakiUI Full Blazor Conversion

## Phase 1: Interactive Elements (Dropdowns, Modals, Tabs) [ ]
These components require the 4-file pattern with Scoped TypeScript for state/DOM management.

- [x] Task: Convert MerakiUI Dropdown variations 122d1b5
  - Write Tests: `DropdownTests.cs`
  - Implement: `Dropdowns/SimpleDropdown.razor`, `Dropdowns/DropdownWithIcons.razor`, etc.
- [x] Task: Convert MerakiUI Modal variations e9af977
  - Write Tests: `ModalTests.cs`
  - Implement: `Modals/SimpleModal.razor`, `Modals/ModalWithAction.razor`, etc.
- [ ] Task: Convert MerakiUI Tab variations
  - Write Tests: `TabTests.cs`
  - Implement: `Tabs/SimpleTabs.razor`, `Tabs/TabWithIcons.razor`, etc.
- [ ] Task: Conductor - User Manual Verification 'Interactive Elements' (Protocol in workflow.md)

## Phase 2: Content Sections (Cards, Tables) [ ]
- [ ] Task: Convert MerakiUI Card variations
  - Write Tests: `CardTests.cs`
  - Implement: `Cards/ProductCard.razor`, `Cards/ArticleCard.razor`, `Cards/UserCard.razor`, etc.
- [ ] Task: Convert MerakiUI Table variations
  - Write Tests: `TableTests.cs`
  - Implement: `Tables/SimpleTable.razor`, `Tables/TableWithActions.razor`, etc.
- [ ] Task: Conductor - User Manual Verification 'Content Sections' (Protocol in workflow.md)

## Phase 3: Layout & Navigation (Heroes, Sidebars, Footers) [ ]
- [ ] Task: Convert MerakiUI Hero variations
  - Write Tests: `HeroTests.cs`
  - Implement: `Heroes/HeroWithImage.razor`, `Heroes/HeroWithForm.razor`, etc.
- [ ] Task: Convert MerakiUI Sidebar variations
  - Write Tests: `SidebarTests.cs`
  - Implement: `Sidebars/SimpleSidebar.razor`, `Sidebars/SidebarWithIcons.razor`, etc.
- [ ] Task: Convert MerakiUI Footer variations
  - Write Tests: `FooterTests.cs`
  - Implement: `Footers/SimpleFooter.razor`, `Footers/FooterWithColumns.razor`, etc.
- [ ] Task: Conductor - User Manual Verification 'Layout & Navigation' (Protocol in workflow.md)

## Phase 4: Complex Forms & Marketing Sections [ ]
- [ ] Task: Convert MerakiUI Complex Form variations
  - Write Tests: `FormTests.cs`
  - Implement: `Forms/ContactForm.razor`, `Forms/NewsletterForm.razor`, etc.
- [ ] Task: Convert MerakiUI Marketing Section variations
  - Write Tests: `SectionTests.cs`
  - Implement: `Sections/FeatureSection.razor`, `Sections/PricingSection.razor`, `Sections/CTASection.razor`, etc.
- [ ] Task: Conductor - User Manual Verification 'Complex Forms & Marketing Sections' (Protocol in workflow.md)

## Phase 5: Finalization & Documentation [ ]
- [ ] Task: Update `README.md` with new component examples and JS interop notes
- [ ] Task: Final project build and cross-browser/mobile verification
- [ ] Task: Conductor - User Manual Verification 'Finalization & Documentation' (Protocol in workflow.md)
