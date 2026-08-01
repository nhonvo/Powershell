using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AgyClientFullTests
{
    [Fact]
    public void AgyVault_ProtectAndUnprotect_RoundtripsSuccessfully()
    {
        IAgyVault vault = new AgyVault();
        var plain = "secret_test_token_123";
        var protectedText = vault.Protect(plain);
        var unprotectedText = vault.Unprotect(protectedText);

        Assert.Equal(plain, unprotectedText);
    }

    [Fact]
    public void AgyVault_SetSecretAndGetSecret_RoundtripsSuccessfully()
    {
        IAgyVault vault = new AgyVault();
        vault.SetSecret("test_key_xyz", "test_val_xyz");
        var val = vault.GetSecret("test_key_xyz");

        Assert.Equal("test_val_xyz", val);

        vault.RemoveSecret("test_key_xyz");
        var valAfterRemove = vault.GetSecret("test_key_xyz");
        Assert.Null(valAfterRemove);
    }
}
