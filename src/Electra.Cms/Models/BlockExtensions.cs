namespace Electra.Cms.Models
{
    public static class BlockExtensions
    {
        public static T GetValue<T>(this BlockDocument block, string key, T defaultValue = default)
        {
            if (block.Data.TryGetValue(key, out var value))
            {
                if (value is T tValue) return tValue;
                // Simple conversion if needed, but risky.
                // Assuming types are correct from storage.
                try 
                {
                    return (T)System.Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
