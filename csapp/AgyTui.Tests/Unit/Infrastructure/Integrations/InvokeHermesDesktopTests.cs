namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class InvokeHermesDesktopTests
{
    [Fact]
    public void InvokeHermesDesktop_Execution_ReturnsValidResultStatus()
    {
        var result = AgyAiCore.InvokeHermesDesktop([]);
        Assert.True(result == AgyAiCore.HermesResult.NotInstalled || result == AgyAiCore.HermesResult.Launched);
    }
}
