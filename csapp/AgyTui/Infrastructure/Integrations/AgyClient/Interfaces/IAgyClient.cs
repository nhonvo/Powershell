namespace AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public interface IAgyClient
{
    string AgySourceHome { get; }
    string GetActiveAccount();
    void SetActiveAccount(string accountName, bool temporary = false);
    string[] GetAccounts();
    string? GetAccountEmail(string accountName);
    AccountMetadata GetAccountMetadata(string accountName);
    AccountStats GetAccountStats(string accountName);
    QuotaMetrics CalculateRollingQuotas(string accountName);
    bool CheckNetworkStatus();
    void AddAccount(string accountName);
    void DeleteAccount(string accountName);
    void LogoutAccount(string accountName);
    bool IsAutoSwitchEnabled();
    void ToggleAutoSwitch();
    bool IsNoAutoCommitEnabled();
    bool ToggleNoAutoCommit();
    void SyncActiveAccountWithKeyring(bool silent);
    void AutoSwitchOnQuotaExceeded();
}
