namespace Aero.Caching;

public record CacheEntry<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public CacheOptions Options { get; set; } = new();
}