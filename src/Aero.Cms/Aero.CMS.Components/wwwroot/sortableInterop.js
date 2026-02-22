window.initializeSortable = (container, blazorComponent, options, sortableId) => {
    // Ensure cleanup of previous instance if it exists to prevent duplicate event listeners
    if (container.__sortable_instance) {
        container.__sortable_instance.destroy();
    }

    blazorComponent.sortableId = sortableId || '';

    const common = {
        onUpdate: function (evt) {
            // Restore DOM state to what Blazor expects
            evt.item.parentNode.removeChild(evt.item);
            const referenceNode = evt.from.childNodes[evt.oldIndex] || null;
            evt.from.insertBefore(evt.item, referenceNode);

            blazorComponent.invokeMethodAsync('UpdateItemOrder', evt.oldIndex, evt.newIndex);
        },
        onRemove: function (evt) {
            // Restore the item to the original list so Blazor DOM diffing doesn't crash
            if (evt.item.parentNode) {
                evt.item.parentNode.removeChild(evt.item);
            }
            const referenceNode = evt.from.childNodes[evt.oldIndex] || null;
            evt.from.insertBefore(evt.item, referenceNode);
        },
        onAdd: function (evt) {
            const oldIndex = evt.oldIndex;
            const newIndex = evt.newIndex;
            const fromComponent = evt.from.__blazor_component;
            const toComponent = evt.to.__blazor_component;
            const sourceSortableId = fromComponent.sortableId || null;

            // Remove the dropped element from the target DOM, letting Blazor re-render it naturally
            if (evt.item.parentNode) {
                evt.item.parentNode.removeChild(evt.item);
            }

            // Call Blazor to move the item over using the parent sortable wrapper context
            toComponent.invokeMethodAsync('MoveItemFromList', sourceSortableId, oldIndex, newIndex);
        }
    };

    const init = Object.assign({}, options, common);
    const instance = Sortable.create(container, init);
    container.__sortable_instance = instance;
    container.__blazor_component = blazorComponent;
};

window.destroySortable = (element) => {
    try {
        if (element && element.__sortable_instance) {
            element.__sortable_instance.destroy();
            element.__sortable_instance = null;
        }
    } catch (e) {
        console.error("Error destroying Sortable:", e);
    }
};

window.updateSortableItems = (container) => {
    // No-op, we no longer need to track the actual items in JS
};