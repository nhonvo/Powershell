using AgyTui.UI.Core.Navigation.Interfaces;
using AgyTui.Infrastructure.Services;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.Core.Services;

public class ProgramTests
{
    [Fact]
    public void RunApp_UnknownCommand_ReturnsNonZeroExitCode()
    {
        int exitCode = Program.RunApp(["invalid-command-alias-xyz"]);
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void CommandRouter_Execute_UnknownAlias_ReturnsNonZeroExitCode()
    {
        var router = Bootstrapper.ServiceProvider.GetRequiredService<ICommandRouter>();
        int exitCode = router.Execute("non-existent-alias-999");
        Assert.Equal(1, exitCode);
    }
}


