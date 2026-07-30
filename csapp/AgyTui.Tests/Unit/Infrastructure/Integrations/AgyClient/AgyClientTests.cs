using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyClientTests
{
    [Fact]
    public void Bootstrapper_Resolves_IAgyAccountStore_Instance()
    {
        var provider = Bootstrapper.BuildServiceProvider();
        var store = provider.GetService<IAgyAccountStore>();

        Assert.NotNull(store);
        Assert.IsType<AgyAccountStore>(store);
    }

    [Fact]
    public void AccountStore_RetrievesActiveAccount()
    {
        var store = new AgyAccountStore();
        var activeAcc = store.GetActiveAccount();

        Assert.NotNull(activeAcc);
        Assert.NotEmpty(activeAcc);
    }
}
