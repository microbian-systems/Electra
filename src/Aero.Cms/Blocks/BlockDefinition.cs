namespace Aero.Cms.Blocks
{
    public abstract class BlockDefinition
    {
        public abstract string Type { get; }
        public abstract string Name { get; }
        public virtual string Icon => "box";
        public virtual string Category => "Common";
        
        public virtual IEnumerable<BlockField> Fields => new List<BlockField>();
    }

    public class BlockField
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; } // Text, RichText, Image, etc.
        public bool Required { get; set; }
    }
}
