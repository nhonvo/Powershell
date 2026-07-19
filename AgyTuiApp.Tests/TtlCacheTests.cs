using System;
using Xunit;
using AgyTui;

namespace AgyTuiApp.Tests;

public class TtlCacheTests
{
    [Fact]
    public void CacheComputeOnceBeforeTtl()
    {
        var cache = new TtlCache<string, int>(TimeSpan.FromSeconds(10));
        int count = 0;
        int Factory() { count++; return 42; }

        var v1 = cache.GetOrCompute("key", Factory);
        var v2 = cache.GetOrCompute("key", Factory);

        Assert.Equal(42, v1);
        Assert.Equal(42, v2);
        Assert.Equal(1, count);
    }

    [Fact]
    public void InvalidateRemovesKey()
    {
        var cache = new TtlCache<string, int>(TimeSpan.FromSeconds(10));
        int count = 0;
        int Factory() { count++; return 100; }

        cache.GetOrCompute("key", Factory);
        cache.Invalidate("key");
        cache.GetOrCompute("key", Factory);

        Assert.Equal(2, count);
    }
}
