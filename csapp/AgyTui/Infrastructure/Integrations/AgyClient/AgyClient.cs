namespace AgyTui.Infrastructure.Integrations.AgyClient;

public class AgyClient : IAgyClient
{
    private readonly IAgyAccountStore _accountStore;
    private readonly IAgyQuotaEngine _quotaEngine;
    private readonly IAgyVault _vault;

    public AgyClient(
        IAgyAccountStore? accountStore = null,
        IAgyQuotaEngine? quotaEngine = null,
        IAgyVault? vault = null)
    {
        _accountStore = accountStore ?? new AgyAccountStore();
        _quotaEngine = quotaEngine ?? new AgyQuotaEngine(_accountStore);
        _vault = vault ?? new AgyVault(_accountStore);
    }

    public string AgySourceHome => _accountStore.AgySourceHome;
    public string GetActiveAccount() => _accountStore.GetActiveAccount();
    public void SetActiveAccount(string accountName, bool temporary = false) => _accountStore.SetActiveAccount(accountName, temporary);
    public string[] GetAccounts() => _accountStore.GetAccounts();
    public string? GetAccountEmail(string accountName) => _accountStore.GetAccountEmail(accountName);
    public AccountMetadata GetAccountMetadata(string accountName) => _accountStore.GetAccountMetadata(accountName);
    public AccountStats GetAccountStats(string accountName) => _quotaEngine.GetAccountStats(accountName);
    public QuotaMetrics CalculateRollingQuotas(string accountName) => _quotaEngine.CalculateRollingQuotas(accountName);
    public bool CheckNetworkStatus()
    {
        try
        {
            var res = HttpClientProvider.Client.GetAsync("https://www.google.com").GetAwaiter().GetResult();
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    public void AddAccount(string accountName) => _accountStore.AddAccount(accountName);
    public void DeleteAccount(string accountName) => _accountStore.DeleteAccount(accountName);
    public void LogoutAccount(string accountName) => _accountStore.LogoutAccount(accountName);
    public bool IsAutoSwitchEnabled() => _accountStore.IsAutoSwitchEnabled();
    public void ToggleAutoSwitch() => _accountStore.ToggleAutoSwitch();
    public bool IsNoAutoCommitEnabled() => _accountStore.IsNoAutoCommitEnabled();
    public bool ToggleNoAutoCommit() => _accountStore.ToggleNoAutoCommit();
    public void SyncActiveAccountWithKeyring(bool silent) => _vault.SyncActiveAccountWithKeyring(silent);
    public void AutoSwitchOnQuotaExceeded() => _accountStore.AutoSwitchOnQuotaExceeded();
}
