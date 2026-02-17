using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Teams;

public partial class TeamCard : MerakiComponentBase
{
    [Parameter]
    public string Name { get; set; } = "Member Name";

    [Parameter]
    public string Role { get; set; } = "Job Role";

    [Parameter]
    public string ImageUrl { get; set; } = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?ixlib=rb-1.2.1&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=880&q=80";
}
