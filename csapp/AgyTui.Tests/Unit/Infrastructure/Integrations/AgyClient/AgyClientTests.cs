using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyClientTests
{
    [Fact]
    public void Bootstrapper_Resolves_IAgyClient_Instance()
    {
        var provider = Bootstrapper.BuildServiceProvider();
        var client = provider.GetService<IAgyClient>();

        Assert.NotNull(client);
        Assert.IsType<AgyTui.Infrastructure.Integrations.AgyClient.AgyClient>(client);
    }

    [Fact]
    public void AgyClient_DelegatesToAccountRepository_ForActiveAccount()
    {
        var repo = new AgyAccountStore();
        var client = new AgyTui.Infrastructure.Integrations.AgyClient.AgyClient(repo);

        var activeAcc = client.GetActiveAccount();
        Assert.NotNull(activeAcc);
        Assert.NotEmpty(activeAcc);
    }
}
