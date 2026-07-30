namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class AccountServiceTests
{
    private readonly IAgyAccountStore _store = new AgyAccountStore();

    [Fact]
    public void GetActiveAccount_ReturnsNonNullDefaultFallback()
    {
        var active = _store.GetActiveAccount();
        Assert.NotNull(active);
        Assert.NotEmpty(active);
    }

    [Fact]
    public void GetAccounts_ReturnsAccountsList()
    {
        var accounts = _store.GetAccounts();
        Assert.NotNull(accounts);
    }
}
