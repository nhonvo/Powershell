namespace AgyTui.Infrastructure.Integrations.AgyClient;

using AgyTui.Core.Interfaces;
using AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyClient : IAgyClient
{
    private readonly IAccountRepository _accountRepository;

    public AgyClient(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    public string AgySourceHome => AgyAccountCore.AgySourceHome;
    public string GetActiveAccount() => _accountRepository.GetActiveAccount();
    public void SetActiveAccount(string accountName, bool temporary = false) => AgyAccountCore.SetActiveAccount(accountName, temporary);
    public string[] GetAccounts() => _accountRepository.GetAccounts();
    public string? GetAccountEmail(string accountName) => AgyAccountCore.GetAccountEmail(accountName);
    public AccountMetadata GetAccountMetadata(string accountName) => AgyAccountCore.GetAccountMetadata(accountName);
    public AccountStats GetAccountStats(string accountName) => AgyAccountCore.GetAccountStats(accountName);
    public QuotaMetrics CalculateRollingQuotas(string accountName) => AgyAccountCore.CalculateRollingQuotas(accountName);
    public bool CheckNetworkStatus() => AgyAccountCore.CheckNetworkStatus();
    public void AddAccount(string accountName) => AgyAccountCore.AddAccount(accountName);
    public void DeleteAccount(string accountName) => AgyAccountCore.DeleteAccount(accountName);
    public void LogoutAccount(string accountName) => AgyAccountCore.LogoutAccount(accountName);
    public bool IsAutoSwitchEnabled() => AgyAccountCore.IsAutoSwitchEnabled();
    public void ToggleAutoSwitch() => AgyAccountCore.ToggleAutoSwitch();
    public bool IsNoAutoCommitEnabled() => AgyAccountCore.IsNoAutoCommitEnabled();
    public bool ToggleNoAutoCommit() => AgyAccountCore.ToggleNoAutoCommit();
    public void SyncActiveAccountWithKeyring(bool silent) => AgyAccountCore.SyncActiveAccountWithKeyring(silent);
    public void AutoSwitchOnQuotaExceeded() => AgyAccountCore.AutoSwitchOnQuotaExceeded();
}
