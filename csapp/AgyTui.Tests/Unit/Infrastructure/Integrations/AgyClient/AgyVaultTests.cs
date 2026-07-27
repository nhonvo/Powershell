namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyVaultTests
{
    [Fact]
    public void Vault_CanProtectAndUnprotectString()
    {
        var vault = new AgyVault();
        var original = "test_secret_payload_123";
        var encrypted = vault.Protect(original);
        var decrypted = vault.Unprotect(encrypted);

        Assert.Equal(original, decrypted);
    }
}
