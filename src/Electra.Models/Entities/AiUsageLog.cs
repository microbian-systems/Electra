using Electra.Core.Entities;

namespace Electra.Models.Entities;

public class AiUsageLog : Entity
{
    public long UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}