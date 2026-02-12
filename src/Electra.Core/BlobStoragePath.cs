using System.Collections.Generic;

namespace Electra.Core;

public class BlobStoragePath
{
    public string Container { get; set; } = "";
    public List<string> FoldersList { get; protected set; } = new();
}