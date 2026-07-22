using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace AgyTui;

public static class QuotaTracker
{
    public static (int windowCount, double percentUsed) CalculateWindowUsage(List<DateTime> timestamps, int limitWindowHours = 5, int maxLimit = 50)
    {
        var cutoff = DateTime.UtcNow.AddHours(-limitWindowHours);
        var activeCount = timestamps.Count(t => t >= cutoff);
        var pct = Math.Min(100.0, (double)activeCount / maxLimit * 100.0);
        return (activeCount, pct);
    }

    public static List<(DateTime TimeSlot, double RestoredPct)> ForecastQuotaRelease(List<DateTime> timestamps, int limitWindowHours = 5, int maxLimit = 50)
    {
        var cutoff = DateTime.UtcNow.AddHours(-limitWindowHours);
        var active = timestamps.Where(t => t >= cutoff).OrderBy(t => t).ToList();
        var result = new List<(DateTime, double)>();
        foreach (var group in active.GroupBy(t => new DateTime(t.AddHours(limitWindowHours).Ticks / TimeSpan.FromMinutes(15).Ticks * TimeSpan.FromMinutes(15).Ticks)))
        {
            var releasedCount = group.Count();
            var releasedPct = (double)releasedCount / maxLimit * 100.0;
            result.Add((group.Key, releasedPct));
        }
        return result;
    }

    public static async void TriggerLowQuotaWebhook(string accountName, double remainingPct, string webhookFile, double threshold = 10.0)
    {
        if (remainingPct > threshold) return;
        if (!File.Exists(webhookFile)) return;
        var url = File.ReadAllText(webhookFile).Trim();
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                event_type = "low_quota_alert",
                account = accountName,
                remaining_percent = remainingPct,
                threshold = threshold,
                timestamp = DateTime.UtcNow
            });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            await HttpClientProvider.Client.PostAsync(url, content);
        }
        catch { }
    }
}
