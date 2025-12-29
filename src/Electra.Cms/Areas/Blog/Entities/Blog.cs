using Electra.Core.Entities;

namespace Electra.Web.BlogEngine.Entities;

public class Blog : Entity
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string[] Maintainers { get; set; } = [];
}