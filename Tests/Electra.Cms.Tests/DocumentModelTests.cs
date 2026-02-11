using Electra.Cms.Models; // This will fail initially

namespace Electra.Cms.Tests
{
    public class DocumentModelTests
    {
        [Fact]
        public void SiteDocument_ShouldHaveRequiredProperties()
        {
            var site = new SiteDocument
            {
                Id = "sites/1",
                Name = "My Site",
                Hostnames = new List<string> { "example.com" },
                DefaultCulture = "en-US",
                Theme = "Default",
                Settings = new Dictionary<string, string> { { "Key", "Value" } },
                Version = 1
            };

            Assert.Equal("sites/1", site.Id);
            Assert.Equal("My Site", site.Name);
            Assert.Contains("example.com", site.Hostnames);
            Assert.Equal("en-US", site.DefaultCulture);
            Assert.Equal("Default", site.Theme);
            Assert.Equal("Value", site.Settings["Key"]);
            Assert.Equal(1, site.Version);
        }

        [Fact]
        public void PageDocument_ShouldHaveRequiredProperties()
        {
            var page = new PageDocument
            {
                Id = "pages/1",
                SiteId = "sites/1",
                Slug = "about-us",
                FullUrl = "/about-us",
                Template = "Standard",
                Metadata = new PageMetadata { Title = "About", SeoDescription = "SEO" },
                DynamicData = new Dictionary<string, object> { { "Custom", 123 } },
                Blocks = new List<BlockDocument>(),
                PublishedState = PagePublishedState.Published,
                Version = 1,
                LastModifiedUtc = DateTime.UtcNow
            };

            Assert.Equal("pages/1", page.Id);
            Assert.Equal("sites/1", page.SiteId);
            Assert.Equal("about-us", page.Slug);
            Assert.Equal("/about-us", page.FullUrl);
            Assert.Equal("Standard", page.Template);
            Assert.Equal("About", page.Metadata.Title);
            Assert.Equal(123, page.DynamicData["Custom"]);
            Assert.NotNull(page.Blocks);
            Assert.Equal(PagePublishedState.Published, page.PublishedState);
            Assert.Equal(1, page.Version);
        }

        [Fact]
        public void BlockDocument_ShouldHaveStructure()
        {
            var block = new BlockDocument
            {
                Type = "Hero",
                Version = 1,
                Data = new Dictionary<string, object> { { "Headline", "Welcome" } }
            };

            Assert.Equal("Hero", block.Type);
            Assert.Equal(1, block.Version);
            Assert.Equal("Welcome", block.Data["Headline"]);
        }
    }
}
