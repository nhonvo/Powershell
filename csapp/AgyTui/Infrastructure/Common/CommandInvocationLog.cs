using System.Text.Json;
using System.Text.Json.Serialization;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Common;

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
    private static readonly object LogLock = new();

    public static string LogFilePath
    {
        get
        {
            return Path.Combine(AppPaths.LogsDir, "command_activity_log.jsonl");
        }
    }

    private static readonly Func<IAgyAccountStore> _accountStoreFactory = () => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>();

    public static void Record(string alias, TimeSpan duration, bool success, string? errorType = null)
    {
        try
        {
            var cmdEntry = CommandRegistry.GetByAlias(alias);
            var category = cmdEntry?.Category ?? "Unknown";
            var activeAcc = _accountStoreFactory().GetActiveAccount();

            var entry = new CommandLogEntry(
                alias,
                DateTime.UtcNow.ToString("o"),
                Math.Round(duration.TotalMilliseconds, 2),
                success,
                category,
                activeAcc,
                errorType);

            var json = JsonSerializer.Serialize(entry);
            lock (LogLock)
            {
                var filePath = LogFilePath;
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(filePath, json + Environment.NewLine);
            }
        }
        catch (Exception) { }
    }

    public static List<CommandLogEntry> GetRecentEntries(int count = 50)
    {
        var list = new List<CommandLogEntry>();
        lock (LogLock)
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
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
        return list;
    }
}
