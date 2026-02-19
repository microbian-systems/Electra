using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.Linq;

namespace Aero.CMS.Core.Content.Services;

public class MarkdownRendererService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRendererService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseEmphasisExtras()
            .UseYamlFrontMatter()
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }

    public (string body, IReadOnlyDictionary<string, string> frontmatter) ParseWithFrontmatter(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return (string.Empty, new Dictionary<string, string>());

        var document = Markdig.Markdown.Parse(markdown, _pipeline);
        var frontmatter = new Dictionary<string, string>();

        // Extract YAML frontmatter blocks
        var yamlBlocks = document.Descendants<YamlFrontMatterBlock>();
        foreach (var yamlBlock in yamlBlocks)
        {
            var yaml = yamlBlock.Lines.ToString();
            // Simple parsing: each line "key: value"
            foreach (var line in yaml.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || !trimmed.Contains(':'))
                    continue;
                var colonIndex = trimmed.IndexOf(':');
                var key = trimmed.Substring(0, colonIndex).Trim();
                var value = trimmed.Substring(colonIndex + 1).Trim();
                frontmatter[key] = value;
            }

        }

        // Get body by rendering the remaining document to HTML? Actually spec says body without YAML.
        // We'll return the markdown without YAML frontmatter lines.
        // Instead of trying to reconstruct markdown, we can get the text of remaining document.
        // Let's use Markdig's ToHtml on the modified document and then strip HTML? That's not correct.
        // Better to return the original markdown with YAML block removed.
        // For simplicity, we'll just return the original markdown after removing YAML lines.
        // We'll implement later.
        // For now, return empty body.
        var body = markdown;
        // Remove YAML block lines
        var lines = markdown.Split('\n').ToList();
        int start = -1, end = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == "---")
            {
                if (start == -1)
                    start = i;
                else
                {
                    end = i;
                    break;
                }
            }
        }
        if (start != -1 && end != -1)
        {
            lines.RemoveRange(start, end - start + 1);
            body = string.Join("\n", lines).Trim();
        }

        return (body, frontmatter);
    }
}