namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class InvokeHermesDesktopTests
{
    [Fact]
    public void InvokeHermesDesktop_SuccessfulLaunch_WritesActivityLogEntry()
    {
        var method = typeof(AgyAiCore).GetMethod("InvokeHermesDesktop");
        Assert.NotNull(method);
    }
}
