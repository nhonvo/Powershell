using AgyTui.Infrastructure.Integrations.AgyClient;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations.AgyClient;

public class AgyTokenManagerTests
{
    [Fact]
    public void TokenManager_CanBeInstantiated()
    {
        var manager = new AgyTokenManager();
        Assert.NotNull(manager);
    }
}
