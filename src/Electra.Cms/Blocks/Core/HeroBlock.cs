namespace Electra.Cms.Blocks.Core
{
    public class HeroBlock : BlockDefinition
    {
        public override string Type => "Hero";
        public override string Name => "Hero Section";
        public override string Icon => "image";

        public override IEnumerable<BlockField> Fields => new List<BlockField>
        {
            new BlockField { Name = "Title", Label = "Title", Type = "Text", Required = true },
            new BlockField { Name = "Subtitle", Label = "Subtitle", Type = "Text" },
            new BlockField { Name = "ImageUrl", Label = "Background Image URL", Type = "Image" },
            new BlockField { Name = "CtaText", Label = "CTA Text", Type = "Text" },
            new BlockField { Name = "CtaUrl", Label = "CTA URL", Type = "Url" }
        };
    }
}
