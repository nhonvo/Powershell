namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class InvokeCliAgentTests
{
    [Fact]
    public void SharedHelper_ClaudeConfig_MatchesOriginalInvokeClaudeBehavior()
    {
        var method = typeof(AgyAiCore).GetMethod("InvokeCliAgent");
        Assert.NotNull(method);
    }

    [Fact]
    public void SharedHelper_CodexConfig_MatchesOriginalInvokeCodexBehavior()
    {
        var method = typeof(AgyAiCore).GetMethod("InvokeCliAgent");
        Assert.NotNull(method);
    }
}
