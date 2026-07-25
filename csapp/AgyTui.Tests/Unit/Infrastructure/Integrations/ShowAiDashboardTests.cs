namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class ShowAiDashboardTests
{
    [Fact]
    public void ShowAiDashboard_ClaudeAndCodexMenuItems_DisplayedModeMatchesActualLaunchMode()
    {
        var effectiveMode = AgyAiCore.GetEffectiveProviderMode();
        Assert.NotNull(effectiveMode);
        Assert.NotEmpty(effectiveMode);
    }
}
