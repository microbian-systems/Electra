using Bunit;
using Electra.MerakiUI.Dropdowns;
using Xunit;

namespace Electra.MerakiUI.Tests.Dropdowns;

public class DropdownTests : BunitContext
{
    [Fact]
    public void SimpleDropdown_ShouldRenderCorrectStructure()
    {
        var cut = Render<SimpleDropdown>(parameters => parameters
            .Add(p => p.TriggerText, "Options")
            .AddChildContent("<a href='#'>Item 1</a>")
        );

        // Should have the main container with Alpine data attribute
        var container = cut.Find("div[x-data]");
        
        // Should have the trigger button
        var button = cut.Find("button");
        Assert.Contains("Options", button.TextContent);
        
        // Should have the dropdown menu container
        var menu = cut.Find("div[x-show]");
        Assert.Contains("Item 1", menu.InnerHtml);
    }

    [Fact]
    public void DropdownWithIcons_ShouldRenderCorrectStructure()
    {
        var cut = Render<DropdownWithIcons>(parameters => parameters
            .Add(p => p.TriggerText, "Menu")
            .AddChildContent("<a href='#'>Settings</a>")
        );

        // Should have the main container with Alpine data attribute
        var container = cut.Find("div[x-data]");
        
        // Should have the trigger button
        var button = cut.Find("button");
        Assert.Contains("Menu", button.TextContent);
        
        // Should have the dropdown menu container
        var menu = cut.Find("div[x-show]");
        Assert.Contains("Settings", menu.InnerHtml);
    }
}
