# Specification: Dynamic ASP.NET Core CMS (Electra.Cms)

## 1. Overview
Design and implement a hybrid CMS within the `Electra.Cms` project that combines editor flexibility with developer control. The system must support multi-site hosting, use RavenDB as the document store, and employ a block-based page composition model. It avoids runtime compilation and strictly enforces strong typing at block boundaries while allowing dynamic page-level metadata.

## 2. Core Architecture
-   **Platform:** ASP.NET Core (MVC for public routing, Razor Pages for admin, Blazor Server for preview, Blazor WASM for islands).
-   **Database:** RavenDB (Sites, Pages, Blocks, Users/Roles, Indexes).
-   **Runtime:** Custom CMS runtime handling routing, block rendering, plugin discovery, and output caching.
-   **Client-Side:** HTMX, AlpineJS, Lit, Hydro (No SPA lock-in).

## 3. Data Models (RavenDB)

### SiteDocument (Multi-Site)
```json
{
  "Id": "sites/1",
  "Name": "My Site",
  "Hostnames": ["example.com"],
  "DefaultCulture": "en-US",
  "Theme": "Default",
  "Settings": {},
  "Version": 1
}
```

### PageDocument
```json
{
  "Id": "pages/1",
  "SiteId": "sites/1",
  "Slug": "about-us",
  "FullUrl": "/about-us",
  "Template": "Standard",
  "Metadata": { "Title": "About", "SEO": "..." },
  "DynamicData": { ... }, // ⚠️ ONLY place for dynamic data
  "Blocks": [ ... ],
  "PublishedState": "Published",
  "Version": 1,
  "LastModifiedUtc": "..."
}
```

### BlockDocument
```json
{
  "Type": "Hero",
  "Version": 1,
  "Data": { ... } // Strongly structured JSON
}
```

## 4. Rendering System
-   **Context:** `PageRenderContext` containing Site, Page, Blocks, and Dynamic PageModel.
-   **Strategy:** No page-specific C# models. Loop through `Blocks` collection and render via Razor partials or Blazor components.
-   **Discovery:** Blocks discovered via assembly scanning.

## 5. Routing & URL Resolution
-   **Flow:** Request -> Host Matching -> Site Resolution -> URL Lookup (RavenDB Index) -> PageDocument -> Render.
-   **Index:** `Pages_BySiteAndUrl` (SiteId, FullUrl, PageId, PublishedState).
-   **Performance:** O(log n) resolution.

## 6. Output Caching & ETags
-   **Configuration:** `CmsOptions` (EnableOutputCaching, EnableEtags, DefaultCacheDuration).
-   **Logic:**
    -   ETag derived from Page Version + LastModified + Site Version.
    -   Support `If-None-Match` returning `304 Not Modified`.
    -   CDN-friendly headers.

## 7. Extensibility (Plugins)
-   Support for plugging in Block definitions, Razor views, Blazor components, and Scripts/Styles.
-   Common use cases: Analytics (GA, Clarity), Chat widgets, etc.

## 8. Built-in Blocks (WebAwesome)
-   **Required:** Hero, Heading, Rich Text, Markdown, Image, Gallery, Video, Button/CTA, Card Grid, Accordion, Tabs, Modal, Form, Table, Embed, Script Injection.
-   **Structure:** Schema + Validation + Razor Partial + Optional Blazor Component.

## 9. Editor UI
-   **Tech:** Razor Pages (Admin), HTMX (Interactions), AlpineJS (State).
-   **Features:** Tree navigation, Drag-and-drop blocks, Inline editing, Site preview, Draft/Published diff.

## 10. Non-Negotiable Constraints
-   ❌ No runtime Roslyn/C# compilation.
-   ❌ No dynamic-only blocks (Schema required).
-   ❌ No static HTML generation by default.
-   ❌ No SPA-only rendering.
-   ✅ Output caching must be toggleable.
-   ✅ Multi-tenant support is mandatory.
