using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

public class AppPathManagerTests
{
    [Fact]
    public void GetAccountDirectory_ReturnsDefault_ForDefaultAccount()
    {
        var manager = new AppPathManager();
        var defaultDir = manager.GetAccountDirectory("default");

        Assert.NotNull(defaultDir);
        Assert.Equal(manager.GeminiHome, defaultDir);
    }

    [Fact]
    public void GetAccountDirectory_CachesNonDefaultAccounts()
    {
        var manager = new AppPathManager();
        var dir1 = manager.GetAccountDirectory("acc1");
        var dir2 = manager.GetAccountDirectory("acc1");

        Assert.Same(dir1, dir2);
        Assert.EndsWith("acc1", dir1);
    }

    [Fact]
    public void InvalidateCache_ClearsCachedPaths()
    {
        var manager = new AppPathManager();
        var dir1 = manager.GetAccountDirectory("acc1");
        
        manager.InvalidateCache();
        var dir2 = manager.GetAccountDirectory("acc1");

        Assert.Equal(dir1, dir2);
        Assert.NotSame(dir1, dir2);
    }
}


