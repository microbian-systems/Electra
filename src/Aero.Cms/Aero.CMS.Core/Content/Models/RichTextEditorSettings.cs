using System.Collections.Generic;

namespace Aero.CMS.Core.Content.Models;

public class RichTextEditorSettings
{
    public int MinHeight { get; set; } = 300;
    public bool EnableMedia { get; set; } = true;
    public bool EnableTables { get; set; } = true;
    public bool EnableCodeBlocks { get; set; } = true;
    public List<string> ToolbarItems { get; set; } = new()
    {
        "bold", "italic", "underline", "strikethrough",
        "alignleft", "aligncenter", "alignright", "alignjustify",
        "bullist", "numlist", "link", "image", "code"
    };
}
