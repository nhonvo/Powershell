using AgyTui.Domain.AccountContext;
using AgyTui.Domain.AiAgentContext;
using AgyTui.Domain.WorkspaceContext;
using Xunit;

namespace AgyTui.Tests.Unit.Domain;

public class DomainContextsTests
{
    [Fact]
    public void AccountAggregate_UpdatesState_Correctly()
    {
        var acc = new AccountAggregate("dev_acc", "dev@example.com");
        Assert.Equal("dev_acc", acc.AccountName);
        Assert.Equal("dev@example.com", acc.Email);
        Assert.False(acc.IsActive);

        acc.MarkActive();
        Assert.True(acc.IsActive);

        acc.SetQuotaExceeded(true);
        Assert.Equal("Exceeded", acc.QuotaStatus);
    }

    [Fact]
    public void WorkspaceAggregate_NormalizesPath_AndTracksBranch()
    {
        var ws = new WorkspaceAggregate("Powershell", AppContext.BaseDirectory, "nhonvo/Powershell", true, "main");
        Assert.Equal("Powershell", ws.Name);
        Assert.True(ws.WorkspacePath.Exists);
        Assert.Equal("main", ws.GitBranch);
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
}
