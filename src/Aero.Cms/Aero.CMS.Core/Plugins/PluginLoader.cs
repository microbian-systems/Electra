using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Aero.CMS.Core.Plugins.Interfaces;

namespace Aero.CMS.Core.Plugins;

public class PluginLoader : IDisposable
{
    private readonly List<PluginLoadContext> _loadContexts = new();
    private readonly List<ICmsPlugin> _loadedPlugins = new();

    public IReadOnlyList<ICmsPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public IReadOnlyList<ICmsPlugin> LoadFromDirectory(string path)
    {
        if (!Directory.Exists(path))
            return Array.Empty<ICmsPlugin>();

        var pluginFiles = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly);
        var loaded = new List<ICmsPlugin>();

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                var context = new PluginLoadContext(pluginFile);
                _loadContexts.Add(context);

                var assembly = context.LoadFromAssemblyPath(pluginFile);
                var pluginTypes = assembly.GetExportedTypes()
                    .Where(t => typeof(ICmsPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    if (Activator.CreateInstance(pluginType) is ICmsPlugin plugin)
                    {
                        loaded.Add(plugin);
                        _loadedPlugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException || ex is FileNotFoundException)
            {
                // Skip invalid assemblies
                continue;
            }
            catch (Exception)
            {
                // For other errors, continue with next assembly
                continue;
            }
        }

        return loaded.AsReadOnly();
    }

    public void Dispose()
    {
        foreach (var context in _loadContexts)
        {
            context.Unload();
        }
        _loadContexts.Clear();
        _loadedPlugins.Clear();
        GC.SuppressFinalize(this);
    }

    internal class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
        }
    }
}