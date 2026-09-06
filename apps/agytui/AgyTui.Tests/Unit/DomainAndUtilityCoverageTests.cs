using AgyTui.Domain.AccountContext;
using AgyTui.Domain.WorkspaceContext;
using AgyTui.Infrastructure.Common;
using Xunit;

namespace AgyTui.Tests.Unit;

public class DomainAndUtilityCoverageTests
{
    [Fact]
    public void AccountAggregate_MarkInactive_UpdatesIsActiveToFalse()
    {
        var agg = new AccountAggregate("test_acc", "test@example.com", true);
        agg.MarkInactive();
        Assert.False(agg.IsActive);

        agg.MarkActive();
        Assert.True(agg.IsActive);

        agg.SetQuotaExceeded(true);
        Assert.Equal("Exceeded", agg.QuotaStatus);

        agg.RecordUsage("2026-08-01T12:00:00Z");
        Assert.Equal(1, agg.UsageCount);

        var meta = agg.ToMetadata();
        Assert.NotNull(meta);

        var fromMeta = AccountAggregate.FromMetadata("test_acc", meta, "test@example.com", true);
        Assert.NotNull(fromMeta);
    }

    [Fact]
    public void WorkspaceAggregate_ActivateAndDeactivate_UpdatesState()
    {
        var ws = new WorkspaceAggregate("WS1", "C:\\WS1", "default");
        ws.Activate();
        Assert.True(ws.IsActive);

        ws.Deactivate();
        Assert.False(ws.IsActive);

        ws.SetBranch("feature/test");
        Assert.Equal("feature/test", ws.GitBranch);

        var entry = ws.ToEntry();
        Assert.NotNull(entry);

        var fromEntry = WorkspaceAggregate.FromEntry(entry, true, "main");
        Assert.NotNull(fromEntry);
    }

    [Fact]
    public void TtlCache_InvalidateAll_ClearsCache()
    {
        var cache = new TtlCache<string, string>(TimeSpan.FromMinutes(5));
        cache.Set("k1", "v1");
        Assert.Equal("v1", cache.Get("k1"));

        cache.InvalidateAll();
        Assert.Null(cache.Get("k1"));
    }

    [Fact]
    public void ProcessRunner_FindOnPath_NonExistentBinary_ReturnsNullOrEmpty()
    {
        var path = ProcessRunner.Instance.FindOnPath("non_existent_binary_9999");
        Assert.True(string.IsNullOrEmpty(path));
    }
}
