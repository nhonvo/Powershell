namespace AgyTui.Infrastructure.Persistence.Interfaces;

using AgyTui.Infrastructure.Integrations.AgyClient;

public interface IAgyAccountRepository
{
    string GetActiveAccount();
    void SetActiveAccount(string accountName);
    AccountMetadata GetAccountMetadata(string accountName);
    void SaveAccountMetadata(string accountName, AccountMetadata metadata);
    string[] GetAccounts();
    void AddAccount(string accountName, string? email = null);
    void DeleteAccount(string accountName);
}
