using System.IO;
using System.Threading.Tasks;

namespace Electra.Common;

public interface IBlobStorageClient
{
    void Post(MemoryStream ms, string filename, bool compress = true);
    Task PostAsync(MemoryStream ms, string filename, string connString, string container, bool compress = true, string contenttype = "text/xml", bool forceLowerCase = true);
}