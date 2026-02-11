using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Cards;

public partial class ProductCard : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Product Name";

    [Parameter]
    public string Price { get; set; } = "$0";

    [Parameter]
    public string ImageUrl { get; set; } = "";
}
