using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Blog;

public partial class BlogSection : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "From the blog";

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public bool IsCentered { get; set; } = false;

    [Parameter]
    public int Columns { get; set; } = 2;

    private string GetHeaderAlignmentClass() => IsCentered ? "text-center" : "";

    private string GetGridColsClass() => Columns switch
    {
        1 => "grid-cols-1",
        2 => "md:grid-cols-2",
        3 => "md:grid-cols-3",
        _ => "md:grid-cols-2"
    };
}
