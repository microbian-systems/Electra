using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aero.CMS.Core.Media.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Aero.CMS.Core.Media.Providers;

public class DiskStorageProvider : IMediaProvider
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private const string BaseMediaPath = "wwwroot/media";
    
    public DiskStorageProvider(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }
    
    public string ProviderAlias => "disk";
    
    public async Task<MediaUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = GenerateStorageKey(fileName);
            var filePath = GetFilePath(storageKey);
            var directory = Path.GetDirectoryName(filePath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            
            await using var file = File.Create(filePath);
            await fileStream.CopyToAsync(file, cancellationToken);
            
            return new MediaUploadResult
            {
                Success = true,
                StorageKey = storageKey
            };
        }
        catch (Exception ex)
        {
            return new MediaUploadResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
    
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(storageKey);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
    
    public string GetPublicUrl(string storageKey)
    {
        return "/media/" + storageKey.Replace('\\', '/');
    }
    
    private string GenerateStorageKey(string fileName)
    {
        var guid = Guid.NewGuid().ToString("N");
        return $"{guid}/{fileName}";
    }
    
    private string GetFilePath(string storageKey)
    {
        var rootPath = _webHostEnvironment.WebRootPath ?? _webHostEnvironment.ContentRootPath;
        return Path.Combine(rootPath, BaseMediaPath, storageKey);
    }
}