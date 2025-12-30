# Electra.MerakiUI

This project contains Blazor Razor components converted from [MerakiUI](https://merakiui.com/).

## Component Pattern

All components follow a strict 4-file structure:

1.  `Category/ComponentName.razor` - HTML markup (stripped of `<html>`, `<head>`, `<body>`).
2.  `Category/ComponentName.razor.cs` - C# code-behind logic.
3.  `Category/ComponentName.razor.css` - Component-specific scoped CSS.
4.  `Category/ComponentName.razor.ts` - Component-specific scoped TypeScript (auto-compiles to `.js`).

### Base Class

Most components inherit from `MerakiComponentBase` to provide standard parameters:

- `Class`: For appending custom CSS classes.
- `Id`: For setting the HTML ID.
- `ChildContent`: For nested content.
- `AdditionalAttributes`: To capture unmatched attributes (Splats).

## Data Binding

Input components support Blazor data binding using the `@bind-Value` syntax.

```razor
<TextInput @bind-Value="myName" Label="Name" />
```

## TypeScript Compilation

TypeScript files (`.ts`) are automatically compiled to JavaScript (`.js`) on build using the `Microsoft.TypeScript.MSBuild` package. Scoped JS files are handled by Blazor as static assets associated with the component.
