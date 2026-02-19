using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Services;

public class NullRichTextEditor : IRichTextEditor
{
    public string EditorAlias => "null";

    public RenderFragment Render(string value, bool isEditing, EventCallback<string> onChanged, RichTextEditorSettings settings)
    {
        return builder =>
        {
            if (isEditing)
            {
                builder.OpenElement(0, "textarea");
                builder.AddAttribute(1, "style", $"min-height: {settings.MinHeight}px; width: 100%;");
                builder.AddAttribute(2, "value", value);
                builder.AddAttribute(3, "onchange", onChanged);
                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, (MarkupString)value);
                builder.CloseElement();
            }
        };
    }
}
