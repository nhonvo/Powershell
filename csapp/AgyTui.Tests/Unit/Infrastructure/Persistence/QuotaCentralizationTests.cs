namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

using AgyTui.Core.Models;
using AgyTui.Infrastructure.Integrations.AgyClient;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

[Collection("Sequential")]
public class QuotaCentralizationTests
{
    [Fact]
    public void CalculateRollingQuotas_5HourAndWeekly_UseSameCodePath()
    {
        var now = DateTime.UtcNow;
        var timestamps = new List<DateTime>
        {
            now.AddHours(-1),
            now.AddHours(-3),
            now.AddDays(-2),
            now.AddDays(-5)
        };

        var (c5, u5) = QuotaTracker.CalculateWindowUsage(timestamps, 5, 50);
        var (cw, uw) = QuotaTracker.CalculateWindowUsage(timestamps, 168, 1000);

        Assert.Equal(2, c5);
        Assert.Equal(4, cw);
        Assert.Equal(4.0, u5); // 2/50 * 100
        Assert.Equal(0.4, uw); // 4/1000 * 100
    }

    [Fact]
    public void ForecastImplementations_AreUnified_NotDivergentDuplicates()
    {
        var now = DateTime.UtcNow;
        var timestamps = new List<DateTime> { now.AddHours(-2), now.AddHours(-2) };

        var forecast = QuotaTracker.ForecastQuotaRelease(timestamps, 5, 50);
        Assert.Single(forecast);
        Assert.Equal(2, forecast[0].ReqsReleased);
    }

    [Fact]
    public void CalculateRollingQuotasForAgent_ClaudeProvider_ReturnsRealDataNotHardcoded100()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_quota_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var originalHome = Config.Current.System.AgySourceHome;
        try
        {
            Config.Current.System.AgySourceHome = tempDir;
            Config.Save();
            var logPath = Path.Combine(tempDir, "ai_activity_log.jsonl");

            var nowStr = DateTime.UtcNow.ToString("o");
            var entries = new[]
            {
                $"{{\"Timestamp\":\"{nowStr}\",\"Agent\":\"Claude\",\"Mode\":\"cloud\",\"DurationMs\":500,\"Success\":true,\"Account\":\"test\"}}",
                $"{{\"Timestamp\":\"{nowStr}\",\"Agent\":\"Claude\",\"Mode\":\"cloud\",\"DurationMs\":600,\"Success\":true,\"Account\":\"test\"}}"
            };
            File.WriteAllLines(logPath, entries);

            var quota = AgyAccountCore.CalculateRollingQuotasForAgent("Claude");
            Assert.Equal(2, quota.Count5H);
            Assert.True(quota.Remaining5H < 100.0);
        }
        finally
        {
            Config.Current.System.AgySourceHome = originalHome;
            Config.Save();
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
        }
    }
}
