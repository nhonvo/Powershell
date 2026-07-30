using AgyTui.Domain.AccountContext;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Tests.Mocks;

public class InMemoryAgyAccountRepository : IAgyAccountRepository
{
    private string _activeAccount = "default";
    private readonly Dictionary<string, AccountMetadata> _metadata = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryAgyAccountRepository()
    {
        _metadata["default"] = new AccountMetadata();
    }

    public string GetActiveAccount() => _activeAccount;

    public void SetActiveAccount(string accountName) => _activeAccount = accountName;

    public AccountMetadata GetAccountMetadata(string accountName)
    {
        if (_metadata.TryGetValue(accountName, out var meta)) return meta;
        return new AccountMetadata();
    }

    public void SaveAccountMetadata(string accountName, AccountMetadata metadata)
    {
        _metadata[accountName] = metadata;
    }

    public string[] GetAccounts() => _metadata.Keys.ToArray();

    public void AddAccount(string accountName, string? email = null)
    {
        if (!_metadata.ContainsKey(accountName))
        {
            _metadata[accountName] = new AccountMetadata();
        }
    }

    public void DeleteAccount(string accountName)
    {
        _metadata.Remove(accountName);
        if (_activeAccount.Equals(accountName, StringComparison.OrdinalIgnoreCase))
        {
            _activeAccount = "default";
        }
    }
}
