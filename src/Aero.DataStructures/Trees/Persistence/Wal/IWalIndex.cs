namespace Aero.DataStructures.Trees.Persistence.Wal;

public interface IWalIndex
{
    void Record(Lsn lsn, long fileOffset);
    bool TryGetOffset(Lsn lsn, out long fileOffset);
    Lsn MinLsn { get; }
    Lsn MaxLsn { get; }
    void TruncateBefore(Lsn lsn);
}
