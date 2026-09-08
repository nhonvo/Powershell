namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface IAgyAccountRepository
{
    string GetActiveAccount();
    void SetActiveAccount(string accountName);
    AccountMetadata GetAccountMetadata(string accountName);
    void SaveAccountMetadata(string accountName, AccountMetadata metadata);
    AccountCredentials? GetAccountCredentials(string accountName);
    void SaveAccountCredentials(AccountCredentials credentials);
    string[] GetAccounts();
    void AddAccount(string accountName, string? email = null);
    void DeleteAccount(string accountName);
}
