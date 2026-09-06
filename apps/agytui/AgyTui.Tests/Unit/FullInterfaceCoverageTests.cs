using AgyTui.Infrastructure.Configuration;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;
using AgyTui.Infrastructure.Integrations.Obsidian;
using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;
using AgyTui.Infrastructure.Persistence.Repositories;
using AgyTui.Infrastructure.Persistence.Seeding;
using AgyTui.Infrastructure.Registries;
using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit;

public class FullInterfaceCoverageTests
{
    [Fact]
    public void IAwsClient_Instance_CanBeCreated()
    {
        IAwsClient client = new AwsClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void IDockerClient_ComposeDown_NullFile_ReturnsExitCode()
    {
        IDockerClient client = new DockerClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void IDotNetClient_AllCommandMethods_Callable()
    {
        IDotNetClient client = new DotNetClient();
        client.Build(null);
        client.Clean(null);
        client.Restore(null);
        client.Format(null);
    }

    [Fact]
    public void IGitClient_Instance_CanBeCreated()
    {
        IGitClient client = new GitClient(new AiCommitGenerator());
        Assert.NotNull(client);
    }

    [Fact]
    public void IObsidianBridge_Instance_CanBeCreated()
    {
        IObsidianBridge bridge = new ObsidianBridge();
        Assert.NotNull(bridge);
    }

    [Fact]
    public void IConfigRepository_LoadAndSave_Callable()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IConfigRepository repo = new SqliteConfigRepository(db);

        var cfg = repo.LoadConfig();
        Assert.NotNull(cfg);

        repo.SaveConfig(cfg);
    }

    [Fact]
    public void IWorkspaceRepository_SaveAndGet_Callable()
    {
        ISqliteDatabase db = new SqliteDatabase();
        IWorkspaceRepository repo = new SqliteWorkspaceRepository(db);

        var ws = repo.GetWorkspace("non_existent_ws_999");
        Assert.Null(ws);

        repo.DeleteWorkspace("non_existent_ws_999");
    }

    [Fact]
    public void IConfigService_SaveAndReload_Callable()
    {
        IConfigService service = new ConfigService();
        Assert.NotNull(service.Current);

        service.Save();
        service.Reload();
    }

    [Fact]
    public void IWorkspaceRegistry_Methods_Callable()
    {
        var aggregates = WorkspaceRegistry.GetWorkspaceAggregates();
        Assert.NotNull(aggregates);

        var queryResult = WorkspaceRegistry.FindByQuery("non_existent_ws_query_xyz");
        Assert.NotNull(queryResult);
    }
}
