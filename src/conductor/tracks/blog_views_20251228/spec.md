# Specification: Blog MVC Views Implementation

## Overview
Implement the MVC views and supporting ViewModels for the `BlogWebController` located in the `Electra.Web.BlogEngine` library. The logic and default views will be encapsulated within the `Electra.Web.BlogEngine` project (Razor Class Library - RCL). Additionally, integrate vector-search capabilities for "Related Articles" functionality.

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
    -   Handle routing (e.g., `/blog`, `/blog/article/{slug}`).
    -   **Related Articles Logic**: Use RavenDB Vector Search combined with Tags/Series context to populate related items.

3.  **Views (RCL in `Electra.Web.BlogEngine`):**
    -   **Views:**
        -   `BlogArticle.cshtml`
        -   `BlogIndex.cshtml`
    -   **Components (Razor Partial Views or ViewComponents):**
        -   `_FeaturedArticle.cshtml`
        -   `_RecommendedArticles.cshtml`
        -   `_RelatedArticles.cshtml` (Vector search results).
    -   Style: Fully styled using **Tailwind CSS**.

4.  **Host Integration:**
    -   The host app (`microbians.io.web`) will reference `Electra.Web.BlogEngine`.
    -   The controller will render views directly from the Razor Class Library.

## Non-Functional Requirements
-   **Extensibility**: The system should allow for easy addition of new blog features or metadata.
-   **Search Performance**: Vector similarity search for related articles must be performant.

## Acceptance Criteria
-   `/blog` renders the `BlogIndex.cshtml` from the RCL.
-   `/blog/article/{slug}` renders the `BlogArticle.cshtml` from the RCL.
-   "Related Articles" section functions using RavenDB vector search (or tag-based fallback).
-   All ViewModels and Controller actions are implemented and tested.