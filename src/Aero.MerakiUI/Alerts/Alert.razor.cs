using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Alerts;

public partial class Alert : MerakiComponentBase
{
    [Parameter]
    public AlertType Type { get; set; } = AlertType.Info;

    [Parameter]
    public string? Title { get; set; }

    protected string TypeColorClass => Type switch
    {
        AlertType.Success => "bg-emerald-500",
        AlertType.Info => "bg-blue-500",
        AlertType.Warning => "bg-yellow-500",
        AlertType.Error => "bg-red-500",
        _ => "bg-blue-500"
    };

    protected string TitleColorClass => Type switch
    {
        AlertType.Success => "text-emerald-500 dark:text-emerald-400",
        AlertType.Info => "text-blue-500 dark:text-blue-400",
        AlertType.Warning => "text-yellow-500 dark:text-yellow-400",
        AlertType.Error => "text-red-500 dark:text-red-400",
        _ => "text-blue-500 dark:text-blue-400"
    };
}
