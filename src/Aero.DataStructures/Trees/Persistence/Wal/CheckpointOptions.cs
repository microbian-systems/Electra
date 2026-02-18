using System;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed class CheckpointOptions
{
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public long WalSizeThresholdBytes { get; set; } = 64 * 1024 * 1024;
    public int WalEntryCountThreshold { get; set; } = 10_000;
    public bool CheckpointOnShutdown { get; set; } = true;
}
