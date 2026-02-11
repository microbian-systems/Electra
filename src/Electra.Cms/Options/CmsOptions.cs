namespace Electra.Cms.Options
{
    public class CmsOptions
    {
        public bool EnableOutputCaching { get; set; } = true;
        public int DefaultCacheDurationSeconds { get; set; } = 60;
        public bool IncludeETag { get; set; } = true;
    }
}
