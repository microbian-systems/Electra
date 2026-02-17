namespace Aero.Cms.Blocks.Core
{
    public class ImageBlock : BlockDefinition
    {
        public override string Type => "Image";
        public override string Name => "Image";
        public override string Icon => "image";

        public override IEnumerable<BlockField> Fields => new List<BlockField>
        {
            new BlockField { Name = "Url", Label = "Image URL", Type = "Image", Required = true },
            new BlockField { Name = "AltText", Label = "Alt Text", Type = "Text" },
            new BlockField { Name = "Caption", Label = "Caption", Type = "Text" }
        };
    }
}
