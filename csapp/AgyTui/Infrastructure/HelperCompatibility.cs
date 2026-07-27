using Microsoft.Extensions.DependencyInjection;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Aws;
using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Integrations.DotNet;
using AgyTui.Infrastructure.Integrations.Git;

namespace AgyTui.Infrastructure;

public static class GitHelper
{
    private static IGitClient Git => Bootstrapper.ServiceProvider.GetRequiredService<IGitClient>();

    public static void ShowStatus() => Git.ShowStatus();
    public static void ShowLog() => Git.ShowLog();
    public static void ShowBranches() => Git.ShowBranches();
    public static void Checkout(string? branchName) => Git.Checkout(branchName);
    public static void AddAll() => Git.AddAll();
    public static void ConventionalCommitWizard() => Git.ConventionalCommitWizard();
    public static void InvokeGitUndo() => Git.InvokeGitUndo();
    public static void Fetch() => Git.Fetch();
    public static void Pull() => Git.Pull();
    public static void Push() => Git.Push();
}

public static class DotNetHelper
{
    private static IDotNetClient DotNet => Bootstrapper.ServiceProvider.GetRequiredService<IDotNetClient>();

    public static int Build(string[]? args = null) => DotNet.Build();
    public static int Run(string[]? args = null) => DotNet.Run();
    public static int Test(string[]? args = null) => DotNet.Test();
    public static int Format(string[]? args = null) => DotNet.Format();
    public static int Clean(string[]? args = null) => DotNet.Clean();
    public static int Restore(string[]? args = null) => DotNet.Restore();
    public static int Publish(string[]? args = null) => DotNet.Publish();
    public static int Pack(string[]? args = null) => DotNet.Pack();
    public static int PublishPackage(string[]? args = null) => DotNet.PublishPackage();
    public static int Watch(string[]? args = null) => DotNet.Watch();
    public static void CleanBinObj() => DotNet.RemoveBinObj(Directory.GetCurrentDirectory());
    public static int AddMigration(string migrationName, string? context = null) => DotNet.AddMigration(migrationName);
    public static int UpdateDatabase(string? context = null) => DotNet.UpdateDatabase();
}

public static class DockerHelper
{
    private static IDockerClient Docker => Bootstrapper.ServiceProvider.GetRequiredService<IDockerClient>();

    public static void ShowDockerHealthDashboard() => Docker.ShowDockerHealthDashboard();
    public static void ShowCleanupDashboard() => Docker.ShowCleanupDashboard();
    public static void RemoveAllContainers() => Docker.RemoveAllContainers();
    public static void StopAllContainers() => Docker.StopAllContainers();
    public static void ShowImages() => Docker.ShowImages();
    public static void ShowContainerLogs() => Docker.ShowContainerLogs();
    public static void ComposeUp() => Docker.ComposeUp();
    public static void ComposeDown() => Docker.ComposeDown();
}

public static class AwsHelper
{
    private static IAwsClient Aws => Bootstrapper.ServiceProvider.GetRequiredService<IAwsClient>();

    public static string LocalStackUrl => "http://localhost:4566";
    public static void ShowCallerIdentity() => Aws.ShowCallerIdentity();
    public static void ShowLocalStackInfo() => Aws.ShowLocalStackInfo();
    public static void ShowS3Buckets() => Aws.ShowS3Buckets();
    public static void ShowSQSQueues() => Aws.ShowSQSQueues();
    public static void ShowSsmParameters() => Aws.ShowSsmParameters();
    public static void ShowSnsTopics() => Aws.ShowSnsTopics();
    public static void ShowDynamoDbTables() => Aws.ShowDynamoDbTables();
    public static void ShowLambdaFunctions() => Aws.ShowLambdaFunctions();
}

public static class SystemHelper
{
    public static void OpenNewTerminalSession(string? workingDirectory = null, string? command = null, bool promptForCommand = false)
        => Common.SystemConsoleView.OpenNewTerminalSession(workingDirectory, command, promptForCommand);

    public static void OpenNewTerminalSession(string workingDirectory)
        => Common.SystemConsoleView.OpenNewTerminalSession(workingDirectory, null, false);

    public static void OpenNewTerminalSession()
        => Common.SystemConsoleView.OpenNewTerminalSession(null, null, false);

    public static bool KillPort(int port) => Common.SystemConsoleView.KillPort(port);

    public static void OpenExplorer(string? path = null) => Common.SystemConsoleView.OpenExplorer(path);

    public static void ShowDiskSpace() => Common.SystemConsoleView.ShowDiskSpace();

    public static string GetPublicIP() => Common.SystemConsoleView.GetPublicIP();

    public static void StopProcessFriendly(string? name = null) => Common.SystemConsoleView.StopProcessFriendly(name);
}
