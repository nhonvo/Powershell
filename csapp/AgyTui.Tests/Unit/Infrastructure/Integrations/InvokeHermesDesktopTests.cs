using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class InvokeHermesDesktopTests
{
    [Fact]
    public void HermesProvider_InvokeHermes_ReturnsResultEnum()
    {
        var runner = new AiProcessRunner();
        var provider = new HermesProvider(runner);
        var result = provider.InvokeHermesDesktop(Array.Empty<string>());
        Assert.True(result == HermesResult.NotInstalled || result == HermesResult.Success || result == HermesResult.Error);
    }
}
