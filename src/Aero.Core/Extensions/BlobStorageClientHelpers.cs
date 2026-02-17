using Microsoft.WindowsAzure.Storage.Blob;

namespace Aero.Core.Extensions;

public static class BlobStorageClientHelpers
{
    public static CloudBlobDirectory GetLastDirectoryReference(this CloudBlobContainer container, List<string> folders)
    {
        var dirs = new List<CloudBlobDirectory>();
        foreach (var folder in folders)
        {
            var directory = container.GetDirectoryReference(folder);
            dirs.Add(directory);
        }

        return dirs.Count > 0 ? dirs.Last() : null;
    }
}