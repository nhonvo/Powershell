using AgyTui.Domain.AccountContext;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Persistence.Interfaces;

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

    [Fact]
    public void SaveAndGetAccountCredentials_PersistsInRepository()
    {
        var repo = new Mocks.InMemoryAgyAccountRepository();
        var creds = new AccountCredentials("test_user", "enc_token_123", "{\"acc\":\"test\"}", "{\"oauth\":\"ok\"}", "{\"state\":\"ok\"}", "test@example.com");
        repo.SaveAccountCredentials(creds);

        var retrieved = repo.GetAccountCredentials("test_user");
        Assert.NotNull(retrieved);
        Assert.Equal("enc_token_123", retrieved.KeyringToken);
        Assert.Equal("test@example.com", retrieved.Email);
    }
}
