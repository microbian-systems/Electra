using System.Text.Json.Serialization;

namespace Electra.Core.Json.Converters;

public class StringToLongConverter : JsonConverter<long>
{
    public override bool CanConvert(Type t) => t == typeof(long);

    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        long l;
        if (Int64.TryParse(value, out l))
        {
            return l;
        }

        throw new Exception("Cannot unmarshal type long");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToString(), options);
        return;
    }

    public static readonly StringToLongConverter Singleton = new StringToLongConverter();
}