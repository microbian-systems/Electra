using Aero.MerakiUI.Tabs;
using Bunit;
using Aero.MerakiUI.Tabs;

namespace Aero.MerakiUI.Tests.Tabs;

public class TabTests : BunitContext
{
    [Fact]
    public void SimpleTabs_ShouldRenderCorrectStructure()
    {
        var cut = Render<SimpleTabs>(parameters => parameters
            .Add(p => p.Tabs, new[] { "Account", "Company", "Team", "Billing" })
            .AddChildContent("<div x-show='activeTab === 0'>Account Content</div><div x-show='activeTab === 1'>Company Content</div>")
        );

        // Verify Alpine data initialization (activeTab state)
        var container = cut.Find("div[x-data]");
        
        // Verify tab buttons are rendered
        var buttons = cut.FindAll("button");
        Assert.Equal(4, buttons.Count);
        Assert.Contains("Account", buttons[0].TextContent);
        
        // Verify content container
        Assert.Contains("Account Content", cut.Markup);
    }

    [Fact]
    public void TabWithIcons_ShouldRenderCorrectStructure()
    {
        var items = new List<Aero.MerakiUI.Tabs.TabWithIcons.TabItem>
        {
            new() { Title = "Profile", Icon = "<svg>...</svg>" },
            new() { Title = "Dashboard", Icon = "<svg>...</svg>" }
        };

        var cut = Render<TabWithIcons>(parameters => parameters
            .Add(p => p.Items, items)
            .AddChildContent("<div>Content</div>")
        );

        // Verify Alpine data initialization
        var container = cut.Find("div[x-data]");
        
        // Verify tab buttons
        var buttons = cut.FindAll("button");
        Assert.Equal(2, buttons.Count);
        Assert.Contains("Profile", buttons[0].TextContent);
        Assert.Contains("<svg>...</svg>", buttons[0].InnerHtml);
    }
}
