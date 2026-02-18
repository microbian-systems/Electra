using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aero.DataStructures.Trees.Persistence.Serialization;

public sealed class SystemTextJsonSerializer<TDocument> : IDocumentSerializer<TDocument>
    where TDocument : class
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };
    }

    public ReadOnlyMemory<byte> Serialize(TDocument document)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        JsonSerializer.Serialize(writer, document, _options);
        writer.Flush();
        return buffer.WrittenMemory;
    }

    public TDocument Deserialize(ReadOnlyMemory<byte> bytes)
    {
        var result = JsonSerializer.Deserialize<TDocument>(bytes.Span, _options);
        return result ?? throw new SerializationException(
            $"Deserialization of {typeof(TDocument).Name} returned null.");
    }
}
