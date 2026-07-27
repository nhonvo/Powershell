namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

public class QuotaTrackerEdgeCasesTests
{
    [Fact]
    public void CalculateWindowUsage_EmptyRequestHistory_ReturnsZeroPercentAndZeroCount()
    {
        var timestamps = new List<DateTime>();
        var (count, pct) = QuotaTracker.CalculateWindowUsage(timestamps, 5, 50);

        Assert.Equal(0, count);
        Assert.Equal(0.0, pct);
    }

    [Fact]
    public void CalculateWindowUsage_OverLimitTimestamps_ClampsUsagePercentageAt100()
    {
        var now = DateTime.UtcNow;
        var timestamps = Enumerable.Repeat(now.AddHours(-1), 60).ToList();
        var (count, pct) = QuotaTracker.CalculateWindowUsage(timestamps, 5, 50);

        Assert.Equal(60, count);
        Assert.Equal(100.0, pct);
    }

    [Fact]
    public void ForecastQuotaRelease_EmptyHistory_ReturnsEmptyList()
    {
        var forecast = QuotaTracker.ForecastQuotaRelease(new List<DateTime>(), 5, 50);
        Assert.Empty(forecast);
    }
}
