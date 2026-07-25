namespace AgyTui.Tests.Unit.Core.Services;

using AgyTui.UI.Core.Navigation;
using Xunit;

public class CommandRouterEdgeCasesTests
{
    [Fact]
    public void Execute_UnknownAlias_ReturnsExitCodeOne()
    {
        int exitCode = CommandRouter.Execute("non_existent_alias_999", Array.Empty<string>());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Execute_NullArgs_DoesNotThrow()
    {
        int exitCode = CommandRouter.Execute("unknown_command", null!);
        Assert.Equal(1, exitCode);
    }
}
