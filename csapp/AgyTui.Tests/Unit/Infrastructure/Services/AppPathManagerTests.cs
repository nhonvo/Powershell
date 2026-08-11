using System;
using System.IO;
using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

public class AppPathManagerTests : IDisposable
{
    private readonly string _testAccountName = "test_acc_" + Guid.NewGuid().ToString("N");

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
        var dir1 = manager.GetAccountDirectory(_testAccountName);
        var dir2 = manager.GetAccountDirectory(_testAccountName);

        Assert.Same(dir1, dir2);
        Assert.EndsWith(_testAccountName, dir1);
    }

    [Fact]
    public void InvalidateCache_ClearsCachedPaths()
    {
        var manager = new AppPathManager();
        var dir1 = manager.GetAccountDirectory(_testAccountName);
        
        manager.InvalidateCache();
        var dir2 = manager.GetAccountDirectory(_testAccountName);

        Assert.Equal(dir1, dir2);
        Assert.NotSame(dir1, dir2);
    }

    public void Dispose()
    {
        try
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            if (!string.IsNullOrEmpty(userProfile))
            {
                var testDir = Path.Combine(userProfile, $".gemini_{_testAccountName}");
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
        }
        catch { }
    }
}
