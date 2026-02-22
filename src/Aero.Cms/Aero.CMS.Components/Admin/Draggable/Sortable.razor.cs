using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aero.CMS.Components.Admin.Draggable;

public partial class Sortable<TItem> : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the parent SortableWrapper, if this Sortable is nested.
    /// </summary>
    [CascadingParameter]
    public SortableWrapper? ParentSortable { get; set; }
    
    /// <summary>
    /// Gets or sets the list of items managed by this sortable instance.
    /// </summary>
    [Parameter]
    public List<TItem> Items { get; set; } = new List<TItem>();
    
    private List<KeyedItem<TItem>> _keyedItems = new();
    
    [Parameter]
    public RenderFragment<TItem> Template { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public Func<Task>? OnDataChanged { get; set; }

    /// <summary>
    /// Callback fired when an item is dropped into this sortable (either from another list or reordered within)
    /// </summary>
    [Parameter]
    public Func<SortableDroppedEventArgs<TItem>, Task>? OnItemDropped { get; set; }

    /// <summary>
    /// Optional identifier for this sortable, useful for identifying which list received a drop
    /// </summary>
    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Parameter]
    public string Class { get; set; } = string.Empty;
    
    [Parameter]
    public object? Options { get; set; } 

    internal ElementReference _dropZoneContainer;
    private DotNetObjectReference<Sortable<TItem>>? _objRef;
    private bool _hasInitializedJs = false;

    /// <summary>
    /// Handles updates to parameters and rebuilds keyed items if needed.
    /// </summary>
    protected override void OnParametersSet()
    {
        // Rebuild keyed items if needed, preserving existing keys where possible to prevent DOM churn
        var newKeyedItems = new List<KeyedItem<TItem>>();
        foreach (var item in Items)
        {
            var existing = _keyedItems.FirstOrDefault(k => EqualityComparer<TItem>.Default.Equals(k.Item, item));
            if (existing != null)
            {
                newKeyedItems.Add(existing);
            }
            else
            {
                newKeyedItems.Add(new KeyedItem<TItem> { Key = Guid.NewGuid().ToString(), Item = item });
            }
        }
        _keyedItems = newKeyedItems;
    }

    /// <summary>
    /// Initializes or syncs the sortable JS instance after rendering.
    /// </summary>
    /// <param name="firstRender">Indicates whether this is the first render.</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_hasInitializedJs)
        {
            _hasInitializedJs = true;
            _objRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("initializeSortable", _dropZoneContainer, _objRef, Options, Id);
        }
        else if (_hasInitializedJs)
        {
            await JsRuntime.InvokeVoidAsync("updateSortableItems", _dropZoneContainer);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (ParentSortable != null && !string.IsNullOrEmpty(Id))
        {
            ParentSortable.RegisterSortable(Id, this);
        }
    }

    /// <summary>
    /// Disposes the JS instance when the component is removed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (ParentSortable != null && !string.IsNullOrEmpty(Id))
        {
            ParentSortable.UnregisterSortable(Id);
        }

        if (_hasInitializedJs)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("destroySortable", _dropZoneContainer);
            }
            catch { }
            
            _objRef?.Dispose();
            _objRef = null;
        }
    }

    /// <summary>
    /// Updates the order of an item within the list. Invoked by JS.
    /// </summary>
    /// <param name="oldIndex">The original index of the item.</param>
    /// <param name="newIndex">The new index of the item.</param>
    [JSInvokable]
    public async Task UpdateItemOrder(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Items.Count) return;
        var item = Items[oldIndex];
        Items.RemoveAt(oldIndex);
        Items.Insert(newIndex, item);

        if (OnItemDropped is not null)
        {
            var args = new SortableDroppedEventArgs<TItem>
            {
                Item = item,
                TargetSortableId = Id,
                SourceSortableId = Id,
                NewIndex = newIndex,
                OldIndex = oldIndex,
                ItemBefore = newIndex > 0 ? Items[newIndex - 1] : default,
                ItemAfter = newIndex < Items.Count - 1 ? Items[newIndex + 1] : default
            };
            await OnItemDropped.Invoke(args);
            return;
        }

        await NotifyChangedAsync();
    }

    /// <summary>
    /// Moves an item from another sortable list to this one. Invoked by JS.
    /// </summary>
    /// <param name="sourceId">The source sortable identifier.</param>
    /// <param name="oldIndex">The index in the source.</param>
    /// <param name="newIndex">The index in the destination.</param>
    [JSInvokable]
    public async Task MoveItemFromList(string sourceId, int oldIndex, int newIndex)
    {
        if (ParentSortable == null) return;
        
        var sourceSortable = ParentSortable.GetSortable(sourceId) as Sortable<TItem>;
        if (sourceSortable == null) return;

        if (oldIndex < 0 || oldIndex >= sourceSortable.Items.Count) return;
        
        var item = sourceSortable.Items[oldIndex];
        sourceSortable.Items.RemoveAt(oldIndex);

        if (newIndex < 0) newIndex = 0;
        if (newIndex > Items.Count) newIndex = Items.Count;
        Items.Insert(newIndex, item);

        if (OnItemDropped is not null)
        {
            var args = new SortableDroppedEventArgs<TItem>
            {
                Item = item,
                TargetSortableId = Id,
                SourceSortableId = sourceId,
                NewIndex = newIndex,
                OldIndex = oldIndex,
                ItemBefore = newIndex > 0 ? Items[newIndex - 1] : default,
                ItemAfter = newIndex < Items.Count - 1 ? Items[newIndex + 1] : default
            };
            await OnItemDropped.Invoke(args);
            return;
        }

        await NotifyChangedAsync();
    }

    private async Task NotifyChangedAsync()
    {
        if (ParentSortable != null)
        {
            if (ParentSortable.OnDataChanged.HasDelegate)
                await ParentSortable.OnDataChanged.InvokeAsync();
            await ParentSortable.RefreshAsync();
        }
        else if (OnDataChanged is not null)
        {
            await OnDataChanged.Invoke();
        }
        
        StateHasChanged();
    }

    /// <summary>
    /// Moves an item within the list or inserts it if it's new.
    /// </summary>
    /// <param name="item">The item to move or insert.</param>
    /// <param name="newIndex">The index location.</param>
    /// <returns>True if the item was already in the list.</returns>
    public bool MoveOrInsertItem(TItem item, int newIndex)
    {
        var existingIndex = Items.IndexOf(item);
        if (existingIndex >= 0)
        {
            if (existingIndex == newIndex) return true;
            Items.RemoveAt(existingIndex);
            if (newIndex > existingIndex) newIndex--;
        }

        if (newIndex < 0) newIndex = 0;
        if (newIndex > Items.Count) newIndex = Items.Count;

        Items.Insert(newIndex, item);
        return existingIndex >= 0;
    }

    /// <summary>
    /// Removes the specified item if it exists in the list.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was found and removed.</returns>
    public bool RemoveItemIfExists(TItem item)
    {
        return Items.Remove(item);
    }

    /// <summary>
    /// Synchronizes the current items with a provided ordered list.
    /// </summary>
    /// <param name="orderedItems">The canonical order of items.</param>
    public void SyncItems(List<TItem> orderedItems)
    {
        Items.Clear();
        Items.AddRange(orderedItems);
    }
}