# Specification: Blog MVC Views Implementation

## Overview
Implement the MVC views and supporting ViewModels for the `BlogWebController` located in the `Electra.Web.BlogEngine` library. The logic and default views will be encapsulated within the `Electra.Web.BlogEngine` project (RCL). Additionally, integrate vector-search capabilities for "Related Articles" functionality.

Crucially, this track includes an MSBuild automation task to copy specific view templates from the library to the host application (`microbians.io.web`) upon build. This copy operation must be **safe**—it should ensure the files exist in the host project but **must not overwrite** them if the user has already customized them.

## Functional Requirements
1.  **ViewModels (in `Electra.Web.BlogEngine`):**
    -   **`BlogIndexViewModel`**:
        -   List of articles (Title, Excerpt, Date, Author, Thumbnail URL).
        -   Pagination metadata.
        -   SEO metadata (Meta Title, Description, OpenGraph).
        -   Featured Article property.
    -   **`ArticleViewModel`**:
        -   Core content: Title, Content (HTML), Date, Author.
        -   Tags, Categories, and Series information.
        -   Navigation: Previous/Next links.
        -   **Related Articles**: Populated via vector search + tag matching.
        -   SEO/Social metadata.

2.  **Controller Logic (`BlogWebController`):**
    -   Implement actions for `Index` and `Article`.
    -   Handle routing (e.g., `/blog`, `/blog/{slug}`).
    -   **Related Articles Logic**: Use RavenDB Vector Search combined with Tags/Series context to populate related items.

3.  **Views (RCL in `Electra.Web.BlogEngine`):**
    -   **Source Templates:**
        -   `blogarticle.cshtml` (located in library root).
        -   `blogindex.html` (located in library root).
    -   **Components (Razor Partial Views or ViewComponents):**
        -   `_FeaturedArticle.cshtml`
        -   `_RecommendedArticles.cshtml`
        -   `_RelatedArticles.cshtml` (Vector search results).
    -   Style: Fully styled using **Tailwind CSS**.

4.  **MSBuild Automation (Safe Copy Task):**
    -   Create a generic MSBuild `<Target>` in `Electra.Web.BlogEngine.csproj`.
    -   **Trigger:** Runs automatically on Build.
    -   **Source 1:** `$(MSBuildProjectDirectory)/blogarticle.cshtml` -> **Dest:** `../[WEB_PROJECT_NAME]/Areas/Blog/Views/BlogArticle.cshtml`.
    -   **Source 2:** `$(MSBuildProjectDirectory)/blogindex.html` -> **Dest:** `../[WEB_PROJECT_NAME]/Areas/Blog/Views/Blog/Index.cshtml`.
    -   **Logic:**
        -   Create missing directories.
        -   **Condition:** Only copy if the destination file does **NOT** exist (`Condition="!Exists(...)"`).
    -   **Goal:** Provide a default starting point for the user without destroying their work on subsequent builds.

5.  **Host Integration:**
    -   The host app (`microbians.io.web`) will reference `Electra.Web.BlogEngine`.
    -   The views safely copied by the MSBuild task will be the primary views rendered by the controller.

## Non-Functional Requirements
-   **Safety**: The MSBuild task must never overwrite existing files in the host project.
-   **Extensibility**: Host app has full control over the views once they are initialized.
-   **Search Performance**: Vector similarity search for related articles must be performant.

## Acceptance Criteria
-   Building `Electra.Web.BlogEngine` copies `blogarticle.cshtml` and `blogindex.html` to the web project **only if they don't exist**.
-   Subsequent builds do not revert changes made to the files in the web project.
-   `/blog` renders the `Index.cshtml`.
-   `/blog/{slug}` renders the `BlogArticle.cshtml`.
-   "Related Articles" section functions using RavenDB vector search.
-   All ViewModels and Controller actions are implemented and tested.
