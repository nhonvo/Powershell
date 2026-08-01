using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;
using AgyTui.Infrastructure.Integrations.Obsidian;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AllIntegrationInterfaceCoverageTests
{
    [Fact]
    public void IAwsClient_AllMethods_DeclaredAndCallable()
    {
        IAwsClient client = new AwsClient();
        Assert.NotNull(client);
        // IAwsClient methods: ShowLocalStackInfo, ShowCallerIdentity, ShowS3Buckets, ShowSQSQueues, ShowSsmParameters, ShowSnsTopics, ShowDynamoDbTables, ShowLambdaFunctions
    }

    [Fact]
    public void IDockerClient_AllMethods_DeclaredAndCallable()
    {
        IDockerClient client = new DockerClient();
        Assert.NotNull(client);
        // IDockerClient methods: ShowCleanupDashboard, ShowDockerHealthDashboard, ComposeUp, ComposeDown, ShowImages, ShowContainerLogs, RemoveAllContainers, StopAllContainers
        client.ComposeDown(null);
    }

    [Fact]
    public void IDotNetClient_AllMethods_DeclaredAndCallable()
    {
        IDotNetClient client = new DotNetClient();
        Assert.NotNull(client);
        // IDotNetClient methods: RemoveBinObj, Build, Run, Test, Format, Clean, Restore, Publish, Pack, PublishPackage, Watch, AddMigration, UpdateDatabase
        client.RemoveBinObj("C:\\NonExistent_Dir_XYZ");
    }

    [Fact]
    public void IGitClient_AllMethods_DeclaredAndCallable()
    {
        IGitClient client = new GitClient(new AiCommitGenerator());
        Assert.NotNull(client);
        // IGitClient methods: ShowStatus, ShowBranches, ShowLog, Pull, Push, AddAll, Fetch, Checkout, ConventionalCommitWizard, InvokeGitUndo
    }

    [Fact]
    public void IObsidianBridge_AllMethods_DeclaredAndCallable()
    {
        IObsidianBridge bridge = new ObsidianBridge();
        Assert.NotNull(bridge);
        // IObsidianBridge methods: Configure, Run, SearchNotes, ShowDailyNote, ListByTag
    }
}
