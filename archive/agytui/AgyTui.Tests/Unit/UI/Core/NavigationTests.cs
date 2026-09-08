using AgyTui.Infrastructure.Di;
using AgyTui.UI.Core.Navigation.Abstractions;
using AgyTui.UI.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.UI.Core;

public class NavigationTests
{
    [Fact]
    public void NavigationInterfaces_ResolveFromDI_Successfully()
    {
        var navigator = Bootstrapper.ServiceProvider.GetRequiredService<ICcNavigator>();
        Assert.NotNull(navigator);

        var palette = Bootstrapper.ServiceProvider.GetRequiredService<ICommandPalette>();
        Assert.NotNull(palette);

        var subPageNav = Bootstrapper.ServiceProvider.GetRequiredService<ISubPageNavigator>();
        Assert.NotNull(subPageNav);
    }

    [Fact]
    public void SubPageNavigator_ProcessSearchKey_HandlesBackspaceAndInput()
    {
        var subPageNav = Bootstrapper.ServiceProvider.GetRequiredService<ISubPageNavigator>();

        var resultAdd = subPageNav.ProcessSearchKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false), "test");
        Assert.Equal("testa", resultAdd);

        var resultBack = subPageNav.ProcessSearchKey(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false), "testa");
        Assert.Equal("test", resultBack);
    }
}

