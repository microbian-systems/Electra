using Microsoft.AspNetCore.Components.Forms;

namespace ZauberCMS.Core.Media.Parameters;

public class SaveMediaParameters
{
    public IBrowserFile? FileToSave { get; set; }
    public Media.Models.Media? MediaToSave { get; set; }
    public string? ParentFolderId { get; set; }
    public bool IsUpdate { get; set; }
}