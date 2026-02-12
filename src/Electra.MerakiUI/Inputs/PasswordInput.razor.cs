using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Inputs;

public partial class PasswordInput : MerakiComponentBase
{
    [Parameter]
    public string? Placeholder { get; set; } = "Password";

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    protected bool ShowPassword { get; set; }

    protected string InputType => ShowPassword ? "text" : "password";

    protected async Task OnInput(ChangeEventArgs e)
    {
        Value = e.Value?.ToString();
        await ValueChanged.InvokeAsync(Value);
    }

    protected void ToggleShowPassword()
    {
        ShowPassword = !ShowPassword;
    }
}
