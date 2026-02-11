namespace Electra.Cms.Blocks.Core
{
    public class RichTextBlock : BlockDefinition
    {
        public override string Type => "RichText";
        public override string Name => "Rich Text";
        public override string Icon => "file-text";

        public override IEnumerable<BlockField> Fields => new List<BlockField>
        {
            new BlockField { Name = "Content", Label = "Content", Type = "RichText", Required = true }
        };
    }
}
