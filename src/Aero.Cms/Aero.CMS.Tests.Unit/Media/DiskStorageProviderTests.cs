using System;
using System.IO;
using System.Threading.Tasks;
using Aero.CMS.Core.Media.Interfaces;
using Aero.CMS.Core.Media.Providers;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Media;

public class DiskStorageProviderTests : IDisposable
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly DiskStorageProvider _provider;
    private readonly string _tempRootPath;
    
    public DiskStorageProviderTests()
    {
        _tempRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRootPath);
        
        _webHostEnvironment = Substitute.For<IWebHostEnvironment>();
        _webHostEnvironment.WebRootPath.Returns(_tempRootPath);
        _webHostEnvironment.ContentRootPath.Returns(_tempRootPath);
        
        _provider = new DiskStorageProvider(_webHostEnvironment);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_tempRootPath))
        {
            Directory.Delete(_tempRootPath, recursive: true);
        }
    }
    
    [Fact]
    public void ProviderAlias_ShouldBe_Disk()
    {
        _provider.ProviderAlias.ShouldBe("disk");
    }
    
    [Fact]
    public async Task UploadAsync_ReturnsSuccessWithStorageKey()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileName = "test.txt";
        var contentType = "text/plain";
        
        var result = await _provider.UploadAsync(stream, fileName, contentType);
        
        result.Success.ShouldBeTrue();
        result.StorageKey.ShouldNotBeNullOrEmpty();
        result.Error.ShouldBeNull();
    }
    
    [Fact]
    public async Task UploadAsync_CreatesFile()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileName = "test.txt";
        var contentType = "text/plain";
        
        var result = await _provider.UploadAsync(stream, fileName, contentType);
        
        var filePath = Path.Combine(_tempRootPath, "wwwroot", "media", result.StorageKey!);
        File.Exists(filePath).ShouldBeTrue();
    }
    
    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var fileName = "test.txt";
        var contentType = "text/plain";
        
        var result = await _provider.UploadAsync(stream, fileName, contentType);
        var storageKey = result.StorageKey!;
        
        await _provider.DeleteAsync(storageKey);
        
        var filePath = Path.Combine(_tempRootPath, "wwwroot", "media", storageKey);
        File.Exists(filePath).ShouldBeFalse();
    }
    
    [Fact]
    public async Task DeleteAsync_OnNonExistentKey_DoesNotThrow()
    {
        var storageKey = "nonexistent/guid/file.txt";
        
        await _provider.DeleteAsync(storageKey);
        
        // No exception thrown
    }
    
    [Fact]
    public void GetPublicUrl_StartsWith_Media()
    {
        var storageKey = "guid123/test.jpg";
        
        var url = _provider.GetPublicUrl(storageKey);
        
        url.ShouldStartWith("/media/");
    }
    
    [Fact]
    public void GetPublicUrl_UsesForwardSlashes()
    {
        var storageKey = "guid123\\test.jpg";
        
        var url = _provider.GetPublicUrl(storageKey);
        
        url.ShouldBe("/media/guid123/test.jpg");
    }
}