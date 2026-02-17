namespace Aero.Core;

    public class JobResponse
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; } = Guid.NewGuid().ToString().Replace("-", "");
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("info")]
        public List<string> Info { get; } = new();
        [JsonPropertyName("errors")]
        public List<string> Errors { get; } = new();
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; } = new();
        public override string ToString() => this.ToString(false);
    }