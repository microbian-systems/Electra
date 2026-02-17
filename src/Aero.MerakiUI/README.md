# Aero.MerakiUI

This project contains Blazor Razor components converted from [MerakiUI](https://merakiui.com/).

## Prerequisites

The host application must include the following client-side libraries:

- **Tailwind CSS**: For styling.
- **Alpine.js**: For interactive component state (Dropdowns, Modals, Tabs).

## Component Pattern

All components follow a strict 4-file structure:

1.  `Category/ComponentName.razor` - HTML markup.
2.  `Category/ComponentName.razor.cs` - C# code-behind logic.
3.  `Category/ComponentName.razor.css` - Component-specific scoped CSS.
4.  `Category/ComponentName.razor.ts` - Component-specific scoped TypeScript (auto-compiles to `.js`).

### Base Class

Most components inherit from `MerakiComponentBase` to provide standard parameters:

- `Class`: For appending custom CSS classes.
- `Id`: For setting the HTML ID.
- `ChildContent`: For nested content.
- `AdditionalAttributes`: To capture unmatched attributes (Splats).

## Examples

### Basic Component (Card)
```razor
<ProductCard 
    Title="Classic Watch" 
    Price="$299.00" 
    ImageUrl="/images/watch.png">
    <button class="btn-primary">Add to Cart</button>
</ProductCard>
```

### Interactive Component (Dropdown)
```razor
<SimpleDropdown TriggerText="Settings">
    <a href="#" class="block px-4 py-2 text-sm text-gray-700">Account</a>
    <a href="#" class="block px-4 py-2 text-sm text-gray-700">Privacy</a>
    <hr class="border-gray-200 dark:border-gray-700">
    <a href="#" class="block px-4 py-2 text-sm text-red-600">Logout</a>
</SimpleDropdown>
```

### Data Binding (Input)
Input components support Blazor data binding using the `@bind-Value` syntax.

```razor
<TextInput @bind-Value="myName" Label="Name" />
```

## Interactive Components & JS Interop

While we leverage **Alpine.js** for lightweight client-side state (like toggling visibility), the project maintains a `.razor.ts` file for every component to allow for more complex JS interop if required.

TypeScript files are automatically compiled to JavaScript on build. Blazor automatically handles these scoped assets.