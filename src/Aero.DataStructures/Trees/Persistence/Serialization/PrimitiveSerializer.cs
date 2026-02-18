using System;
using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

public sealed class PrimitiveSerializer<T> : INodeSerializer<T> where T : unmanaged
{
    public int SerializedSize => Marshal.SizeOf<T>();

    public T Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < SerializedSize)
            throw new ArgumentException($"Data span too small. Expected {SerializedSize} bytes.", nameof(data));
        
        return MemoryMarshal.Read<T>(data);
    }

    public void Serialize(T node, Span<byte> destination)
    {
        if (destination.Length < SerializedSize)
            throw new ArgumentException($"Destination span too small. Expected {SerializedSize} bytes.", nameof(destination));
        
        MemoryMarshal.Write(destination, ref node);
    }
}
