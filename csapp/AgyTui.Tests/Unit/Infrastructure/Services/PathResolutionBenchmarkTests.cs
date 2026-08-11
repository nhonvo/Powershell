using System;
using System.Diagnostics;
using System.IO;
using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

public class PathResolutionBenchmarkTests : IDisposable
{
    private readonly string _benchAccountName = "bench_acc_" + Guid.NewGuid().ToString("N");

    [Fact]
    public void PathResolution_CachedCalls_ExecuteUnder1Millisecond()
    {
        var manager = new AppPathManager();
        
        // Warmup / Prime cache
        _ = manager.GetAccountDirectory("default");
        _ = manager.GetAccountDirectory(_benchAccountName);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = manager.GetAccountDirectory(_benchAccountName);
        }
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500, $"1000 cached path resolutions took {sw.ElapsedMilliseconds}ms (expected < 500ms)");
    }

    public void Dispose()
    {
        try
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            if (!string.IsNullOrEmpty(userProfile))
            {
                var testDir = Path.Combine(userProfile, $".gemini_{_benchAccountName}");
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, recursive: true);
                }
            }
        }
        catch { }
    }
}
