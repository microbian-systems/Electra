using Aero.CMS.Core.Content.Services;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Content;

public class MarkdownRendererServiceTests
{
    private readonly MarkdownRendererService _service;

    public MarkdownRendererServiceTests()
    {
        _service = new MarkdownRendererService();
    }

    [Fact]
    public void ToHtml_ConvertsParagraphToP()
    {
        var result = _service.ToHtml("Hello world");
        result.ShouldBe("<p>Hello world</p>\n");
    }

    [Fact]
    public void ToHtml_ConvertsHashToH1()
    {
        var result = _service.ToHtml("# Heading");
        result.ShouldBe("<h1>Heading</h1>\n");
    }

    [Fact]
    public void ToHtml_ConvertsBoldToStrong()
    {
        var result = _service.ToHtml("**bold**");
        result.ShouldBe("<p><strong>bold</strong></p>\n");
    }

    [Fact]
    public void ParseWithFrontmatter_ExtractsTitle()
    {
        var content = @"---
title: My Title
---
Body content";
        var (body, frontmatter) = _service.ParseWithFrontmatter(content);
        body.ShouldBe("Body content");
        frontmatter.ShouldContainKey("title");
        frontmatter["title"].ShouldBe("My Title");
    }

    [Fact]
    public void ParseWithFrontmatter_ReturnsBodyWithoutYaml()
    {
        var content = @"---
key: value
---
Body only";
        var (body, frontmatter) = _service.ParseWithFrontmatter(content);
        body.ShouldBe("Body only");
        frontmatter.ShouldContainKey("key");
        frontmatter["key"].ShouldBe("value");
    }

    [Fact]
    public void ParseWithFrontmatter_NoFrontmatter_ReturnsFullContentAsBody()
    {
        var content = "No frontmatter here";
        var (body, frontmatter) = _service.ParseWithFrontmatter(content);
        body.ShouldBe("No frontmatter here");
        frontmatter.ShouldBeEmpty();
    }
}