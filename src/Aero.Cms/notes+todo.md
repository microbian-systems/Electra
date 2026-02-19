*** NOTE *** -  any line item with a 💥 or more means really need to make use of it

💥 on content pages add a "Summarize w/ AI" as found here: https://www.telerik.com/blogs/creating-drag-drop-listboxes-blazor
💥 on content pages add a "share bar" like on the right side of the page here: https://www.telerik.com/blogs/creating-drag-drop-listboxes-blazor


if I didn't mention it earlier, we also need the new .net 10 passkey support. 
add ai content generation, web site SEO analysis (borrow from zaubercms - I will get source cod e later).  Pull web content from other sources.  add automatic content generation (published after approval) 

https://gist.github.com/SteveSandersonMS/ec232992c2446ab9a0059dd0fbc5d0c3
https://learn.microsoft.com/en-us/aspnet/core/blazor/advanced-scenarios?view=aspnetcore-10.0
https://www.telerik.com/blogs/how-to-render-blazor-components-dynamically
https://docs.devexpress.com/Blazor/401753/common-concepts/customize-and-reuse-components
https://www.syncfusion.com/blogs/post/how-to-dynamically-render-a-component-in-a-blazor-application
https://docs.devexpress.com/Blazor/401753/common-concepts/customize-and-reuse-components
https://www.daveabrock.com/2021/04/08/blazor-dynamic-component/
https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dynamiccomponent?view=aspnetcore-10.0
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering?view=aspnetcore-10.0


--------- premade components ---------

💥 https://chrissainty.com/investigating-drag-and-drop-with-blazor/ 💥  -  (this has no js)
https://github.com/Postlagerkarte/blazor-dragdrop - no js
💥💥💥 https://github.com/the-urlist/blazorsortable 💥💥💥 - (this makes use of js but is very smooth)



(maybe usable? nice zones layout) - https://fluentui-blazor.azurewebsites.net/Drag


Use weboptimizer to bundle and minify css and js files (prod only)

Introduced in .NET 6, the DynamicComponent is now the preferred way to render a component by its type without manually dealing with the RenderTreeBuilder. 
Microsoft Learn
Microsoft Learn
 +1
Usage: You pass it a Type and an optional Dictionary<string, object> for parameters.
Benefit: Much cleaner and less error-prone than manual tree building.


<DynamicComponent Type="@selectedType" Parameters="@myParameters" />

@code {
    private Type? componentType;
    private Dictionary<string, object> componentParameters = new();

    protected override async Task OnInitializedAsync() {
        // 1. Fetch from Database (Example: Get "MyNamespace.MyComponent")
        var dbResult = await MyDatabaseService.GetPageConfigAsync(); 

        // 2. Resolve the string name to a real Type
        componentType = Type.GetType(dbResult.ComponentName);

        // 3. Deserialize JSON parameters into a Dictionary
        componentParameters = JsonSerializer.Deserialize<Dictionary<string, object>>(dbResult.JsonParams) 
                              ?? new();
    }
}

Summary Comparison
Method 	Best For	Complexity
RenderTreeBuilder	Framework-level libraries or extreme performance	High (Error-prone)
RenderFragment	Reusable UI snippets or simple dynamic blocks	Medium
DynamicComponent	Most application-level dynamic rendering	Low (Cleanest)

For a CMS with drag-and-drop functionality, you should use the DynamicComponent for the majority of the implementation, potentially supplemented by RenderFragment for layout wrappers. 

1. The Core: DynamicComponent
This is the modern standard for CMS-style applications. It is specifically designed to render components when the type and parameters are only known at runtime (e.g., loaded from a database after a user drops a widget onto a page). 

Why it's best for CMS: It avoids complex switch statements or massive if/else blocks to determine which component to render.
Ease of Use: You simply provide the Type and a Dictionary<string, object> of parameters.
State Management: It handles the component lifecycle and parameter updates more reliably than manual tree building. 

2. The Layout: RenderFragment
While DynamicComponent handles the "widgets" themselves, you use RenderFragment to define the drop zones or "containers" that hold those widgets.
Droppable Areas: You can create a "Section" component that accepts a ChildContent parameter of type RenderFragment.
Nesting: This allows you to nest dynamic components inside layouts (e.g., a "Two Column" layout containing two different DynamicComponent instances). 

3. Why avoid RenderTreeBuilder?
Unless you are building the underlying drag-and-drop library itself, avoid manual RenderTreeBuilder logic.
Sequence Numbers: You must manually manage "sequence numbers" for every element. If these aren't hard-coded and consistent, Blazor's "diffing" algorithm will fail, causing the entire UI to re-render or lose state (like focus or scroll position) every time a user moves an item.
Complexity: It is significantly more verbose and harder to maintain than the Razor-based DynamicComponent. 

Recommended Architecture
Storage: Save the page structure in your database as a list of objects, each containing a ComponentType (string) and Parameters (JSON).
Mapping: On page load, convert those strings to actual Type objects using Type.GetType().
Rendering: Use a @foreach loop in your Razor page to iterate through your list and render each one using <DynamicComponent />.



----------- suggestions  given the craftcms suggestion +  the following diagrams, etc --------- 

Page
  └── BlockCanvas (root)
        ├── HeroBlock (leaf)
        ├── DivBlock (composite) ◄── what we're building
        │     ├── RichTextBlock (leaf)
        │     ├── ImageBlock (leaf)
        │     └── DivBlock (composite, nested)
        │           └── QuoteBlock (leaf)
        └── FAQBlock (leaf)


We want several types of pages: 

1. markdown (pure markdown)
2. blank page 

We want several types of containers
1. horizontal row (width adjustable)
2. vertical column (height adjustable)


----------------------------------------------------------------------- 

I have prebuilt tpyescript components i want to convert to blazor cms blocks (like 30 of them) - I will add those in later


