using System;

namespace Electra.Common.Caching
{
    public interface ICacheOptions
    {
        TimeSpan Expiry { get; set; }
        ExpirationType ExpiryType { get; set; }
    }

    public enum ExpirationType
    {
        Sliding,
        Absolute
    }
    
    public sealed class CacheOptions : ICacheOptions
    {
        public ExpirationType ExpiryType { get; set; } = ExpirationType.Absolute;
        public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(15);
        
        public static CacheOptions SetSlidingExpiration(int minutes)
        {
            var opts = new CacheOptions
            {
                ExpiryType = ExpirationType.Sliding, 
                Expiry = TimeSpan.FromMinutes(minutes)
            };
            return opts;
        }
        
        public static CacheOptions SetAbsoluteExpiration(int minutes)
        {
            var opts = new CacheOptions {
                ExpiryType = ExpirationType.Absolute, 
                Expiry = TimeSpan.FromMinutes(minutes)
            };
            return opts;
        }
        
        public static CacheOptions SetSlidingExpiration(TimeSpan span)
        {
            var opts = new CacheOptions
            {
                ExpiryType = ExpirationType.Sliding, 
                Expiry = span
            };
            return opts;
        }
        
        public static CacheOptions SetAbsoluteExpiration(TimeSpan span)
        {
            var opts = new CacheOptions
            {
                ExpiryType = ExpirationType.Absolute, 
                Expiry = span
            };
            return opts;
        }
    }

    public static class CacheOptionExtension
    {
        public static void SetSlidingExpiration(this ICacheOptions opts, int minutes)
        {
            opts.ExpiryType = ExpirationType.Sliding;
            opts.Expiry = TimeSpan.FromMinutes(minutes);
        }
        
        public static void SetAbsoluteExpiration(this ICacheOptions opts, int minutes)
        {
            opts.ExpiryType = ExpirationType.Absolute;
            opts.Expiry = TimeSpan.FromMinutes(minutes);
        }
    }
}