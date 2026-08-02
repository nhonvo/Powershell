namespace AgyTui.Domain.AccountContext;

public sealed record AccountCredentials(
    string AccountName,
    string? KeyringToken = null,
    string? GoogleAccountsJson = null,
    string? OAuthCredsJson = null,
    string? StateJson = null,
    string? Email = null
);
