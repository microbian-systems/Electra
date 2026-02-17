using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Testimonials;

public partial class TestimonialCard : MerakiComponentBase
{
    [Parameter]
    public string Content { get; set; } = "Lorem ipsum dolor sit amet...";

    [Parameter]
    public string AuthorName { get; set; } = "Author Name";

    [Parameter]
    public string AuthorRole { get; set; } = "Job Role";

    [Parameter]
    public string AuthorImageUrl { get; set; } = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=880&q=80";

    [Parameter]
    public bool IsPrimary { get; set; } = false;

    private string GetBackgroundClass() => IsPrimary ? "bg-blue-500 dark:bg-blue-600" : "";

    private string GetBorderClass() => IsPrimary ? "border-transparent" : "dark:border-gray-700";

    private string GetTextClass() => IsPrimary ? "text-white" : "text-gray-500 dark:text-gray-400";

    private string GetImageRingClass() => IsPrimary ? "ring-blue-200" : "ring-gray-300 dark:ring-gray-700";

    private string GetAuthorNameClass() => IsPrimary ? "text-white" : "text-gray-800 dark:text-white";

    private string GetAuthorRoleClass() => IsPrimary ? "text-blue-200" : "text-gray-500 dark:text-gray-400";
}
