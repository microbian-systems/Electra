namespace Aero.CMS.Web.Draggable;

public class NestedModel
{
    public string Data { get; set; } = string.Empty;
    public List<NestedModel> Children { get; set; } = new List<NestedModel>();
}