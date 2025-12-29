# Track Plan: Blog MVC Views Implementation

## Phase 1: Foundation & MSBuild Automation [checkpoint: 4917e1b]
Setup the project structure and the automated build tasks.

- [x] Task: Configure MSBuild Target in `Electra.Web.BlogEngine.csproj` for safe file copying to `microbians.io.web` 200a822
- [x] Task: Create initial placeholder `blogarticle.cshtml` and `blogindex.html` in library root 51b00e8
- [x] Task: Verify MSBuild copy logic (ensuring no overwrite if file exists) 51b00e8
- [x] Task: Conductor - User Manual Verification 'Foundation & MSBuild Automation' (Protocol in workflow.md) 4917e1b

## Phase 2: Data Models & ViewModels (TDD)
Implement the ViewModels required for the blog pages and components.

- [ ] Task: Write failing unit tests for `BlogIndexViewModel` and `ArticleViewModel` (including SEO and Featured data)
- [ ] Task: Implement `BlogIndexViewModel` and `ArticleViewModel` in `Electra.Web.BlogEngine`
- [ ] Task: Conductor - User Manual Verification 'Data Models & ViewModels' (Protocol in workflow.md)

## Phase 3: Controller & Related Articles Logic (TDD)
Implement the core logic in the controller, focusing on the vector search integration.

- [ ] Task: Write failing unit tests for `BlogWebController` actions (Index, Article)
- [ ] Task: Implement `BlogWebController` actions and routing
- [ ] Task: Write failing tests for Related Articles logic (RavenDB Vector Search + Tags)
- [ ] Task: Implement Vector Search integration for related articles
- [ ] Task: Conductor - User Manual Verification 'Controller & Related Articles Logic' (Protocol in workflow.md)

## Phase 4: Razor Views & Components
Develop the UI using Tailwind CSS within the Razor Class Library.

- [ ] Task: Implement `_FeaturedArticle.cshtml`, `_RecommendedArticles.cshtml`, and `_RelatedArticles.cshtml` components
- [ ] Task: Refine `blogarticle.cshtml` and `blogindex.html` with full Tailwind CSS styling
- [ ] Task: Ensure all Razor components are correctly discoverable in the RCL
- [ ] Task: Conductor - User Manual Verification 'Razor Views & Components' (Protocol in workflow.md)

## Phase 5: Final Integration & Verification
Final testing of the end-to-end flow.

- [ ] Task: Verify the full flow in `microbians.io.web` (Build -> Copy -> Browser Render)
- [ ] Task: Verify "Safe Copy" (manually modify a file in Web and rebuild to ensure it's not overwritten)
- [ ] Task: Perform final linting and XML documentation sweep
- [ ] Task: Conductor - User Manual Verification 'Final Integration & Verification' (Protocol in workflow.md)
