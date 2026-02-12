using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Inputs;

public partial class Textarea : MerakiComponentBase
{
    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    protected async Task OnInput(ChangeEventArgs e)
    {
        Value = e.Value?.ToString();
        await ValueChanged.InvokeAsync(Value);
    }
}
