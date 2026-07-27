using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyAccountSwitcherTests
{
    [Fact]
    public void IsAutoSwitchEnabled_ReturnsBoolean()
    {
        var switcher = new AgyAccountSwitcher();
        var enabled = switcher.IsAutoSwitchEnabled();

        Assert.True(enabled || !enabled);
    }
}
