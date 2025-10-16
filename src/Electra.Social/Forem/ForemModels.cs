namespace Electra.Social.Forem;


    public class ArticleCreateRequest
    {
        public ArticleData Article { get; set; } = new();
    }

    public class ArticleData
    {
        public string Title { get; set; } = string.Empty;
        public string BodyMarkdown { get; set; } = string.Empty;
        public bool Published { get; set; }
        public string? Series { get; set; }
        public string? MainImage { get; set; }
        public string? CanonicalUrl { get; set; }
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public int? OrganizationId { get; set; }
    }

    public class ArticleCreateResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Slug { get; set; }
        public string? Path { get; set; }
        public string? CanonicalUrl { get; set; }
        public string? BodyMarkdown { get; set; }
        public string? BodyHtml { get; set; }
    }

