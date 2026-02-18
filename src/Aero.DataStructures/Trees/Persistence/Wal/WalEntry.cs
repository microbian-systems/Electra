namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed class WalEntry
{
    public WalEntryHeader Header;

    public ReadOnlyMemory<byte> BeforeImage;

    public ReadOnlyMemory<byte> AfterImage;

    public bool IsCommitted => Header.Type == WalEntryType.Commit;
    public bool IsAborted => Header.Type == WalEntryType.Abort;
    public bool IsWrite => Header.Type == WalEntryType.Write;
    public bool IsCheckpoint => Header.Type == WalEntryType.Checkpoint;
}
