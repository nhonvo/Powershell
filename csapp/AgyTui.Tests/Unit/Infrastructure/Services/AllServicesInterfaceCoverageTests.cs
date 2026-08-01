using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

public class AllServicesInterfaceCoverageTests
{
    [Fact]
    public void AppPathManager_GetAccountDirectory_ReturnsPath()
    {
        IAppPathManager pathManager = new AppPathManager();
        var dir = pathManager.GetAccountDirectory("default");
        Assert.NotNull(dir);
        Assert.NotEmpty(dir);

        pathManager.InvalidateAccountCache("default");
        pathManager.ClearAllCache();
    }
}
