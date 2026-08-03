namespace AgyTui.Infrastructure.Integrations.Docker;

public interface IDockerClient
{
    void ShowCleanupDashboard();
    void ShowDockerHealthDashboard();
    int ComposeUp(string? composeFile = null);
    int ComposeDown(string? composeFile = null);
    void ShowImages();
    void ShowContainerLogs();
    void RemoveAllContainers();
    void StopAllContainers();
    void ShowContainers();
    int ComposeUpBuild(string? composeFile = null);
    void PruneVolumes();
    void PruneImages();
}
