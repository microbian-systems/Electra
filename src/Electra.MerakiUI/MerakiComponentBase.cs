using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI;

public abstract class MerakiComponentBase : ComponentBase
{
    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }
}
