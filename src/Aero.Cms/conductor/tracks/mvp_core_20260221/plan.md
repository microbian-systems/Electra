# Implementation Plan: MVP Core Implementation

**Track ID:** mvp_core_20260221
**Phases:** MVP-1 to MVP-7

---

## Phase MVP-1: Site Document & Bootstrapping

- [ ] Task: Create SiteDocument model
    - [ ] Create file: Aero.CMS.Core/Site/Models/SiteDocument.cs
    - [ ] Properties: Name, BaseUrl, DefaultLayout, Description, IsDefault, FaviconMediaId, LogoMediaId, FooterText

- [ ] Task: Create SiteRepository
    - [ ] Create file: Aero.CMS.Core/Site/Data/SiteRepository.cs
    - [ ] Implement ISiteRepository interface
    - [ ] Methods: GetDefaultAsync, GetAllAsync

- [ ] Task: Create Media Providers
    - [ ] Create file: Aero.CMS.Core/Media/Providers/NullMediaProvider.cs
    - [ ] Create file: Aero.CMS.Core/Media/Providers/DiskStorageProvider.cs (as per Phase 14 spec)

- [ ] Task: Create SiteBootstrapService
    - [ ] Create file: Aero.CMS.Core/Site/Services/SiteBootstrapService.cs
    - [ ] Implement IHostedService
    - [ ] Seed default site and "page" content type if absent

- [ ] Task: Write Site and Bootstrap tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Site/SiteDocumentTests.cs
    - [ ] Create file: Aero.CMS.Tests.Integration/Site/SiteRepositoryTests.cs
    - [ ] Create file: Aero.CMS.Tests.Integration/Site/SiteBootstrapServiceTests.cs

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-1: Site Document & Bootstrapping' (Protocol in workflow.md)

---

## Phase MVP-2: Section and Column Blocks (Layout)

- [ ] Task: Create SectionLayout enum
    - [ ] Create file: Aero.CMS.Core/Content/Models/SectionLayout.cs

- [ ] Task: Create SectionBlock and ColumnBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/SectionBlock.cs (ICompositeContentBlock)
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/ColumnBlock.cs (ICompositeContentBlock)
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/HtmlBlock.cs (Leaf)

- [ ] Task: Create SectionService
    - [ ] Create file: Aero.CMS.Core/Content/Services/SectionService.cs
    - [ ] Logic for adding/removing/moving sections and blocks

- [ ] Task: Write Layout and SectionService tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/SectionBlockTests.cs
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/SectionServiceTests.cs

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-2: Section and Column Blocks (Layout)' (Protocol in workflow.md)

---

## Phase MVP-3: Page Service & Repository

- [ ] Task: Implement PageService
    - [ ] Create file: Aero.CMS.Core/Content/Services/PageService.cs
    - [ ] Methods: GetPagesForSiteAsync, GetBySlugAsync, CreatePageAsync, SavePageAsync, DeletePageAsync, GetByIdAsync

- [ ] Task: Write PageService tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/PageServiceTests.cs

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-3: Page Service & Repository' (Protocol in workflow.md)

---

## Phase MVP-4: Public Rendering & Block Views

- [ ] Task: Create Public Layout and Views
    - [ ] Create file: Aero.CMS.Web/Layouts/PublicLayout.razor
    - [ ] Create file: Aero.CMS.Web/Pages/PublicPageView.razor (IContentView)
    - [ ] Create file: Aero.CMS.Web/Pages/EntryPage.razor (Dispatcher)

- [ ] Task: Create Block View Components
    - [ ] Create file: Aero.CMS.Web/Blocks/SectionBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/ColumnBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/RichTextBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/HeroBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/QuoteBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/MarkdownBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/HtmlBlockView.razor
    - [ ] Create file: Aero.CMS.Web/Blocks/ImageBlockView.razor

- [ ] Task: Register blocks in Program.cs
    - [ ] Wire up IBlockRegistry in Aero.CMS.Web/Program.cs

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-4: Public Rendering & Block Views' (Protocol in workflow.md)

---

## Phase MVP-5: Admin UI & Block Canvas

- [ ] Task: Implement Admin Layout and Shell
    - [ ] Create file: Aero.CMS.Components/Admin/Layout/AdminLayout.razor
    - [ ] Create file: Aero.CMS.Components/Admin/Layout/AdminNavBar.razor
    - [ ] Create file: Aero.CMS.Web/wwwroot/css/admin.css

- [ ] Task: Implement Page and Site Management UIs
    - [ ] Create file: Aero.CMS.Components/Admin/PageSection/PageList.razor
    - [ ] Create file: Aero.CMS.Components/Admin/PageSection/NewPageDialog.razor
    - [ ] Create file: Aero.CMS.Components/Admin/PageSection/DeletePageDialog.razor
    - [ ] Create file: Aero.CMS.Components/Admin/SiteSection/SiteEditor.razor

- [ ] Task: Implement Block Canvas and Editors
    - [ ] Create file: Aero.CMS.Components/Admin/BlockCanvas/BlockEditContext.cs
    - [ ] Create file: Aero.CMS.Components/Admin/BlockCanvas/BlockWrapper.razor
    - [ ] Create file: Aero.CMS.Components/Admin/BlockCanvas/AddBlockButton.razor
    - [ ] Create file: Aero.CMS.Components/Admin/BlockCanvas/AddSectionButton.razor
    - [ ] Create file: Aero.CMS.Components/Admin/PageSection/PageEditor.razor
    - [ ] Create all block editors in Aero.CMS.Components/Admin/BlockEditors/

- [ ] Task: Implement Drag & Drop with BlazorSortable
    - [ ] Add BlazorSortable dependency
    - [ ] Integrate SortableList into BlockCanvas.razor for sections and columns

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-5: Admin UI & Block Canvas' (Protocol in workflow.md)

---

## Phase MVP-6: Wiring & End-to-End Verification

- [ ] Task: Final Wiring in Web Project
    - [ ] Update Aero.CMS.Web/Components/App.razor router
    - [ ] Update Aero.CMS.Web/Program.cs service registrations
    - [ ] Update Aero.CMS.Web/wwwroot/css/site.css baseline styles

- [ ] Task: E2E Checklist Verification
    - [ ] Perform manual verification as per MVP-6.2 checklist

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-6: Wiring & End-to-End Verification' (Protocol in workflow.md)

---

## Phase MVP-7: Final Test Coverage & Polish

- [ ] Task: Finalize Tests
    - [ ] Create file: Aero.CMS.Tests.Integration/Content/PageCycleTests.cs
    - [ ] Verify coverage for all MVP components (>= 80%)

- [ ] Task: Conductor - User Manual Verification 'Phase MVP-7: Final Test Coverage & Polish' (Protocol in workflow.md)
