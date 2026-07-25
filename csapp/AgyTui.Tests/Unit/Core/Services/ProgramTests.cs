namespace AgyTui.Tests.Unit.Core.Services;

using AgyTui;
using AgyTui.UI.Core.Navigation;
using Xunit;

public class ProgramTests
{
    [Fact]
    public void Main_CommandThrowsException_ReturnsNonZeroExitCode()
    {
        int exitCode = Program.RunApp(["invalid-command-alias-xyz"]);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void CommandRouter_Execute_UnknownAlias_ReturnsNonZeroExitCode()
    {
        int exitCode = CommandRouter.Execute("non-existent-alias-999");
        Assert.Equal(1, exitCode);
    }
}
