using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyAccountStatsProviderTests
{
    [Fact]
    public void GetAccountStats_ReturnsValidStats()
    {
        var provider = new AgyAccountStatsProvider();
        var stats = provider.GetAccountStats("default");

        Assert.NotNull(stats);
        Assert.NotNull(stats.JunctionStatus);
        Assert.NotNull(stats.PrivateSize);
    }
}
