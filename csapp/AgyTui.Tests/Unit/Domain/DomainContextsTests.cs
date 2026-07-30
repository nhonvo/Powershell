using AgyTui.Core.Models;
using AgyTui.Core.Registries;
using AgyTui.Domain.AccountContext;
using AgyTui.Domain.AiAgentContext;
using AgyTui.Domain.LearnContext;
using AgyTui.Domain.WorkspaceContext;
using Xunit;

namespace AgyTui.Tests.Unit.Domain;

public class DomainContextsTests
{
    [Fact]
    public void AccountAggregate_UpdatesState_AndConvertsMetadata()
    {
        var acc = new AccountAggregate("dev_acc", "dev@example.com");
        Assert.Equal("dev_acc", acc.AccountName);
        Assert.Equal("dev@example.com", acc.Email);
        Assert.False(acc.IsActive);

        acc.MarkActive();
        Assert.True(acc.IsActive);

        acc.SetQuotaExceeded(true);
        Assert.Equal("Exceeded", acc.QuotaStatus);

        acc.RecordUsage("2026-07-30T12:00:00Z");
        Assert.Equal(1, acc.UsageCount);

        var meta = acc.ToMetadata();
        Assert.Equal("Exceeded", meta.QuotaStatus);
        Assert.Equal(1, meta.UsageCount);

        var restored = AccountAggregate.FromMetadata("dev_acc", meta, "dev@example.com", true);
        Assert.True(restored.IsActive);
        Assert.Equal("Exceeded", restored.QuotaStatus);
        Assert.Equal(1, restored.UsageCount);
    }

    [Fact]
    public void WorkspaceAggregate_NormalizesPath_AndConvertsWorkspaceEntry()
    {
        var ws = new WorkspaceAggregate("Powershell", AppContext.BaseDirectory, "nhonvo/Powershell", true, "main", "ps", new[] { "tag1" });
        Assert.Equal("Powershell", ws.Name);
        Assert.True(ws.WorkspacePath.Exists);
        Assert.Equal("main", ws.GitBranch);

        var entry = ws.ToEntry();
        Assert.Equal("Powershell", entry.Name);
        Assert.Equal("ps", entry.Alias);

        var restored = WorkspaceAggregate.FromEntry(entry, true, "main");
        Assert.True(restored.IsActive);
        Assert.Equal("main", restored.GitBranch);
    }

    [Fact]
    public void AgentInvocationLog_Initializes_Defaults()
    {
        var log = new AgentInvocationLog("claude", 1200, true, "default");
        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.Equal("claude", log.Alias);
        Assert.True(log.Success);
        Assert.Equal(ProviderMode.Auto, log.Mode);
    }

    [Fact]
    public void FlashcardDeck_UpdatesStats_Correctly()
    {
        var deck = new FlashcardDeck("C#", 10, 2.5);
        Assert.Equal("C#", deck.Topic);
        Assert.Equal(10, deck.CardsCount);

        deck.UpdateStats(15, 2.8);
        Assert.Equal(15, deck.CardsCount);
        Assert.Equal(2.8, deck.AverageEaseFactor);
    }
}
