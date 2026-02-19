using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Aero.CMS.Tests.Unit.Content;

public class RichTextEditorTests
{
    [Fact]
    public void NullRichTextEditor_EditorAlias_Is_Null()
    {
        var editor = new NullRichTextEditor();
        Assert.Equal("null", editor.EditorAlias);
    }

    [Fact]
    public void NullRichTextEditor_Render_Returns_NonNull_Fragment()
    {
        var editor = new NullRichTextEditor();
        var settings = new RichTextEditorSettings();
        
        var fragment = editor.Render("test", true, default, settings);
        
        Assert.NotNull(fragment);
    }

    [Fact]
    public void RichTextEditorSettings_Has_Defaults()
    {
        var settings = new RichTextEditorSettings();
        
        Assert.Equal(300, settings.MinHeight);
        Assert.True(settings.EnableMedia);
        Assert.True(settings.EnableTables);
        Assert.True(settings.EnableCodeBlocks);
        Assert.NotEmpty(settings.ToolbarItems);
    }
}
