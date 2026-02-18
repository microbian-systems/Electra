using System;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

public interface IDocumentSerializer<TDocument>
    where TDocument : class
{
    ReadOnlyMemory<byte> Serialize(TDocument document);
    TDocument Deserialize(ReadOnlyMemory<byte> bytes);
}

public sealed class SerializationException : Exception
{
    public SerializationException(string message) : base(message) { }
    public SerializationException(string message, Exception inner) : base(message, inner) { }
}
