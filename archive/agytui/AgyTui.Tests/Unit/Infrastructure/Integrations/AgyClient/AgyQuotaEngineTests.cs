namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyQuotaEngineTests
{
    [Fact]
    public void QuotaEngine_CalculateRollingQuotas_ReturnsNonNullMetrics()
    {
        var engine = new AgyQuotaEngine();
        var metrics = engine.CalculateRollingQuotas("default");

        Assert.NotNull(metrics);
        Assert.True(metrics.RemainingWeekly >= 0);
        Assert.True(metrics.Remaining5H >= 0);
    }

    [Fact]
    public void QuotaEngine_GetAccountStats_ReturnsValidStats()
    {
        var engine = new AgyQuotaEngine();
        var stats = engine.GetAccountStats("default");

        Assert.NotNull(stats);
        Assert.NotNull(stats.JunctionStatus);
        Assert.NotNull(stats.PrivateSize);
    }
}
