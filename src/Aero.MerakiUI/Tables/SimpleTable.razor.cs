using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Tables;

public partial class SimpleTable : MerakiComponentBase
{
    [Parameter]
    public string[] Headers { get; set; } = Array.Empty<string>();
}
