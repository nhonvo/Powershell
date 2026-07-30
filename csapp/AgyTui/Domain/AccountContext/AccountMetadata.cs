using System.Text.Json.Serialization;

namespace AgyTui.Domain.AccountContext;

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

    [JsonPropertyName("RemainingWeekly")]
    public double? RemainingWeekly { get; set; }

    [JsonPropertyName("Remaining5H")]
    public double? Remaining5H { get; set; }

    [JsonPropertyName("TimeWeekly")]
    public string? TimeWeekly { get; set; }

    [JsonPropertyName("Time5H")]
    public string? Time5H { get; set; }
}
