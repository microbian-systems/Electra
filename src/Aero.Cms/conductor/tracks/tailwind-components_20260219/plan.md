# Track Plan: Tailwind Example Blazor Components

**Track ID:** tailwind-components_20260219
**Phases:** 17-19

---

## Phase 17: Authentication Blocks

- [x] Task: Create LoginBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/LoginBlock.cs
    - [ ] BlockType = "loginBlock"
    - [ ] Properties: Title, ShowForgotPasswordLink, RedirectUrl
    - [ ] Support mock mode vs real authentication mode

- [x] Task: Create RegisterBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/RegisterBlock.cs
    - [ ] BlockType = "registerBlock"
    - [ ] Properties: Title, RequireEmailConfirmation, TermsUrl, PrivacyUrl
    - [ ] Include validation properties

- [x] Task: Create ForgotPasswordBlock
    - [x] Create file: Aero.CMS.Core/Content/Models/Blocks/ForgotPasswordBlock.cs
    - [ ] BlockType = "forgotPasswordBlock"
    - [ ] Properties: Title, SuccessMessage, ErrorMessage

- [x] Task: Write AuthenticationBlocks unit tests
    - [x] Create file: Aero.CMS.Tests.Unit/Content/AuthenticationBlocksTests.cs
    - [ ] Test: LoginBlock.Type == "loginBlock"
    - [ ] Test: LoginBlock.Title getter/setter round-trips
    - [ ] Test: RegisterBlock.RequireEmailConfirmation defaults to true
    - [ ] Test: ForgotPasswordBlock.SuccessMessage returns default when absent
    - [ ] Test: All blocks have non-empty Guid Id

- [ ] Task: Create LoginBlock Blazor component
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/LoginBlock.razor
    - [ ] Use Tailwind CSS classes for styling
    - [ ] Include form validation with error states
    - [ ] Support mock mode (display success/error messages without real auth)

- [ ] Task: Create RegisterBlock Blazor component
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/RegisterBlock.razor
    - [ ] Use Tailwind CSS classes for styling
    - [ ] Include validation for required fields
    - [ ] Display appropriate success/error messages

- [ ] Task: Create ForgotPasswordBlock Blazor component
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/ForgotPasswordBlock.razor
    - [ ] Use Tailwind CSS classes for styling
    - [ ] Simple email input form
    - [ ] Display success/error messages

- [ ] Task: Verify Phase 17 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~AuthenticationBlocks"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 17: Authentication Blocks' (Protocol in workflow.md)

---

## Phase 18: Layout Blocks

- [ ] Task: Create HeroBlock2
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/HeroBlock2.cs
    - [ ] BlockType = "heroBlock2"
    - [ ] Properties: Title, Subtitle, CallToActionText, CallToActionUrl, BackgroundColor, TextColor
    - [ ] Follow "layout2" naming convention

- [ ] Task: Create OneColumnRowBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/OneColumnRowBlock.cs
    - [ ] BlockType = "oneColumnRowBlock"
    - [ ] Implement ICompositeContentBlock
    - [ ] Properties: Padding, Gap, BackgroundColor
    - [ ] AllowedChildTypes = empty array (allow any)
    - [ ] AllowNestedComposites = true
    - [ ] MaxChildren = null (unlimited)

- [ ] Task: Create TwoColumnRowBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/TwoColumnRowBlock.cs
    - [ ] BlockType = "twoColumnRowBlock"
    - [ ] Implement ICompositeContentBlock
    - [ ] Properties: Column1Width, Column2Width, Gap, ResponsiveBreakpoint
    - [ ] AllowedChildTypes = empty array
    - [ ] AllowNestedComposites = true
    - [ ] MaxChildren = null

- [ ] Task: Create ThreeColumnRowBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/ThreeColumnRowBlock.cs
    - [ ] BlockType = "threeColumnRowBlock"
    - [ ] Implement ICompositeContentBlock
    - [ ] Properties: EqualColumns (bool), Gap, ResponsiveBreakpoint
    - [ ] AllowedChildTypes = empty array
    - [ ] AllowNestedComposites = true
    - [ ] MaxChildren = null

- [ ] Task: Create FourColumnRowBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/FourColumnRowBlock.cs
    - [ ] BlockType = "fourColumnRowBlock"
    - [ ] Implement ICompositeContentBlock
    - [ ] Properties: EqualColumns (bool), Gap, ResponsiveBreakpoint
    - [ ] AllowedChildTypes = empty array
    - [ ] AllowNestedComposites = true
    - [ ] MaxChildren = null

