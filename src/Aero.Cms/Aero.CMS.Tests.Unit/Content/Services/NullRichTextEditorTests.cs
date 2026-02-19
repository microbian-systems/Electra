using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Content.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content.Services;

public class NullRichTextEditorTests
{
    [Fact]
    public void EditorAlias_Is_null()
    {
        var editor = new NullRichTextEditor();
        editor.EditorAlias.ShouldBe("null");
    }

    [Fact]
    public void Render_WithIsEditingTrue_ReturnsTextarea()
    {
        var editor = new NullRichTextEditor();
        var settings = new RichTextEditorSettings { MinHeight = 200 };
        var renderFragment = editor.Render("test value", isEditing: true, EventCallback<string>.Empty, settings);
        
        renderFragment.ShouldNotBeNull();
        
        // Execute the render fragment with a dummy RenderTreeBuilder to ensure no exception
        var builder = new RenderTreeBuilder();
        renderFragment(builder);
    }

    [Fact]
    public void Render_WithIsEditingFalse_ReturnsDiv()
    {
        var editor = new NullRichTextEditor();
        var settings = new RichTextEditorSettings { MinHeight = 200 };
        var renderFragment = editor.Render("test value", isEditing: false, EventCallback<string>.Empty, settings);
        
        renderFragment.ShouldNotBeNull();
        
        var builder = new RenderTreeBuilder();
        renderFragment(builder);
    }
}