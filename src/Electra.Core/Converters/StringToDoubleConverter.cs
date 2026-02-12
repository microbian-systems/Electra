namespace Electra.Core.Json.Converters;

public class StringToDoubleConverter : JsonConverter<double>
{
    public override bool CanConvert(Type t) => t == typeof(double);

    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        double l;
        if (double.TryParse(value, out l))
        {
            return l;
        }
        return 0;
        //throw new Exception("Cannot unmarshal type double");
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToString(), options);
        return;
    }

    public static readonly StringToDoubleConverter Singleton = new StringToDoubleConverter();
}