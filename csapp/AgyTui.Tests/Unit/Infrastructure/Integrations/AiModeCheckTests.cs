namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

using AgyTui.Infrastructure.Integrations.Ai;
using Xunit;

public class AiModeCheckTests
{
    [Fact]
    public void ExplicitCloudOverride_ReportsCloud()
    {
        var (mode, reason) = AgyAiCore.ResolveAiMode("claude", "cloud");
        Assert.Equal("cloud", mode);
        Assert.Contains("explicit", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AutoMode_ReportsResolvedModeAndReason()
    {
        var (mode, reason) = AgyAiCore.ResolveAiMode("claude", null);
        Assert.NotNull(mode);
        Assert.NotNull(reason);
        Assert.NotEmpty(mode);
        Assert.NotEmpty(reason);
    }
}
