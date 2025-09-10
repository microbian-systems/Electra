using Electra.Core.Entities;

namespace Electra.Services.Features;

public class Features : Entity
{
    public string Application { get; set; }
    public string Version { get; set; }
    public string Feature { get; set; }
    public string Description { get; set; }
    public bool Toggled { get; set; }
    public string Value { get; set; }
}