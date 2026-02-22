namespace Aero.CMS.Components;

internal sealed class KeyedItem<T>
{
    public string Key { get; set; } = Guid.NewGuid().ToString();
    public T Item { get; set; }
}