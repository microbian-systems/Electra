using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Inputs;

public partial class EmailInput : MerakiComponentBase
{
    [Parameter]
    public string? Placeholder { get; set; } = "Email Address";

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
