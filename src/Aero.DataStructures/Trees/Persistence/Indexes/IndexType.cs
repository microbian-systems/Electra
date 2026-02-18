namespace Aero.DataStructures.Trees.Persistence.Indexes;

public enum IndexType : byte
{
    Primary = 0x01,
    Secondary = 0x02,
    Unique = 0x03,
    Composite = 0x04,
}
