using AgyTui.UI.Core.State;
using Xunit;

namespace AgyTui.Tests.Unit.UI.Core;

public class UiStateStoreTests
{
    [Fact]
    public void Update_ModifiesCurrentStateAndFiresEvent()
    {
        IUiStateStore store = new UiStateStore();
        bool eventFired = false;

        store.OnStateChanged += state =>
        {
            eventFired = true;
            Assert.Equal("test_account", state.ActiveAccount);
        };

        store.Update(s => s with { ActiveAccount = "test_account" });

        Assert.True(eventFired);
        Assert.Equal("test_account", store.Current.ActiveAccount);
    }

    [Fact]
    public void Current_ReturnsInitialDefaultState()
    {
        IUiStateStore store = new UiStateStore();
        Assert.NotNull(store.Current);
        Assert.Equal("default", store.Current.ActiveAccount);
    }

    [Fact]
    public void Update_ZeroFilterBuffer_HandlesEmptyString()
    {
        // Zero case: empty filter buffer
        IUiStateStore store = new UiStateStore();
        store.Update(s => s with { FilterBuffer = "" });

        Assert.Equal("", store.Current.FilterBuffer);
        Assert.Equal(0, store.Current.SelectedIndex);
    }

    [Fact]
    public void Update_NegativeSelectedIndex_MaintainsValue()
    {
        // Boundary case: negative index or zero reset
        IUiStateStore store = new UiStateStore();
        store.Update(s => s with { SelectedIndex = -1 });

        Assert.Equal(-1, store.Current.SelectedIndex);
    }

    [Fact]
    public void Update_NullEventSubscribers_DoesNotThrow()
    {
        // Failure/Edge case: zero subscribers on event
        IUiStateStore store = new UiStateStore();
        store.Update(s => s with { FilterBuffer = "query" });

        Assert.Equal("query", store.Current.FilterBuffer);
    }
}
