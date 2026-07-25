namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class ShowAiDashboardTests
{
    [Fact]
    public void ResolveAiMode_ClaudeAndCodex_ReturnsValidModeAndReason()
    {
        var (claudeMode, claudeReason) = AgyAiCore.ResolveAiMode("claude");
        var (codexMode, codexReason) = AgyAiCore.ResolveAiMode("codex");

        Assert.True(claudeMode == "cloud" || claudeMode == "local");
        Assert.NotEmpty(claudeReason);

        Assert.True(codexMode == "cloud" || codexMode == "local");
        Assert.NotEmpty(codexReason);
    }
}
