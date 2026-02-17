using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Breadcrumbs;

public partial class BreadcrumbItem : MerakiComponentBase
{
    [Parameter]
    public string Text { get; set; } = "Home";

    [Parameter]
    public string Url { get; set; } = "#";

    [Parameter]
    public bool IsActive { get; set; } = false;

    [Parameter]
    public bool ShowSeparator { get; set; } = true;

    [Parameter]
    public RenderFragment? IconContent { get; set; }

    [Parameter]
    public RenderFragment? SeparatorContent { get; set; }
}
