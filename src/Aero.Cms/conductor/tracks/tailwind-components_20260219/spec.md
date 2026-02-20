# Track Specification: Tailwind Example Blazor Components

## Overview
Create a set of example Blazor components using Tailwind CSS that can be used in the component editor and rendered on CMS published pages. These components will serve as visual examples for product users while following the existing block system pattern.

## Functional Requirements

### 1. Component Types
- **LoginBlock**: Authentication form with username/email and password fields
- **RegisterBlock**: User registration form with validation
- **ForgotPasswordBlock**: Password recovery form
- **HeroBlock2**: Hero section with title, subtitle, and call-to-action (named "layout2" if HeroBlock exists)
- **OneColumnRowBlock**: Single column layout container
- **TwoColumnRowBlock**: Two-column equal width layout
- **ThreeColumnRowBlock**: Three-column equal width layout  
- **FourColumnRowBlock**: Four-column equal width layout
- **MarkdownRendererBlock**: Renders markdown content from database property

### 2. Block System Integration
- All components must extend the existing `ContentBlock` base class
- Column components must implement `ICompositeContentBlock` interface
- Follow existing serialization/deserialization patterns
- Use "layout2" naming convention for any duplicate existing block types
- **CRITICAL**: Do not modify or overwrite existing content blocks - only add new blocks

### 3. Authentication Components
- Support both mock mode (for testing/visual purposes) and real authentication mode
- Include form validation with error state display
- Use existing ASP.NET Identity system when in production mode
- Display appropriate success/error messages

### 4. Column Layout Components
- Support variable number of child content blocks
- Distribute children equally across columns
- Include responsive breakpoints (stack vertically on mobile)
- Maintain consistent spacing and alignment

### 5. Markdown Rendering
- Use `Markdig` library for markdown parsing
- Render markdown content from block property stored in database
- Support common markdown features (headings, lists, code blocks, etc.)
- Apply Tailwind typography styles

### 6. Tailwind CSS Integration
- Include Tailwind CSS via CDN link in layout
- Use Tailwind utility classes for styling
- Ensure components work with default Tailwind configuration

## Non-Functional Requirements

### 1. Technology Stack
- Pure Blazor/C# implementation (no JavaScript dependencies)
- Use existing `Aero.CMS.Core` project structure
- Follow existing testing patterns with NSubstitute and Shouldly

### 2. Responsive Design
- All components must be fully responsive
- Column layouts must stack vertically on mobile devices
- Forms must be usable on all screen sizes

### 3. Accessibility
- Form components must include proper ARIA labels
- Error messages must be accessible to screen readers
- Color contrast must meet WCAG AA standards

### 4. Performance
- No external JavaScript dependencies
- Minimal CSS beyond Tailwind utilities
- Efficient markdown parsing for large content

## Acceptance Criteria

1. All 9 component types exist as `ContentBlock` subclasses
2. Components render correctly in component editor preview
3. Column components correctly distribute child blocks
4. Authentication forms validate input and show appropriate messages
5. Markdown content renders with proper HTML output
6. All components are fully responsive
7. No build warnings or errors when integrated
8. Existing CMS functionality remains unchanged

## Out of Scope

1. Custom Tailwind configuration or build process
2. JavaScript-based interactivity beyond Blazor
3. Advanced authentication features (OAuth, social login)
4. Custom markdown extensions beyond standard Markdig
5. Theme customization or dark mode support