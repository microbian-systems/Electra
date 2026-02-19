using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Media.Models;

public enum MediaType
{
    Image,
    Video,
    Document,
    Audio,
    Other
}

public class MediaDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public MediaType MediaType { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public string? ParentFolderId { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}