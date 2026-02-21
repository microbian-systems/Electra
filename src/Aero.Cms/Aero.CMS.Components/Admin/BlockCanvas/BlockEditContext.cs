namespace Aero.CMS.Components.Admin.BlockCanvas;

public class BlockEditContext
{
    public Guid? ActiveBlockId { get; private set; }
    public Guid? ActiveSectionId { get; private set; }
    public event Action? OnChanged;

    public void SetActiveBlock(Guid blockId, Guid sectionId)
    {
        ActiveBlockId = blockId;
        ActiveSectionId = sectionId;
        OnChanged?.Invoke();
    }

    public void ClearBlock()
    {
        ActiveBlockId = null;
        OnChanged?.Invoke();
    }

    public void ClearAll()
    {
        ActiveBlockId = null;
        ActiveSectionId = null;
        OnChanged?.Invoke();
    }

    public bool IsBlockActive(Guid blockId) => ActiveBlockId == blockId;
    public bool IsSectionActive(Guid sectionId) => ActiveSectionId == sectionId;
}
