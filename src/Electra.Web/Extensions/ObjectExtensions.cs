namespace Electra.Common.Web.Extensions;

public static class ObjectExtensions
{
    public static string ToJson(this object obj) => JsonSerializer.Serialize(obj);
}