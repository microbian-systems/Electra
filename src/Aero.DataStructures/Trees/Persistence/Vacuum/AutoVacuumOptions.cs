using System;

namespace Aero.DataStructures.Trees.Persistence.Vacuum;

public sealed class AutoVacuumOptions
{
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    public double FragmentationThreshold { get; set; } = 0.5;
}
