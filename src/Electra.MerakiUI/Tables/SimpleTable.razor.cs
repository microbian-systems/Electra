using System;
using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Tables;

public partial class SimpleTable : MerakiComponentBase
{
    [Parameter]
    public string[] Headers { get; set; } = Array.Empty<string>();
}
