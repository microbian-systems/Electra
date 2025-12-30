using System.ComponentModel.DataAnnotations;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

public class AiUsageLog : Entity
{
    public string UserId { get; set; } = string.Empty;
    [MaxLength(8000)]
    public string Provider { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}