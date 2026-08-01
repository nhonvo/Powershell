using System.Diagnostics;
using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

public class PathResolutionBenchmarkTests
{
    [Fact]
    public void PathResolution_CachedCalls_ExecuteUnder1Millisecond()
    {
        var manager = new AppPathManager();
        
        // Warmup / Prime cache
        _ = manager.GetAccountDirectory("default");
        _ = manager.GetAccountDirectory("account1");

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = manager.GetAccountDirectory("account1");
        }
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500, $"1000 cached path resolutions took {sw.ElapsedMilliseconds}ms (expected < 500ms)");
    }
}


