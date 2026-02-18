using System;
using System.Buffers.Binary;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

/// <summary>
/// Serializes and deserializes BST nodes.
/// </summary>
/// <typeparam name="T">The type of the value. Must be unmanaged.</typeparam>
public sealed class BstNodeSerializer<T> : INodeSerializer<BstNode<T>> where T : unmanaged
{
    private readonly int _valueSize;
    private readonly int _serializedSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="BstNodeSerializer{T}"/> class.
    /// </summary>
    public BstNodeSerializer()
    {
        _valueSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
        _serializedSize = sizeof(long) + _valueSize + sizeof(long) * 3 + sizeof(byte);
        
        // Round up to 8-byte alignment
        _serializedSize = (_serializedSize + 7) & ~7;
    }

    /// <inheritdoc />
    public int SerializedSize => _serializedSize;

    /// <inheritdoc />
    public BstNode<T> Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length < _serializedSize)
            throw new ArgumentException($"Data span too small. Expected {_serializedSize} bytes.", nameof(data));

        int offset = 0;
        
        var id = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, sizeof(long)));
        offset += sizeof(long);
        
        var value = System.Runtime.InteropServices.MemoryMarshal.Read<T>(data.Slice(offset, _valueSize));
        offset += _valueSize;
        
        var leftId = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, sizeof(long)));
        offset += sizeof(long);
        
        var rightId = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, sizeof(long)));
        offset += sizeof(long);
        
        var parentId = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset, sizeof(long)));
        offset += sizeof(long);
        
        var isRed = data[offset] != 0;
        
        return new BstNode<T>(id, value, leftId, rightId, parentId, isRed);
    }

    /// <inheritdoc />
    public void Serialize(BstNode<T> node, Span<byte> destination)
    {
        if (destination.Length < _serializedSize)
            throw new ArgumentException($"Destination span too small. Expected {_serializedSize} bytes.", nameof(destination));

        int offset = 0;
        
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(offset, sizeof(long)), node.Id);
        offset += sizeof(long);
        
        var value = node.Value;
        System.Runtime.InteropServices.MemoryMarshal.Write(destination.Slice(offset, _valueSize), ref value);
        offset += _valueSize;
        
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(offset, sizeof(long)), node.LeftId);
        offset += sizeof(long);
        
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(offset, sizeof(long)), node.RightId);
        offset += sizeof(long);
        
        BinaryPrimitives.WriteInt64LittleEndian(destination.Slice(offset, sizeof(long)), node.ParentId);
        offset += sizeof(long);
        
        destination[offset] = node.IsRed ? (byte)1 : (byte)0;
    }
}
