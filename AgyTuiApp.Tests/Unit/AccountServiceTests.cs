using Xunit;
using AgyTui;

namespace AgyTui.Tests.Unit;

public class AccountServiceTests
{
    [Fact]
    public void GetActiveAccount_ReturnsNonNullDefaultFallback()
    {
        var active = AccountRepository.GetActiveAccount();
        Assert.NotNull(active);
        Assert.NotEmpty(active);
    }

    [Fact]
    public void GetAccounts_ReturnsAccountsList()
    {
        var accounts = AccountRepository.GetAccounts();
        Assert.NotNull(accounts);
    }
}
