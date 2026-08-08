using AgyTui.Infrastructure.Integrations.Git;

namespace AgyTui.UI.Core.Navigation.Routers;

public class GitCommandRouter
{
    private readonly IGitClient _git;

    public GitCommandRouter(IGitClient git)
    {
        _git = git;
    }

    public bool TryHandle(string alias, string[] args, out int exitCode)
    {
        exitCode = 0;
        switch (alias.ToLowerInvariant())
        {
            case "git-status":
            case "gst":
                _git.ShowStatus();
                return true;
            case "git-branches":
            case "gbr":
                _git.ShowBranches();
                return true;
            case "git-commit-wizard":
            case "gcmt":
                _git.ConventionalCommitWizard();
                return true;
            case "git-conflict":
            case "gconflict":
                _git.ShowConflictResolver();
                return true;
            case "git-stash":
            case "gstash":
                _git.ShowStashManager();
                return true;
            case "git-rebase":
            case "grebase":
                _git.ShowRebaseWizard();
                return true;
            default:
                return false;
        }
    }
}
