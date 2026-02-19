using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.CMS.Core.Media.Interfaces;

public record MediaUploadResult
{
    public bool Success { get; init; }
    public string? StorageKey { get; init; }
    public string? Error { get; init; }
}

public interface IMediaProvider
{
    string ProviderAlias { get; }
    
    Task<MediaUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
        
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    
    string GetPublicUrl(string storageKey);
}