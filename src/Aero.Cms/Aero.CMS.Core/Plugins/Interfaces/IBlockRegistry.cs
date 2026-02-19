using System;
using System.Collections.Generic;

namespace Aero.CMS.Core.Plugins.Interfaces;

public interface IBlockRegistry
{
    void Register<TBlock, TView>() where TBlock : class where TView : class;
    void Register(string blockTypeAlias, Type viewComponentType);
    Type? Resolve(string blockTypeAlias);
    IDictionary<string, Type> GetAll();
}
