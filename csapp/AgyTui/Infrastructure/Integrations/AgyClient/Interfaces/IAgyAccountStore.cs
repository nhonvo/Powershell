namespace AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public interface IAgyAccountStore
{
    string GetActiveAccount();
    void SetActiveAccount(string accountName);
    void SetActiveAccount(string accountName, bool temporary);
    string[] GetAccounts();
    void AddAccount(string accountName);
    void DeleteAccount(string accountName);
    void LogoutAccount(string accountName);
    bool IsAutoSwitchEnabled();
    void ToggleAutoSwitch();
    string? FindAutoSwitchCandidate();
    void AutoSwitchOnQuotaExceeded();
}
