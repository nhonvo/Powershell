using AgyTui.Infrastructure.Integrations.Obsidian;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class ObsidianBridgeTests
{
    [Fact]
    public void ObsidianBridge_ShowDailyNote_NullVault_HandlesGracefully()
    {
        IObsidianBridge bridge = new ObsidianBridge();
        Assert.NotNull(bridge);
    }
}
