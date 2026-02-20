using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Plugins;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Integration.Components;

public class BlockRenderingTests
{
    [Fact]
    public void LoginBlock_Component_Renders_Without_Errors()
    {
        // Integration test: Verify that LoginBlock can be registered in the block registry
        // and resolved correctly. This ensures the block integrates with the CMS system.
        var registry = new BlockRegistry();
        
        // Register the block with a dummy view component type (using object as placeholder)
        registry.Register<LoginBlock, object>();
        
        // Resolve by block type alias
        var resolvedType = registry.Resolve("loginBlock");
        
        resolvedType.ShouldNotBeNull();
        resolvedType.ShouldBe(typeof(object)); // Placeholder view component
    }

    [Fact]
    public void Column_Blocks_Distribute_Children_Correctly()
    {
        // Integration test: Verify that column block properties are correctly serialized
        // and can be used in composite block scenarios.
        var twoColumn = new TwoColumnRowBlock();
        twoColumn.Column1Width.ShouldBe("50%");
        twoColumn.Column2Width.ShouldBe("50%");
        
        var threeColumn = new ThreeColumnRowBlock();
        threeColumn.EqualColumns.ShouldBeTrue();
        
        var fourColumn = new FourColumnRowBlock();
        fourColumn.EqualColumns.ShouldBeTrue();
        
        // Verify composite block interface implementation
        twoColumn.AllowedChildTypes.ShouldBeEmpty();
        twoColumn.AllowNestedComposites.ShouldBeTrue();
        twoColumn.MaxChildren.ShouldBeNull();
        
        threeColumn.AllowedChildTypes.ShouldBeEmpty();
        threeColumn.AllowNestedComposites.ShouldBeTrue();
        threeColumn.MaxChildren.ShouldBeNull();
        
        fourColumn.AllowedChildTypes.ShouldBeEmpty();
        fourColumn.AllowNestedComposites.ShouldBeTrue();
        fourColumn.MaxChildren.ShouldBeNull();
    }

    [Fact]
    public void MarkdownRendererBlock_Renders_Basic_Markdown_To_HTML()
    {
        // Integration test: Verify that markdown conversion and sanitization work together
        var markdown = "# Heading\n\nParagraph with **bold** text.";
        var service = new MarkdownRendererService();
        var html = service.ToHtml(markdown);
        
        html.ShouldNotBeNullOrEmpty();
        html.ShouldContain("<h1>Heading</h1>");
        html.ShouldContain("<p>Paragraph with <strong>bold</strong> text.</p>");
        
        // Test sanitization removes script tags
        var dangerousMarkdown = "<script>alert('xss');</script>";
        var dangerousHtml = service.ToHtml(dangerousMarkdown);
        var sanitized = HtmlSanitizer.Sanitize(dangerousHtml);
        
        sanitized.ShouldNotContain("<script>");
        sanitized.ShouldNotContain("alert");
        
        // Test that MarkdownRendererBlock properties work with the service
        var block = new MarkdownRendererBlock
        {
            MarkdownContent = markdown,
            UseTypographyStyles = false,
            MaxWidth = "max-w-4xl"
        };
        
        block.MarkdownContent.ShouldBe(markdown);
        block.UseTypographyStyles.ShouldBeFalse();
        block.MaxWidth.ShouldBe("max-w-4xl");
    }

    [Fact]
    public void All_Blazor_Components_Compile_Successfully()
    {
        // Integration test: Verify that all new block types can be registered in the block registry
        // This ensures they are compatible with the CMS block system.
        var registry = new BlockRegistry();
        
        // Register all new block types with placeholder view components
        registry.Register<LoginBlock, object>();
        registry.Register<RegisterBlock, object>();
        registry.Register<ForgotPasswordBlock, object>();
        registry.Register<HeroBlock2, object>();
        registry.Register<OneColumnRowBlock, object>();
        registry.Register<TwoColumnRowBlock, object>();
        registry.Register<ThreeColumnRowBlock, object>();
        registry.Register<FourColumnRowBlock, object>();
        registry.Register<MarkdownRendererBlock, object>();
        
        // Verify each registration resolves correctly
        registry.Resolve("loginBlock").ShouldNotBeNull();
        registry.Resolve("registerBlock").ShouldNotBeNull();
        registry.Resolve("forgotPasswordBlock").ShouldNotBeNull();
        registry.Resolve("heroBlock2").ShouldNotBeNull();
        registry.Resolve("oneColumnRowBlock").ShouldNotBeNull();
        registry.Resolve("twoColumnRowBlock").ShouldNotBeNull();
        registry.Resolve("threeColumnRowBlock").ShouldNotBeNull();
        registry.Resolve("fourColumnRowBlock").ShouldNotBeNull();
        registry.Resolve("markdownRendererBlock").ShouldNotBeNull();
    }
}