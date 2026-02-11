namespace Electra.Cms.Models
{
    public class BlockDocument
    {
        public string Type { get; set; }
        public int Version { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
