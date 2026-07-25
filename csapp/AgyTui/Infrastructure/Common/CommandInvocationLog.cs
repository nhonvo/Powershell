namespace AgyTui.Infrastructure.Common;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record CommandLogEntry(
    [property: JsonPropertyName("alias")] string Alias,
    [property: JsonPropertyName("timestampUtc")] string TimestampUtc,
    [property: JsonPropertyName("durationMs")] double DurationMs,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("errorType")] string? ErrorType);

public static class CommandInvocationLog
{
    private static readonly object _logLock = new();

    public static string LogFilePath => Path.Combine(AgyAccountCore.AgySourceHome, "command_activity_log.jsonl");

    public static void Record(string alias, TimeSpan duration, bool success, string? errorType = null)
    {
        try
        {
            var cmdEntry = CommandRegistry.GetByAlias(alias);
            var category = cmdEntry?.Category ?? "Unknown";

            var entry = new CommandLogEntry(
                alias,
                DateTime.UtcNow.ToString("o"),
                Math.Round(duration.TotalMilliseconds, 2),
                success,
                category,
                errorType);

            var json = JsonSerializer.Serialize(entry);
            lock (_logLock)
            {
                var filePath = LogFilePath;
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(filePath, json + Environment.NewLine);
            }
        }
        catch { }
    }
}
