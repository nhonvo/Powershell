using AgyTui.Domain.WorkspaceContext;

namespace AgyTui.Infrastructure.Services;

public interface IWorkspaceRegistry
{
    WorkspaceEntry[] GetWorkspaces();
    WorkspaceAggregate[] GetWorkspaceAggregates();
    int SyncAllProjects(string? customBaseDir = null);
    void SaveWorkspaces(WorkspaceEntry[] entries);
    WorkspaceEntry[] FindByQuery(string query, bool asRegex = false);
    WorkspaceEntry[] GetByAccount(string accountName);
    string GetGitBranch(string dirPath);
    string HandleWorkspaceAction(WorkspaceEntry selected, int actionIdx);
}
