using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Electra.Cms.Blocks
{
    public class BlockRegistry : IBlockRegistry
    {
        private readonly Dictionary<string, BlockDefinition> _blocks = new();

        public BlockRegistry()
        {
            ScanForBlocks();
        }

        private void ScanForBlocks()
        {
            // Scan the current assembly and referencing assemblies
            // For simplicity, just scanning the current AppDomain or Entry Assembly + Cms Assembly
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.StartsWith("Electra"));

            foreach (var assembly in assemblies)
            {
                var blockTypes = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(BlockDefinition)) && !t.IsAbstract);

                foreach (var type in blockTypes)
                {
                    var block = (BlockDefinition)Activator.CreateInstance(type);
                    if (!_blocks.ContainsKey(block.Type))
                    {
                        _blocks.Add(block.Type, block);
                    }
                }
            }
        }

        public IEnumerable<BlockDefinition> GetAllBlocks() => _blocks.Values;

        public BlockDefinition? GetBlock(string type) => _blocks.ContainsKey(type) ? _blocks[type] : null;
    }
}
