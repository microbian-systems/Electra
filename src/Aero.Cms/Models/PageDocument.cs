namespace Aero.Cms.Models
{
    public enum PagePublishedState
    {
        Draft,
        Published,
        Archived
    }

    public class PageMetadata
    {
        public string Title { get; set; }
        public string SeoDescription { get; set; }
    }

    public class PageDocument
    {
        public string Id { get; set; }
        public string SiteId { get; set; }
        public string Slug { get; set; }
        public string FullUrl { get; set; }
        public string Template { get; set; }
        public PageMetadata Metadata { get; set; }
        public Dictionary<string, object> DynamicData { get; set; } = new();
        public List<BlockDocument> Blocks { get; set; } = new();
        public PagePublishedState PublishedState { get; set; }
        public int Version { get; set; }
        public DateTime LastModifiedUtc { get; set; }
    }
}
