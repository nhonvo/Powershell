using AgyTui.UI.Core.Navigation.Interfaces;
using AgyTui.Infrastructure.Services;
using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.Core.Services;

public class CommandRouterEdgeCasesTests
{
    [Fact]
    public void Execute_UnknownAlias_ReturnsExitCodeOne()
    {
        var router = Bootstrapper.ServiceProvider.GetRequiredService<ICommandRouter>();
        int exitCode = router.Execute("non_existent_alias_999", Array.Empty<string>());
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Execute_NullArgs_DoesNotThrow()
    {
        var router = Bootstrapper.ServiceProvider.GetRequiredService<ICommandRouter>();
        int exitCode = router.Execute("unknown_command", null!);
        Assert.Equal(1, exitCode);
    }
}


