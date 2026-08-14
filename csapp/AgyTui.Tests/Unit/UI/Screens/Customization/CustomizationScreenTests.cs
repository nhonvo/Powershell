using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.UI.Core.Abstractions;
using AgyTui.UI.Screens.Customization;
using AgyTui.UI.Screens.Customization.Helpers;
using AgyTui.UI.Screens.Customization.Navigators;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.UI.Screens.Customization;

public class CustomizationScreenTests
{
    [Fact]
    public void AccountScreen_CanBeInstantiated()
    {
        var store = Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>();
        var quota = Bootstrapper.ServiceProvider.GetRequiredService<IAgyQuotaEngine>();
        var screen = new AccountScreen(store, quota);
        Assert.NotNull(screen);
        int count = screen.GetItemCount("");
        Assert.True(count >= 0);
    }

    [Fact]
    public void ThemeScreen_CanBeInstantiated()
    {
        var screen = new ThemeScreen();
        Assert.NotNull(screen);
        int count = screen.GetItemCount("");
        Assert.True(count >= 0);
    }

    [Fact]
    public void TopicScreen_CanBeInstantiated()
    {
        var screen = new TopicScreen();
        Assert.NotNull(screen);
        int count = screen.GetItemCount("");
        Assert.True(count >= 0);
    }

    [Fact]
    public void SubPageNavigators_StaticTypes_Exist()
    {
        Assert.NotNull(typeof(SubPageAccountNavigator));
        Assert.NotNull(typeof(SubPageThemeNavigator));
        Assert.NotNull(typeof(SubPageTopicNavigator));
    }

    [Fact]
    public void AccountManagerScreen_HandleInput_HandlesEscape()
    {
        var screen = new AccountManagerScreen();
        var state = new ScreenState("", 0);

        var resEsc = screen.HandleInput(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false), state);
        var resQ = screen.HandleInput(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false), state);

        Assert.Equal(NavigationAction.Exit, resEsc.Action);
        Assert.Equal(NavigationAction.Exit, resQ.Action);
    }
}
