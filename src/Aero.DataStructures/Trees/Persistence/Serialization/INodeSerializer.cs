using System;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

/// <summary>
/// Serializes and deserializes tree nodes to and from fixed-size byte spans.
/// </summary>
/// <typeparam name="TNode">The type of the node to serialize.</typeparam>
public interface INodeSerializer<TNode>
{
    /// <summary>
    /// Gets the fixed byte size of one serialized node.
    /// </summary>
    int SerializedSize { get; }

    /// <summary>
    /// Deserializes a node from a byte span.
    /// </summary>
    /// <param name="data">The byte span containing serialized data.</param>
    /// <returns>The deserialized node.</returns>
    TNode Deserialize(ReadOnlySpan<byte> data);

    /// <summary>
    /// Serializes a node to a byte span.
    /// </summary>
    /// <param name="node">The node to serialize.</param>
    /// <param name="destination">The destination span. Length must equal <see cref="SerializedSize"/>.</param>
    void Serialize(TNode node, Span<byte> destination);
}
