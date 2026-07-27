namespace AgyTui.Infrastructure.Integrations.AgyClient;

using AgyTui.Core.Interfaces;

public class AgyClient : IAgyClient
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAgyTokenManager _tokenManager;
    private readonly IAgyQuotaCalculator _quotaCalculator;
    private readonly IAgyAccountStatsProvider _statsProvider;
    private readonly IAgyAccountSwitcher _accountSwitcher;

    public AgyClient(
        IAccountRepository accountRepository,
        IAgyTokenManager tokenManager,
        IAgyQuotaCalculator quotaCalculator,
        IAgyAccountStatsProvider statsProvider,
        IAgyAccountSwitcher accountSwitcher)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _quotaCalculator = quotaCalculator ?? throw new ArgumentNullException(nameof(quotaCalculator));
        _statsProvider = statsProvider ?? throw new ArgumentNullException(nameof(statsProvider));
        _accountSwitcher = accountSwitcher ?? throw new ArgumentNullException(nameof(accountSwitcher));
    }

    public AgyClient(IAccountRepository accountRepository)
        : this(
            accountRepository,
            new AgyTokenManager(),
            new AgyQuotaCalculator(),
            new AgyAccountStatsProvider(),
            new AgyAccountSwitcher())
    {
    }

    public string AgySourceHome => AgyAccountCore.AgySourceHome;
    public string GetActiveAccount() => _accountRepository.GetActiveAccount();
    public void SetActiveAccount(string accountName, bool temporary = false) => _accountSwitcher.SetActiveAccount(accountName, temporary);
    public string[] GetAccounts() => _accountRepository.GetAccounts();
    public string? GetAccountEmail(string accountName) => AgyAccountCore.GetAccountEmail(accountName);
    public AccountMetadata GetAccountMetadata(string accountName) => AgyAccountCore.GetAccountMetadata(accountName);
    public AccountStats GetAccountStats(string accountName) => _statsProvider.GetAccountStats(accountName);
    public QuotaMetrics CalculateRollingQuotas(string accountName) => _quotaCalculator.CalculateRollingQuotas(accountName);
    public bool CheckNetworkStatus() => AgyAccountCore.CheckNetworkStatus();
    public void AddAccount(string accountName) => _accountSwitcher.AddAccount(accountName);
    public void DeleteAccount(string accountName) => _accountSwitcher.DeleteAccount(accountName);
    public void LogoutAccount(string accountName) => _accountSwitcher.LogoutAccount(accountName);
    public bool IsAutoSwitchEnabled() => _accountSwitcher.IsAutoSwitchEnabled();
    public void ToggleAutoSwitch() => _accountSwitcher.ToggleAutoSwitch();
    public bool IsNoAutoCommitEnabled() => AgyAccountCore.IsNoAutoCommitEnabled();
    public bool ToggleNoAutoCommit() => AgyAccountCore.ToggleNoAutoCommit();
    public void SyncActiveAccountWithKeyring(bool silent) => _tokenManager.SyncActiveAccountWithKeyring(silent);
    public void AutoSwitchOnQuotaExceeded() => _accountSwitcher.AutoSwitchOnQuotaExceeded();
}
