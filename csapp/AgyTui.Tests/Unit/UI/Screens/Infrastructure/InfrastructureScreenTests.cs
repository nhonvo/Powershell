using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Obsidian;
using AgyTui.Infrastructure.Integrations.Sys;

namespace AgyTui.Tests.Unit.UI.Screens.Infrastructure;

public class InfrastructureScreenTests
{
    [Fact]
    public void AwsClient_StaticType_Exists()
    {
        Assert.NotNull(typeof(AwsClient));
    }

    [Fact]
    public void ObsidianBridge_StaticType_Exists()
    {
        Assert.NotNull(typeof(ObsidianBridge));
    }

    [Fact]
    public void AntigravityDeckHelper_StaticType_Exists()
    {
        Assert.NotNull(typeof(AntigravityDeckHelper));
    }
}
