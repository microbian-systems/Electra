using System.Collections.Generic;

namespace Electra.Cms.Blocks
{
    public interface IBlockRegistry
    {
        IEnumerable<BlockDefinition> GetAllBlocks();
        BlockDefinition? GetBlock(string type);
    }
}
