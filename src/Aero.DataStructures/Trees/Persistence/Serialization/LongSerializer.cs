using System;
using System.Buffers.Binary;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

/// <summary>
/// Serializes and deserializes 64-bit integers using little-endian encoding.
/// </summary>
public sealed class LongSerializer : INodeSerializer<long>
{
    /// <inheritdoc />
    public int SerializedSize => sizeof(long);

    /// <inheritdoc />
    public long Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < SerializedSize)
            throw new ArgumentException($"Data span too small. Expected {SerializedSize} bytes.", nameof(data));
        
        return BinaryPrimitives.ReadInt64LittleEndian(data);
    }

    /// <inheritdoc />
    public void Serialize(long node, Span<byte> destination)
    {
        if (destination.Length < SerializedSize)
            throw new ArgumentException($"Destination span too small. Expected {SerializedSize} bytes.", nameof(destination));
        
        BinaryPrimitives.WriteInt64LittleEndian(destination, node);
    }
}
