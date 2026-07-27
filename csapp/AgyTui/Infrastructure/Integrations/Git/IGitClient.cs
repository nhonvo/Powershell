namespace AgyTui.Infrastructure.Integrations.Git;

public interface IGitClient
{
    void ShowStatus();
    void ShowBranches();
    void ShowLog();
    void Pull();
    void Push();
    void AddAll();
    void Fetch();

    void Checkout(string? branchName = null);
    void ConventionalCommitWizard();
    void InvokeGitUndo();
}
