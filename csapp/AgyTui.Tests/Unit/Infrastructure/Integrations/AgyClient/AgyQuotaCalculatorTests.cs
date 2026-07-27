using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyQuotaCalculatorTests
{
    [Fact]
    public void CalculateRollingQuotas_ReturnsNonNullMetrics()
    {
        var calculator = new AgyQuotaCalculator();
        var metrics = calculator.CalculateRollingQuotas("default");

        Assert.NotNull(metrics);
        Assert.True(metrics.RemainingWeekly >= 0);
        Assert.True(metrics.Remaining5H >= 0);
    }
}
