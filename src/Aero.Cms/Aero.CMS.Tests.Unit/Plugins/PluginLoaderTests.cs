using System;
using System.IO;
using System.Linq;
using Aero.CMS.Core.Plugins;
using Aero.CMS.Core.Plugins.Interfaces;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Plugins;

public class PluginLoaderTests : IDisposable
{
    private readonly PluginLoader _sut;
    private readonly string _tempDirectory;

    public PluginLoaderTests()
    {
        _sut = new PluginLoader();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Fact]
    public void LoadFromDirectory_NonExistentPath_ReturnsEmpty()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent");

        // Act
        var plugins = _sut.LoadFromDirectory(nonExistentPath);

        // Assert
        plugins.ShouldBeEmpty();
    }

    [Fact]
    public void LoadFromDirectory_EmptyDirectory_ReturnsEmpty()
    {
        // Act
        var plugins = _sut.LoadFromDirectory(_tempDirectory);

        // Assert
        plugins.ShouldBeEmpty();
    }

    [Fact]
    public void LoadFromDirectory_InvalidDll_SkipsAndReturnsEmpty()
    {
        // Arrange
        var dummyDll = Path.Combine(_tempDirectory, "invalid.dll");
        File.WriteAllBytes(dummyDll, new byte[] { 0, 1, 2, 3 });

        // Act
        var plugins = _sut.LoadFromDirectory(_tempDirectory);

        // Assert
        plugins.ShouldBeEmpty();
    }

    [Fact]
    public void LoadedPlugins_InitiallyEmpty()
    {
        // Assert
        _sut.LoadedPlugins.ShouldBeEmpty();
    }

    [Fact(Skip = "PlatformNotSupported: CodeDom not available on this platform")]
    public void LoadFromDirectory_LoadsPlugin_WhenPluginAssemblyExists()
    {
        // Test skipped due to platform limitations
        // The plugin loading functionality is tested via integration tests.
        // This unit test is skipped to avoid platform-specific compilation issues.
    }
}