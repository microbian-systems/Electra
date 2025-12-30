using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Pagination;

public partial class PaginationControl : MerakiComponentBase
{
    [Parameter]
    public int CurrentPage { get; set; } = 1;

    [Parameter]
    public int TotalPages { get; set; } = 5;

    [Parameter]
    public string PreviousButtonText { get; set; } = "previous";

    [Parameter]
    public string NextButtonText { get; set; } = "Next";

    [Parameter]
    public EventCallback<int> OnPageChange { get; set; }
}
