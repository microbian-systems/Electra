using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electra.Core.Json.Converters;

public class StringToDecimalConverter : JsonConverter<decimal>
{
    public override bool CanConvert(Type t) => t == typeof(decimal);

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        decimal l;
        if (decimal.TryParse(value, out l))
        {
            return l;
        }

        throw new Exception("Cannot unmarshal type decimal");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToString(), options);
        return;
    }

    public static readonly StringToDecimalConverter Singleton = new StringToDecimalConverter();
}