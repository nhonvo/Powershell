using AgyTui.Infrastructure.Integrations.Ai.Providers;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class InvokeCliAgentTests
{
    [Fact]
    public void ClaudeProvider_CanBeInstantiatedWithInjectedRunner()
    {
        var runner = new AgyTui.Infrastructure.Integrations.Ai.Services.AiProcessRunner();
        var provider = new ClaudeProvider(runner);
        Assert.NotNull(provider);
    }
}
