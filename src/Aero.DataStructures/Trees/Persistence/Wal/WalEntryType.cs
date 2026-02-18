namespace Aero.DataStructures.Trees.Persistence.Wal;

public enum WalEntryType : byte
{
    Begin = 0x01,
    Write = 0x02,
    Allocate = 0x03,
    Free = 0x04,
    Commit = 0x05,
    Abort = 0x06,
    Checkpoint = 0x07,
    Clr = 0x08,
}
