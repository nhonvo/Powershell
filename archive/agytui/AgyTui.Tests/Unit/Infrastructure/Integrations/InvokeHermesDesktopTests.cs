using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class InvokeHermesDesktopTests
{
    private class DummyAiProcessRunner : IAiProcessRunner
    {
        public bool Invoked { get; private set; }
        public string ResolveProxyScriptPath() => "";
        public void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
        {
            Invoked = true;
        }
        public string RunCapture(string exe, string args) => "";
    }

    [Fact]
    public void HermesProvider_InvokeHermes_ReturnsResultEnum()
    {
        var runner = new DummyAiProcessRunner();
        var provider = new HermesProvider(runner);
        var result = provider.InvokeHermesDesktop(Array.Empty<string>());
        Assert.True(result == HermesResult.NotInstalled || result == HermesResult.Success || result == HermesResult.Error);
    }
}
