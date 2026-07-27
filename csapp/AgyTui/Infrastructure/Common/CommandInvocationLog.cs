namespace AgyTui.Infrastructure.Common;

using AgyTui.Core.Registries;
using AgyTui.Infrastructure.Integrations.AgyClient;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record CommandLogEntry(
    [property: JsonPropertyName("alias")] string Alias,
    [property: JsonPropertyName("timestampUtc")] string TimestampUtc,
    [property: JsonPropertyName("durationMs")] double DurationMs,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("activeAccount")] string ActiveAccount,
    [property: JsonPropertyName("errorType")] string? ErrorType
);

public static class CommandInvocationLog
{
    private static readonly object _logLock = new();

    public static string LogFilePath
    {
        get
        {
            return Path.Combine(AppPaths.LogsDir, "command_activity_log.jsonl");
        }
    }

    public static void Record(string alias, TimeSpan duration, bool success, string? errorType = null)
    {
        try
        {
            var cmdEntry = CommandRegistry.GetByAlias(alias);
            var category = cmdEntry?.Category ?? "Unknown";
            var activeAcc = AgyAccountCore.GetActiveAccount();

            var entry = new CommandLogEntry(
                alias,
                DateTime.UtcNow.ToString("o"),
                Math.Round(duration.TotalMilliseconds, 2),
                success,
                category,
                activeAcc,
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

    public static List<CommandLogEntry> GetRecentEntries(int count = 50)
    {
        var list = new List<CommandLogEntry>();
        lock (_logLock)
        {
            var filePath = LogFilePath;
            if (!File.Exists(filePath)) return list;

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines.TakeLast(count))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<CommandLogEntry>(line);
                        if (entry != null) list.Add(entry);
                    }
                    catch { }
                }
            }
            catch { }
        }
        return list;
    }
}