- [ ] Task: Write LayoutBlocks unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/LayoutBlocksTests.cs
    - [ ] Test: HeroBlock2.Type == "heroBlock2"
    - [ ] Test: OneColumnRowBlock implements ICompositeContentBlock
    - [ ] Test: TwoColumnRowBlock.Column1Width defaults to "50%"
    - [ ] Test: ThreeColumnRowBlock.EqualColumns defaults to true
    - [ ] Test: FourColumnRowBlock.ResponsiveBreakpoint defaults to "md"
    - [ ] Test: All column blocks have empty AllowedChildTypes array
    - [ ] Test: All column blocks have AllowNestedComposites = true

- [ ] Task: Create HeroBlock2 Blazor component
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/HeroBlock2.razor
    - [ ] Use Tailwind CSS classes for responsive hero section
    - [ ] Apply background and text colors from properties
    - [ ] Render call-to-action button if provided

- [ ] Task: Create ColumnRow Blazor components
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/OneColumnRowBlock.razor
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/TwoColumnRowBlock.razor
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/ThreeColumnRowBlock.razor
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/FourColumnRowBlock.razor
    - [ ] Use Tailwind grid classes (grid, grid-cols-1, grid-cols-2, etc.)
    - [ ] Implement responsive stacking on mobile (stack vertically)
    - [ ] Distribute child blocks equally across columns
    - [ ] Apply gap and padding from properties

- [ ] Task: Verify Phase 18 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~LayoutBlocks"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 18: Layout Blocks' (Protocol in workflow.md)

---

## Phase 19: Markdown Renderer and Integration

- [ ] Task: Create MarkdownRendererBlock
    - [ ] Create file: Aero.CMS.Core/Content/Models/Blocks/MarkdownRendererBlock.cs
    - [ ] BlockType = "markdownRendererBlock"
    - [ ] Properties: MarkdownContent, UseTypographyStyles, MaxWidth
    - [ ] Use Markdig library for parsing (add NuGet reference if needed)

- [ ] Task: Write MarkdownRendererBlock unit tests
    - [ ] Create file: Aero.CMS.Tests.Unit/Content/MarkdownRendererBlockTests.cs
    - [ ] Test: MarkdownRendererBlock.Type == "markdownRendererBlock"
    - [ ] Test: MarkdownContent getter/setter round-trips
    - [ ] Test: UseTypographyStyles defaults to true
    - [ ] Test: MaxWidth defaults to "prose"

- [ ] Task: Create MarkdownRendererBlock Blazor component
    - [ ] Create file: Aero.CMS.Web/Components/Blocks/MarkdownRendererBlock.razor
    - [ ] Use Markdig to parse markdown to HTML
    - [ ] Apply Tailwind typography classes (prose, prose-lg, etc.)
    - [ ] Sanitize HTML output for security
    - [ ] Apply MaxWidth property

- [ ] Task: Add Tailwind CSS CDN to layout
    - [ ] Update main layout to include Tailwind CSS CDN link
    - [ ] Ensure components work with default Tailwind configuration

- [ ] Task: Create integration tests for block rendering
    - [ ] Create file: Aero.CMS.Tests.Integration/Components/BlockRenderingTests.cs
    - [ ] Test: LoginBlock component renders without errors
    - [ ] Test: Column blocks distribute children correctly
    - [ ] Test: MarkdownRendererBlock renders basic markdown to HTML
    - [ ] Test: All Blazor components compile successfully

- [ ] Task: Verify Phase 19 gate
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit --filter "FullyQualifiedName~MarkdownRenderer"`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration --filter "FullyQualifiedName~BlockRendering"`
    - [ ] Confirm all pass, zero failures

- [ ] Task: Conductor - User Manual Verification 'Phase 19: Markdown Renderer and Integration' (Protocol in workflow.md)

---

## Final Track Verification

- [ ] Task: Verify all new blocks integrate with existing system
    - [ ] Confirm no modifications to existing blocks
    - [ ] Test polymorphic serialization with new blocks
    - [ ] Ensure block registry can discover new blocks

- [ ] Task: Final build verification
    - [ ] Run `dotnet build Aero.CMS.sln`
    - [ ] Zero errors, zero warnings

- [ ] Task: Final test verification
    - [ ] Run `dotnet test Aero.CMS.Tests.Unit`
    - [ ] Run `dotnet test Aero.CMS.Tests.Integration`
    - [ ] All tests pass, no skipped tests

- [ ] Task: Update tracks file to mark track as complete
    - [ ] Update `conductor/tracks.md` status to `[x]`
    - [ ] Commit with message `chore(conductor): Mark track 'Tailwind Example Blazor Components' as complete`