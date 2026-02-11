using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Blog;

public partial class BlogCard : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Blog Post Title";

    [Parameter]
    public string Excerpt { get; set; } = "Lorem ipsum dolor sit amet consectetur adipisicing elit. Iure veritatis sint autem nesciunt...";

    [Parameter]
    public string Date { get; set; } = "21 October 2019";

    [Parameter]
    public string Url { get; set; } = "#";

    [Parameter]
    public string ImageUrl { get; set; } = "https://images.unsplash.com/photo-1644018335954-ab54c83e007f?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=1470&q=80";

    [Parameter]
    public string ImageAlt { get; set; } = "Blog Post Image";
}
