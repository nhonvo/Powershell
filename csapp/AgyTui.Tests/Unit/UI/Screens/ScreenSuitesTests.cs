using AgyTui.Infrastructure.Di;
using AgyTui.UI.Screens.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.UI.Screens;

public class ScreenSuitesTests
{
    [Fact]
    public void ScreenSuites_ResolveFromDI_Successfully()
    {
        var career = Bootstrapper.ServiceProvider.GetRequiredService<ICareerSuite>();
        Assert.NotNull(career);

        var git = Bootstrapper.ServiceProvider.GetRequiredService<IGitNexusSuite>();
        Assert.NotNull(git);

        var ide = Bootstrapper.ServiceProvider.GetRequiredService<IIdeSuite>();
        Assert.NotNull(ide);

        var learn = Bootstrapper.ServiceProvider.GetRequiredService<ILearnSuite>();
        Assert.NotNull(learn);
    }
}
