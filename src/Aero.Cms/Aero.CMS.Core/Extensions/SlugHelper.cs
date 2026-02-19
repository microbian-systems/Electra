using System.Text.RegularExpressions;

namespace Aero.CMS.Core.Extensions;

public static class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Lowercase
        string slug = input.ToLowerInvariant();

        // Replace spaces and underscores with hyphens
        slug = Regex.Replace(slug, @"[\s_]+", "-");

        // Remove all non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);

        // Collapse multiple hyphens
        slug = Regex.Replace(slug, @"\-+", "-");

        // Trim leading/trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }
}