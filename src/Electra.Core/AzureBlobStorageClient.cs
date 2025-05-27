using Microsoft.WindowsAzure.Storage;
using Electra.Core.Extensions;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Globalization;
using Electra.Common;
using Electra.Common.Extensions;
using Electra.Core.Helpers;
using ILogger = Serilog.ILogger;

namespace Electra.Core;

public class AzureBlobStorageClient : IBlobStorageClient
{
    private readonly ILogger log;

    public AzureBlobStorageClient(ILogger log) =>
        this.log = log; // ?? JobLog.GetLog();

    // todo - convert MemorySTream to Stream
    public void Post(MemoryStream ms, string filename, bool compress = true) =>
        PostAsync(ms, filename, Config.GetStorageConnectionString(),
            Config.GetSetting("FeedContainer"), compress).GetAwaiter().GetResult();

    public async Task PostAsync(MemoryStream ms, string filename, string connString, string path, bool compress = true, string contentType = "text/xml", bool forceLowerCase = true)
    {
        if (forceLowerCase)
            filename = filename.ToLower(CultureInfo.InvariantCulture); // filenames are case sensitive
        if (string.IsNullOrEmpty(filename))
            throw new ArgumentException($"{nameof(filename)} argument was not specified. the blob filename must have a value");

        if (string.IsNullOrEmpty(path))
        {
            log.Information($"blob storage container name was null. defaulting to feeds");
            path = "feeds";
        }

        path = path.ToLower();
        var acct = CloudStorageAccount.Parse(connString);
        log.Information($"getting blob storage for {acct.BlobStorageUri} and file {filename} - compressed = {compress}");
        var cbc = acct.CreateCloudBlobClient();
        //var blob = cbc.GetBlobReference(container + "/" + filename);
        var bsp = ParseContainerPath(path);

        var conref = cbc.GetContainerReference(bsp.Container);
        await conref.CreateIfNotExistsAsync();
        await conref.SetPermissionsAsync(new BlobContainerPermissions
        {
            PublicAccess = BlobContainerPublicAccessType.Blob
        });

        var dir = conref.GetLastDirectoryReference(bsp.FoldersList);
        var blob = dir?.GetBlockBlobReference(filename) ?? conref.GetBlockBlobReference(filename);
        if (compress)
            blob.Properties.ContentEncoding = "gzip";
        blob.Properties.ContentType = contentType;

        //blob.UploadFromStream(ms);
        //blob.UploadByteArray(ms.ToArray());
        if (!compress)
            await blob.UploadFromStreamAsync(ms);
        else
            await blob.UploadFromStreamAsync(ms); // todo - make this async compat
        //blob.UploadFromStream(ms.Compress());
        //blob.UploadByteArray(ms.Compress().ToArray());
        log.Information($"successfully uploaded blob storage file {filename} at {acct.BlobStorageUri} @ {path}");
    }



    protected BlobStoragePath ParseContainerPath(string path)
    {
        var paths = path.Replace("//", "/")
            .StripTrailingBackSlash()
            .Split('/').ToList();
        var bsp = new BlobStoragePath
        {
            Container = paths.FirstOrDefault()
        };
        bsp.FoldersList.AddRange(paths.Skip(1));
        return bsp;
    }
}