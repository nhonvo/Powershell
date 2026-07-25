namespace AgyTui.Infrastructure;

public static class GitHelper
{
    public static void ShowStatus() => AgyServices.Git.ShowStatus();
    public static void ShowLog() => AgyServices.Git.ShowLog();
    public static void ShowBranches() => AgyServices.Git.ShowBranches();
    public static void Checkout(string? branchName) => AgyServices.Git.Checkout(branchName);
    public static void AddAll() => AgyServices.Git.AddAll();
    public static void ConventionalCommitWizard() => AgyServices.Git.ConventionalCommitWizard();
    public static void InvokeGitUndo() => AgyServices.Git.InvokeGitUndo();
    public static void Fetch() => AgyServices.Git.Fetch();
    public static void Pull() => AgyServices.Git.Pull();
    public static void Push() => AgyServices.Git.Push();
}

public static class DotNetHelper
{
    public static int Build(string[]? args = null) => AgyServices.DotNet.Build();
    public static int Run(string[]? args = null) => AgyServices.DotNet.Run();
    public static int Test(string[]? args = null) => AgyServices.DotNet.Test();
    public static int Format(string[]? args = null) => AgyServices.DotNet.Format();
    public static int Clean(string[]? args = null) => AgyServices.DotNet.Clean();
    public static int Restore(string[]? args = null) => AgyServices.DotNet.Restore();
    public static int Publish(string[]? args = null) => AgyServices.DotNet.Publish();
    public static int Pack(string[]? args = null) => AgyServices.DotNet.Pack();
    public static int PublishPackage(string[]? args = null) => AgyServices.DotNet.PublishPackage();
    public static int Watch(string[]? args = null) => AgyServices.DotNet.Watch();
    public static void CleanBinObj() => AgyServices.DotNet.RemoveBinObj(Directory.GetCurrentDirectory());
    public static int AddMigration(string migrationName, string? context = null) => AgyServices.DotNet.AddMigration(migrationName);
    public static int UpdateDatabase(string? context = null) => AgyServices.DotNet.UpdateDatabase();
}

public static class DockerHelper
{
    public static void ShowDockerHealthDashboard() => AgyServices.Docker.ShowDockerHealthDashboard();
    public static void ShowCleanupDashboard() => AgyServices.Docker.ShowCleanupDashboard();
    public static void RemoveAllContainers() => AgyServices.Docker.RemoveAllContainers();
    public static void StopAllContainers() => AgyServices.Docker.StopAllContainers();
    public static void ShowImages() => AgyServices.Docker.ShowImages();
    public static void ShowContainerLogs() => AgyServices.Docker.ShowContainerLogs();
    public static void ComposeUp() => AgyServices.Docker.ComposeUp();
    public static void ComposeDown() => AgyServices.Docker.ComposeDown();
}

public static class AwsHelper
{
    public static string LocalStackUrl => "http://localhost:4566";
    public static void ShowCallerIdentity() => AgyServices.Aws.ShowCallerIdentity();
    public static void ShowLocalStackInfo() => AgyServices.Aws.ShowLocalStackInfo();
    public static void ShowS3Buckets() => AgyServices.Aws.ShowS3Buckets();
    public static void ShowSQSQueues() => AgyServices.Aws.ShowSQSQueues();
    public static void ShowSsmParameters() => AgyServices.Aws.ShowSsmParameters();
    public static void ShowSnsTopics() => AgyServices.Aws.ShowSnsTopics();
    public static void ShowDynamoDbTables() => AgyServices.Aws.ShowDynamoDbTables();
    public static void ShowLambdaFunctions() => AgyServices.Aws.ShowLambdaFunctions();
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
