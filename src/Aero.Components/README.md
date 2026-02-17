# Aero.Components

Blazor components for the Aero framework.

## Overview

`Aero.Components` provides reusable Blazor components for building interactive web UIs with the Aero framework.

## Key Components

### Data Components

```csharp
@typeparam TItem

@if (Items == null)
{
    <div class="loading">Loading...</div>
}
else if (!Items.Any())
{
    <div class="empty">@EmptyMessage</div>
}
else
{
    <table class="aero-table">
        <thead>
            <tr>
                @HeaderTemplate
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Items)
            {
                <tr @onclick="() => OnRowClick.InvokeAsync(item)">
                    @RowTemplate(item)
                </tr>
            }
        </tbody>
    </table>

    @if (ShowPagination)
    {
        <AeroPagination 
            Page="Page"
            PageSize="PageSize"
            TotalCount="TotalCount"
            OnPageChange="OnPageChange" />
    }
}

@code {
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public RenderFragment HeaderTemplate { get; set; } = null!;
    [Parameter] public RenderFragment<TItem> RowTemplate { get; set; } = null!;
    [Parameter] public string EmptyMessage { get; set; } = "No items found";
    [Parameter] public bool ShowPagination { get; set; } = true;
    [Parameter] public int Page { get; set; } = 1;
    [Parameter] public int PageSize { get; set; } = 20;
    [Parameter] public int TotalCount { get; set; }
    [Parameter] public EventCallback<int> OnPageChange { get; set; }
    [Parameter] public EventCallback<TItem> OnRowClick { get; set; }
}
```

### Form Components

```csharp
@inherits InputBase<TValue>
@typeparam TValue

<div class="aero-input @CssClass">
    @if (!string.IsNullOrEmpty(Label))
    {
        <label for="@Id">@Label</label>
    }
    
    <input 
        id="@Id"
        type="@InputType"
        class="form-control @((ValidationContext.GetValidationMessages().Any() ? "is-invalid" : ""))"
        @bind="CurrentValue"
        @bind:event="oninput"
        placeholder="@Placeholder"
        disabled="@Disabled" />
    
    @if (ValidationContext.GetValidationMessages().Any())
    {
        <div class="invalid-feedback">
            @foreach (var message in ValidationContext.GetValidationMessages())
            {
                <span>@message</span>
            }
        </div>
    }
</div>

@code {
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string InputType { get; set; } = "text";
    [Parameter] public bool Disabled { get; set; }
    
    private string Id { get; set; } = Guid.NewGuid().ToString();

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        // Implementation
    }
}
```

### Layout Components

```csharp
<div class="aero-layout">
    <AeroSidebar 
        Title="@SidebarTitle"
        Items="MenuItems"
        IsCollapsed="sidebarCollapsed"
        OnToggle="ToggleSidebar" />
    
    <div class="main-content">
        <AeroTopBar 
            User="CurrentUser"
            Notifications="Notifications"
            OnLogout="HandleLogout" />
        
        <main class="content">
            @ChildContent
        </main>
    </div>
</div>

@code {
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;
    [Parameter] public string SidebarTitle { get; set; } = "Aero";
    [Parameter] public List<MenuItem> MenuItems { get; set; } = new();
    [Parameter] public UserDto? CurrentUser { get; set; }
    [Parameter] public List<Notification> Notifications { get; set; } = new();
    [Parameter] public EventCallback OnLogout { get; set; }

    private bool sidebarCollapsed = false;

    private void ToggleSidebar() => sidebarCollapsed = !sidebarCollapsed;
}
```

## Usage

```razor
@page "/products"

<AeroLayout MenuItems="menuItems" CurrentUser="currentUser">
    <h1>Products</h1>
    
    <AeroDataGrid 
        Items="products"
        Page="page"
        PageSize="pageSize"
        TotalCount="totalCount"
        OnPageChange="LoadProducts">
        <HeaderTemplate>
            <th>Name</th>
            <th>Price</th>
            <th>Actions</th>
        </HeaderTemplate>
        <RowTemplate>
            <td>@context.Name</td>
            <td>@context.Price.ToString("C")</td>
            <td>
                <AeroButton OnClick="() => EditProduct(context.Id)">Edit</AeroButton>
                <AeroButton Variant="Danger" OnClick="() => DeleteProduct(context.Id)">Delete</AeroButton>
            </td>
        </RowTemplate>
    </AeroDataGrid>
</AeroLayout>

@code {
    private List<ProductDto> products = new();
    private int page = 1;
    private int pageSize = 20;
    private int totalCount;

    protected override async Task OnInitializedAsync()
    {
        await LoadProducts(1);
    }

    private async Task LoadProducts(int newPage)
    {
        page = newPage;
        var result = await ProductService.GetProductsAsync(page, pageSize);
        products = result.Items;
        totalCount = result.TotalCount;
    }
}
```

## Configuration

```csharp
builder.Services.AddAeroComponents();

public static class ComponentExtensions
{
    public static IServiceCollection AddAeroComponents(this IServiceCollection services)
    {
        services.AddScoped<ToastService>();
        services.AddScoped<ModalService>();
        services.AddScoped<DialogService>();

        return services;
    }
}
```

## Related Packages

- `Aero.Web` - Server-side Blazor integration
- `Aero.Models` - DTOs used in components
