using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Blog;

public partial class HorizontalBlogCard : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Blog Post Title";

    [Parameter]
    public string Date { get; set; } = "20 October 2019";

    [Parameter]
    public string Url { get; set; } = "#";

    [Parameter]
    public string ImageUrl { get; set; } = "https://images.unsplash.com/photo-1515378960530-7c0da6231fb1?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1470&q=80";

    [Parameter]
    public string ImageAlt { get; set; } = "Blog Post Image";
}
