using System;
using System.Collections.Generic;
using System.Reflection;
using Aero.CMS.Core.Plugins.Interfaces;

namespace Aero.CMS.Core.Plugins;

public class BlockRegistry : IBlockRegistry
{
    private readonly Dictionary<string, Type> _registry = new(StringComparer.OrdinalIgnoreCase);

    public void Register<TBlock, TView>() where TBlock : class where TView : class
    {
        var blockTypeProperty = typeof(TBlock).GetProperty("BlockType", BindingFlags.Public | BindingFlags.Static);
        var blockType = blockTypeProperty?.GetValue(null) as string;

        if (string.IsNullOrEmpty(blockType))
        {
            // Fallback to type name if static BlockType is missing
            blockType = typeof(TBlock).Name;
            if (blockType.EndsWith("Block"))
            {
                blockType = char.ToLower(blockType[0]) + blockType[1..];
            }
        }

        Register(blockType, typeof(TView));
    }

    public void Register(string blockTypeAlias, Type viewComponentType)
    {
        _registry[blockTypeAlias] = viewComponentType;
    }

    public Type? Resolve(string blockTypeAlias)
    {
        return _registry.TryGetValue(blockTypeAlias, out var type) ? type : null;
    }

    public IDictionary<string, Type> GetAll()
    {
        return new Dictionary<string, Type>(_registry);
    }
}
