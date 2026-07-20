using System;
using System.Collections.Generic;
using AgyTui;
using Xunit;

namespace AgyTuiApp.Tests;

public class QuotaMetricsTests
{
    [Fact]
    public void CalculateWindowUsage_AccuratelyCountsTimestampsWithinWindow()
    {
        var now = DateTime.UtcNow;
        var timestamps = new List<DateTime>
        {
            now.AddHours(-1),
            now.AddHours(-3),
            now.AddHours(-6) // Outside 5-hour window
        };

        var (count, pct) = QuotaTracker.CalculateWindowUsage(timestamps, limitWindowHours: 5, maxLimit: 50);

        Assert.Equal(2, count);
        Assert.Equal(4.0, pct); // 2 / 50 * 100% = 4%
    }

    [Fact]
    public void ForecastQuotaRelease_GroupsTimestampsByQuarterHourSlots()
    {
        var now = DateTime.UtcNow;
        var timestamps = new List<DateTime>
        {
            now.AddHours(-1),
            now.AddHours(-2)
        };

        var forecast = QuotaTracker.ForecastQuotaRelease(timestamps, limitWindowHours: 5, maxLimit: 50);

        Assert.NotNull(forecast);
        Assert.NotEmpty(forecast);
    }
}
