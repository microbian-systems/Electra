namespace Aero.Core.Extensions;

public static class ObjectExtensions
{
    public static string ToJson(this object obj) 
        => JsonSerializer.Serialize(obj);
    
    public static T? FromJson<T>(string json) where T : class
    {
        return JsonSerializer.Deserialize<T>(json);
    }
}