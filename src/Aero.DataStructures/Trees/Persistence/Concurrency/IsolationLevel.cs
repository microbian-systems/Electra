namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public enum IsolationLevel : byte
{
    ReadCommitted = 0x01,
    SnapshotMVCC = 0x02,
    OptimisticOCC = 0x03,
    Serializable = 0x04,
}
