using System.Collections.Generic;

namespace Microbians.Core
{
    public class BlobStoragePath
    {
        public string Container { get; set; } = "";
        public List<string> FoldersList { get; protected set; } = new List<string>();
    }
}
