namespace AgyTui.Infrastructure.Integrations.Git;

public interface IGitClient
{
    void ShowStatus();
    void ShowStatusNative(string[]? passArgs = null);
    void ShowBranches();
    void ShowLog();
    void Pull();
    void Push();
    void AddAll();
    void Fetch();

    void Checkout(string? branchName = null);
    void ConventionalCommitWizard();
    void InvokeGitUndo();
    void ShowDiff();
    void ShowLogGraph();
    void ShowLogPretty();
    void NewBranch(string? branchName = null);
    void RemoveBranch(string? branchName = null);
    void UnstageAll();
    void CommitAmend(string[]? passArgs = null);
    void ResetSoft();
    void ResetHard();
    void PushForce(string[]? passArgs = null);
    void CloneProject(string? url = null, string? destName = null);
}
