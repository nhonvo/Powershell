namespace AgyTui.Domain.WorkspaceContext;

public class WorkspaceAggregate
{
    public string Name { get; private set; }
    public ProjectPath WorkspacePath { get; private set; }
    public string CorpusName { get; private set; }
    public bool IsActive { get; private set; }
    public string? GitBranch { get; private set; }
    public string? Alias { get; private set; }
    public string[] Tags { get; private set; }

    public WorkspaceAggregate(string name, string workspacePath, string corpusName, bool isActive = false, string? gitBranch = null, string? alias = null, IEnumerable<string>? tags = null)
    {
        Name = name;
        WorkspacePath = new ProjectPath(workspacePath);
        CorpusName = corpusName;
        IsActive = isActive;
        GitBranch = gitBranch;
        Alias = alias;
        Tags = tags != null ? tags.ToArray() : Array.Empty<string>();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void SetBranch(string? branch) => GitBranch = branch;

    public WorkspaceEntry ToEntry()
    {
        return new WorkspaceEntry(Name, WorkspacePath.Value, CorpusName, Tags, null, Alias);
    }

    public static WorkspaceAggregate FromEntry(WorkspaceEntry entry, bool isActive = false, string? gitBranch = null)
    {
        return new WorkspaceAggregate(
            entry.Name,
            entry.WorkspacePath,
            entry.AssociatedAccount ?? "default",
            isActive,
            gitBranch,
            entry.Alias,
            entry.Tags
        );
    }
}

