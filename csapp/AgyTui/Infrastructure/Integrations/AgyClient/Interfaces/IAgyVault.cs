namespace AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public interface IAgyVault
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
    void BackupActiveToken(string accountName);
    void RestoreActiveToken(string accountName);
    void SyncActiveAccountWithKeyring(bool silent);
    void SetSecret(string key, string value);
    string? GetSecret(string key);
    void ListSecrets();
    void RemoveSecret(string key);
}
