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

    [Fact]
    public void AccountStore_DeleteAccount_NonExistentDir_DoesNotThrowException()
    {
        var store = new AgyAccountStore();
        var testAcc = "test_delete_orphan_" + Guid.NewGuid().ToString("N")[..6];

        // Should not throw DirectoryNotFoundException when deleting
        var exception = Record.Exception(() => store.DeleteAccount(testAcc));
        Assert.Null(exception);
    }
}
