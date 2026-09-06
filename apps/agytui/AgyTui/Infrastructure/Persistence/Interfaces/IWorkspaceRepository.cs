namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface IWorkspaceRepository : IRepository<WorkspaceAggregate, string>
{
    IEnumerable<WorkspaceAggregate> GetAllWorkspaces();
    WorkspaceAggregate? GetWorkspace(string name);
    void SaveWorkspace(WorkspaceAggregate workspace);
    void DeleteWorkspace(string name);
}
