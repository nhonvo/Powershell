namespace AgyTui.Infrastructure;

using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Git;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interop bridge providing static wrappers for PowerShell profile commands.
/// </summary>
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
