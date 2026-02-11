using Bunit;
using Electra.MerakiUI.Modals;

namespace Electra.MerakiUI.Tests.Modals;

public class ModalTests : BunitContext
{
    [Fact]
    public void SimpleModal_ShouldRenderCorrectStructure()
    {
        var cut = Render<SimpleModal>(parameters => parameters
            .Add(p => p.TriggerText, "Open Modal")
            .AddChildContent("<p>Modal Content</p>")
        );

        // Verify Alpine data initialization
        var container = cut.Find("div[x-data]");
        
        // Verify trigger button
        var button = cut.Find("button");
        Assert.Contains("Open Modal", button.TextContent);
        
        // Verify modal overlay/container exists (hidden by default via Alpine)
        var modal = cut.Find("div[x-show]");
        Assert.Contains("Modal Content", modal.InnerHtml);
    }

    [Fact]
    public void ModalWithAction_ShouldRenderCorrectStructure()
    {
        var cut = Render<ModalWithAction>(parameters => parameters
            .Add(p => p.TriggerText, "Delete")
            .AddChildContent("<p>Are you sure?</p>")
        );

        // Verify Alpine data initialization
        var container = cut.Find("div[x-data]");
        
        // Verify trigger button
        var button = cut.Find("button");
        Assert.Contains("Delete", button.TextContent);
        
        // Verify modal overlay/container exists (hidden by default via Alpine)
        var modal = cut.Find("div[x-show]");
        Assert.Contains("Are you sure?", modal.InnerHtml);
    }
}
