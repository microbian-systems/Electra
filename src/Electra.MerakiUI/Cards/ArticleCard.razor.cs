using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Cards;

public partial class ArticleCard : MerakiComponentBase
{
    [Parameter]
    public string Category { get; set; } = "Category";

    [Parameter]
    public string Title { get; set; } = "Title";

    [Parameter]
    public string Description { get; set; } = "";

    [Parameter]
    public string Date { get; set; } = "";

    [Parameter]
    public string Href { get; set; } = "#";
}
