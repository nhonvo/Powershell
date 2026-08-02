using AgyTui.Domain.AccountContext;

namespace AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public interface IAgyAccountStore
{
    string AgySourceHome { get; }
    string AgyAccountPrefix { get; }
    string GetAccountDirectory(string accountName);
    string? GetAccountEmail(string accountName);
    string GetShortCredentialSignature(string accountName);
    AccountMetadata GetAccountMetadata(string accountName);
    void UpdateAccountMetadata(string accountName);
    void SetAccountQuotaExceeded(string accountName, bool exceeded);
    bool IsNoAutoCommitEnabled();
    bool ToggleNoAutoCommit();

    AccountAggregate GetAccountAggregate(string accountName);
    void SaveAccountAggregate(AccountAggregate aggregate);

    string GetActiveAccount();
    void SetActiveAccount(string accountName);
    void SetActiveAccount(string accountName, bool temporary);
    string[] GetAccounts();
    void AddAccount(string accountName);
    void DeleteAccount(string accountName);
    void LogoutAccount(string accountName);
    void AuthenticateAccount(string accountName);
    void PurgeAllNonDefaultAccounts();
    bool IsAutoSwitchEnabled();
    void ToggleAutoSwitch();
    string? FindAutoSwitchCandidate();
    void AutoSwitchOnQuotaExceeded();
}
