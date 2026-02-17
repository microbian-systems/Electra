namespace Aero.Cms.Blocks
{
    public interface IBlockRegistry
    {
        IEnumerable<BlockDefinition> GetAllBlocks();
        BlockDefinition? GetBlock(string type);
    }
}
