using System.Text.Json.Serialization;

namespace AgyTui.Infrastructure.Integrations.AgyClient;

public sealed class AccountMetadata
{
    [JsonPropertyName("LastUsed")]
    public string LastUsed { get; set; } = "Never";

    [JsonPropertyName("UsageCount")]
    public int UsageCount { get; set; }

    [JsonPropertyName("QuotaStatus")]
    public string QuotaStatus { get; set; } = "OK";

    [JsonPropertyName("RequestHistory")]
    public List<string> RequestHistory { get; set; } = [];
}
