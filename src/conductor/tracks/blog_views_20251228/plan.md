# Track Plan: Blog MVC Views Implementation

## Phase 1: Foundation & Project Structure
Setup the project structure and initial views.

- [x] Task: Initialize Razor views directory structure in `Electra.Web.BlogEngine`
- [x] Task: Create initial placeholder `BlogArticle.cshtml` and `BlogIndex.cshtml` in `Views/`
- [x] Task: Verify compilation of the Razor Class Library

## Phase 2: Data Models & ViewModels (TDD)
Implement the ViewModels required for the blog pages and components.

- [x] Task: Write failing unit tests for `BlogIndexViewModel` and `ArticleViewModel` (including SEO and Featured data) 3e75e95
- [x] Task: Implement `BlogIndexViewModel` and `ArticleViewModel` in `Electra.Web.BlogEngine` 9cb86d8
- [x] Task: Verify compilation and tests pass 1d6ce28

## Phase 3: Controller & Related Articles Logic (TDD)
Implement the core logic in the controller, focusing on the vector search integration.

- [x] Task: Write unit tests for `BlogWebController` actions (Index, Article)
- [x] Task: Implement `BlogWebController` actions and routing
- [x] Task: Write failing tests for Related Articles logic (RavenDB Vector Search + Tags)
- [x] Task: Implement tag-based matching for related articles (Vector search placeholder added)
- [ ] Task: Conductor - User Manual Verification 'Controller & Related Articles Logic' (Protocol in workflow.md)

## Phase 4: Razor Views & Components
Develop the UI using Tailwind CSS within the Razor Class Library.

- [x] Task: Implement `_FeaturedArticle.cshtml`, `_RecommendedArticles.cshtml`, and `_RelatedArticles.cshtml` components
- [x] Task: Refine `BlogArticle.cshtml` and `BlogIndex.cshtml` with full Tailwind CSS styling
- [x] Task: Ensure all Razor components are correctly discoverable in the RCL
- [x] Task: Conductor - User Manual Verification 'Razor Views & Components' (Protocol in workflow.md)

## Phase 5: Final Integration & Verification
Final testing of the end-to-end flow.

- [x] Task: Verify the full flow in `microbians.io.web` (Build -> Browser Render)
- [x] Task: Perform final linting and XML documentation sweep
- [ ] Task: Conductor - User Manual Verification 'Final Integration & Verification' (Protocol in workflow.md)