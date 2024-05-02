using System.Collections.Generic;

namespace Microbians.Core;

    public class JobResponse
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; } = Guid.NewGuid().ToString().Replace("-", "");
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("info")]
        public List<string> Info { get; } = new List<string>();
        [JsonPropertyName("errors")]
        public List<string> Errors { get; } = new List<string>();
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; } = new List<string>();
        public override string ToString() => this.ToString(false);
    }
