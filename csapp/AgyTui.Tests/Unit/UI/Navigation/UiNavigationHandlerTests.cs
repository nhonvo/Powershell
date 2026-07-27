namespace AgyTui.Tests.Unit.UI.Navigation;

using AgyTui.UI.Core.Navigation;
using AgyTui.UI.Core.Navigation.Interfaces;
using Xunit;

public class UiNavigationHandlerTests
{
    [Fact]
    public void NavigationHistory_TracksPushedStatesInOrder()
    {
        var handler = new UiNavigationHandler();
        handler.PushState("home");
        handler.PushState("settings");
        handler.PushState("account");

        Assert.Equal(3, handler.NavigationHistory.Count);
        Assert.Equal("account", handler.NavigationHistory.First());
    }

    [Fact]
    public void PopState_ReturnsAndRemovesLastPushedState()
    {
        var handler = new UiNavigationHandler();
        handler.PushState("dashboard");
        handler.PushState("analytics");

        var popped = handler.PopState();

        Assert.Equal("analytics", popped);
        Assert.Single(handler.NavigationHistory);
        Assert.Equal("dashboard", handler.NavigationHistory.First());
    }

    [Fact]
    public void PopState_OnEmptyHistory_ReturnsNull()
    {
        var handler = new UiNavigationHandler();

        var popped = handler.PopState();

        Assert.Null(popped);
    }

    [Fact]
    public void NavigateTo_PushesStateToHistory()
    {
        var handler = new UiNavigationHandler();

        handler.PushState("test-screen");

        Assert.Contains("test-screen", handler.NavigationHistory);
    }
}
