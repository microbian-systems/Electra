using System;

namespace Aero.DataStructures.Trees.Persistence.Format;

public sealed class UnsupportedFormatVersionException : Exception
{
    public ushort FoundVersion { get; }
    public ushort SupportedVersion { get; }

    public UnsupportedFormatVersionException(ushort found, ushort supported)
        : base($"File format version {found} is not supported. Maximum supported: {supported}.")
    {
        FoundVersion = found;
        SupportedVersion = supported;
    }
}
