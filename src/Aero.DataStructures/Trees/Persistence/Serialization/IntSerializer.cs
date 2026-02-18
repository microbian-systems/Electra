using System;
using System.Buffers.Binary;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

/// <summary>
/// Serializes and deserializes 32-bit integers using little-endian encoding.
/// </summary>
public sealed class IntSerializer : INodeSerializer<int>
{
    /// <inheritdoc />
    public int SerializedSize => sizeof(int);

    /// <inheritdoc />
    public int Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < SerializedSize)
            throw new ArgumentException($"Data span too small. Expected {SerializedSize} bytes.", nameof(data));
        
        return BinaryPrimitives.ReadInt32LittleEndian(data);
    }

    /// <inheritdoc />
    public void Serialize(int node, Span<byte> destination)
    {
        if (destination.Length < SerializedSize)
            throw new ArgumentException($"Destination span too small. Expected {SerializedSize} bytes.", nameof(destination));
        
        BinaryPrimitives.WriteInt32LittleEndian(destination, node);
    }
}
