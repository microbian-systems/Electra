using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Wal;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WalEntryHeader
{
    public uint Crc32;
    public int TotalLength;
    public Lsn Lsn;
    public long TransactionId;
    public WalEntryType Type;
    public long PageId;
    public int PageOffset;
    public int ImageLength;
    public Lsn ReferenceLsn;
}
