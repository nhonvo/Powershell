namespace AgyTui.Domain.WorkspaceContext;

public class WorkspaceAggregate
{
    public string Name { get; private set; }
    public ProjectPath WorkspacePath { get; private set; }
    public string CorpusName { get; private set; }
    public bool IsActive { get; private set; }
    public string? GitBranch { get; private set; }

    public WorkspaceAggregate(string name, string workspacePath, string corpusName, bool isActive = false, string? gitBranch = null)
    {
        Name = name;
        WorkspacePath = new ProjectPath(workspacePath);
        CorpusName = corpusName;
        IsActive = isActive;
        GitBranch = gitBranch;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void SetBranch(string? branch) => GitBranch = branch;
}
