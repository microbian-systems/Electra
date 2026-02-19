using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Seo.Checks;
using Aero.CMS.Core.Seo.Interfaces;
using Aero.CMS.Core.Seo.Models;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Seo;

public class SeoCheckTests
{
    public class PageTitleSeoCheckTests
    {
        private readonly PageTitleSeoCheck _check = new();

        [Fact]
        public async Task RunAsync_Pass_WhenTitleLengthBetween10And60()
        {
            var content = new ContentDocument { Name = "This is a perfect title length for SEO" };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Pass);
            result.Message.ShouldContain("optimal");
        }

        [Fact]
        public async Task RunAsync_Warning_WhenTitleTooShort()
        {
            var content = new ContentDocument { Name = "Short" };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Warning);
            result.Message.ShouldContain("too short");
        }

        [Fact]
        public async Task RunAsync_Warning_WhenTitleTooLong()
        {
            var content = new ContentDocument { Name = new string('a', 65) };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Warning);
            result.Message.ShouldContain("too long");
        }

        [Fact]
        public async Task RunAsync_Fail_WhenTitleMissing()
        {
            var content = new ContentDocument { Name = "" };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Fail);
            result.Message.ShouldContain("missing");
        }
    }

    public class MetaDescriptionSeoCheckTests
    {
        private readonly MetaDescriptionSeoCheck _check = new();

        [Fact]
        public async Task RunAsync_Pass_WhenDescriptionLengthBetween50And160()
        {
            var content = new ContentDocument
            {
                Properties = new Dictionary<string, object?>
                {
                    { "metaDescription", "This is a perfectly sized meta description that is between 50 and 160 characters in length, which is ideal for SEO purposes." }
                }
            };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Pass);
            result.Message.ShouldContain("optimal");
        }

        [Fact]
        public async Task RunAsync_Warning_WhenDescriptionTooShort()
        {
            var content = new ContentDocument
            {
                Properties = new Dictionary<string, object?>
                {
                    { "metaDescription", "Too short" }
                }
            };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Warning);
            result.Message.ShouldContain("too short");
        }

        [Fact]
        public async Task RunAsync_Warning_WhenDescriptionTooLong()
        {
            var content = new ContentDocument
            {
                Properties = new Dictionary<string, object?>
                {
                    { "metaDescription", new string('a', 200) }
                }
            };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Warning);
            result.Message.ShouldContain("too long");
        }

        [Fact]
        public async Task RunAsync_Fail_WhenDescriptionMissing()
        {
            var content = new ContentDocument { Properties = new Dictionary<string, object?>() };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Fail);
            result.Message.ShouldContain("missing");
        }
    }

    public class WordCountSeoCheckTests
    {
        private readonly WordCountSeoCheck _check = new();

        [Fact]
        public async Task RunAsync_Pass_WhenWordCount300OrMore()
        {
            var content = new ContentDocument
            {
                SearchText = string.Join(" ", Enumerable.Repeat("word", 300))
            };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Pass);
            result.Message.ShouldContain("meets recommended minimum");
        }

        [Fact]
        public async Task RunAsync_Warning_WhenWordCountLessThan300()
        {
            var content = new ContentDocument
            {
                SearchText = "Just a few words"
            };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Warning);
            result.Message.ShouldContain("below recommended minimum");
        }

        [Fact]
        public async Task RunAsync_Fail_WhenSearchTextEmpty()
        {
            var content = new ContentDocument { SearchText = "" };
            var context = new SeoCheckContext { Content = content };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Fail);
            result.Message.ShouldContain("empty");
        }
    }

    public class HeadingOneSeoCheckTests
    {
        private readonly HeadingOneSeoCheck _check = new();

        [Fact]
        public async Task RunAsync_Pass_WhenExactlyOneH1()
        {
            var context = new SeoCheckContext
            {
                Content = new ContentDocument(),
                RenderedHtml = "<h1>Main Heading</h1><p>Content</p>"
            };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Pass);
            result.Message.ShouldContain("exactly one");
        }

        [Fact]
        public async Task RunAsync_Warning_WhenMultipleH1()
        {
            var context = new SeoCheckContext
            {
                Content = new ContentDocument(),
                RenderedHtml = "<h1>First</h1><h1>Second</h1>"
            };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Warning);
            result.Message.ShouldContain("Multiple");
        }

        [Fact]
        public async Task RunAsync_Fail_WhenNoH1()
        {
            var context = new SeoCheckContext
            {
                Content = new ContentDocument(),
                RenderedHtml = "<p>No headings here</p>"
            };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Fail);
            result.Message.ShouldContain("No H1");
        }

        [Fact]
        public async Task RunAsync_Info_WhenRenderedHtmlNull()
        {
            var context = new SeoCheckContext
            {
                Content = new ContentDocument(),
                RenderedHtml = null
            };
            
            var result = await _check.RunAsync(context);
            
            result.Status.ShouldBe(SeoCheckStatus.Info);
            result.Message.ShouldContain("not available");
        }
    }
}