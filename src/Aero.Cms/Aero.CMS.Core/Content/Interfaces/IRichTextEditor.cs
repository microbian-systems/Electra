using Microsoft.AspNetCore.Components;
using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Interfaces;

public interface IRichTextEditor
{
    string EditorAlias { get; }
    RenderFragment Render(string value, bool isEditing, EventCallback<string> onChanged, RichTextEditorSettings settings);
}
