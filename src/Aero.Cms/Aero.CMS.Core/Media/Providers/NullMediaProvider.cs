using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aero.CMS.Core.Media.Interfaces;

namespace Aero.CMS.Core.Media.Providers;

public class NullMediaProvider : IMediaProvider
{
    public string ProviderAlias => "null";

    public Task<MediaUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MediaUploadResult
        {
            Success = true,
            StorageKey = "null-storage-key"
        });
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storageKey)
    {
        return "/media/placeholder.png";
    }
}
