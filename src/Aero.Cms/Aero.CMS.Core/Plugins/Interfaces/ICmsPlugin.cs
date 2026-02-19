using System;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.CMS.Core.Plugins.Interfaces;

public interface ICmsPlugin
{
    string Alias { get; }
    Version Version { get; }
    string DisplayName { get; }
    
    void ConfigureServices(IServiceCollection services);
    void ConfigureBlocks(IBlockRegistry blockRegistry);
}