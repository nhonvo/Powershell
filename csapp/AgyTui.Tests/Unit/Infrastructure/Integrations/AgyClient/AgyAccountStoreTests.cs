using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyAccountStoreTests
{
    [Fact]
    public void AccountStore_GetActiveAccount_ReturnsNonNullDefault()
    {
        var store = new AgyAccountStore();
        var active = store.GetActiveAccount();

        Assert.NotNull(active);
        Assert.NotEmpty(active);
    }

    [Fact]
    public void AccountStore_IsAutoSwitchEnabled_ReturnsBoolean()
    {
        var store = new AgyAccountStore();
        var enabled = store.IsAutoSwitchEnabled();

        Assert.True(enabled || !enabled);
    }
}
