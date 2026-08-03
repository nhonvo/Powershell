using System.Runtime.InteropServices;

namespace AgyTui.Infrastructure.Integrations.Docker;

public class DockerClient : CliToolWrapper, IDockerClient
{
    private static readonly string[] CleanupOptions = [
        "Stop & remove all running containers",
        "Prune unused images and dangling layers",
        "Delete unused volumes",
        "Delete unused networks",
        "Full cleanup (all of the above)",
        "Cancel"
    ];

    public DockerClient() : base("docker")
    {
    }

    public void ShowCleanupDashboard()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Docker Cleanup Dashboard[/]").RuleStyle("grey"));
        var idx = SpectreMenu.Show("Select cleanup action", CleanupOptions, 0, false);
        if (idx < 0 || idx >= CleanupOptions.Length) return;

        bool confirm = Console.IsInputRedirected || AnsiConsole.Confirm($"Are you sure you want to perform: [yellow]{CleanupOptions[idx].EscapeMarkup()}[/]?");
        if (!confirm)
        {
            SpectrePanel.Info("Cleanup cancelled.");
            return;
        }

        switch (idx)
        {
            case 0:
                StopAllContainers();
                RemoveAllContainers();
                break;
            case 1:
                RunDocker("image prune -af");
                break;
            case 2:
                RunDocker("volume prune -f");
                break;
            case 3:
                RunDocker("network prune -f");
                break;
            case 4:
                SpectreProgress.BulkProgress("Docker cleanup", CleanupOptions[..4], (i, step) =>
                {
                    switch (i)
                    {
                        case 0:
                            StopAllContainers();
                            RunDocker("container prune -f");
                            break;
                        case 1:
                            RunDocker("image prune -af");
                            break;
                        case 2:
                            RunDocker("volume prune -f");
                            break;
                        case 3:
                            RunDocker("network prune -f");
                            break;
                    }
                });
                break;
            default:
                return;
        }
        SpectrePanel.Success("Docker cleanup completed.");
    }

    public void ShowDockerHealthDashboard()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Docker Health Dashboard[/]").RuleStyle("grey"));

        try
        {
            var psOutput = RunCapture("ps --format \"table {{.ID}}\\t{{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
            if (string.IsNullOrWhiteSpace(psOutput) || psOutput.Contains("error during connect"))
            {
                SpectrePanel.Warning("Docker daemon is not running or not responding.");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold green]Running Containers:[/]");
                AnsiConsole.WriteLine(psOutput);

                var statsOutput = RunCapture("stats --no-stream --format \"table {{.Name}}\\t{{.CPUPerc}}\\t{{.MemUsage}}\\t{{.NetIO}}\"");
                if (!string.IsNullOrWhiteSpace(statsOutput))
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold green]Resource Usage Stats:[/]");
                    AnsiConsole.WriteLine(statsOutput);
                }
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to query Docker: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public int ComposeUp(string? composeFile = null)
    {
        var args = composeFile != null ? $"-f {composeFile} up -d" : "up -d";
        return RunDockerCompose(args);
    }

    public int ComposeDown(string? composeFile = null)
    {
        var args = composeFile != null ? $"-f {composeFile} down" : "down";
        return RunDockerCompose(args);
    }

    public void ShowImages()
    {
        var output = RunCapture("images --format \"table {{.Repository}}\\t{{.Tag}}\\t{{.Size}}\\t{{.CreatedAt}}\"");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No Docker images found or Docker daemon offline.");
            return;
        }
        SpectrePager.Show("Docker Images", output);
    }

    public void ShowContainerLogs()
    {
        var output = RunCapture("ps --format \"{{.ID}}\\t{{.Names}}\\t{{.Status}}\"");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No running Docker containers found.");
            return;
        }
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var selectedIdx = SpectreMenu.Show("Select Container to Tail Logs", lines, 0, false);
        if (selectedIdx < 0) return;

        var containerId = lines[selectedIdx].Split('\t')[0];
        var containerName = lines[selectedIdx].Split('\t')[1];
        AnsiConsole.MarkupLine($"[cyan]Tailing last 200 lines of logs for [bold green]{containerName.EscapeMarkup()}[/]:[/]");
        var logs = RunCapture($"logs --tail 200 {containerId}");
        SpectrePager.Show($"Logs: {containerName}", logs);
    }

    private string[] GetContainerIds(bool runningOnly)
    {
        var filter = runningOnly ? "-q" : "-aq";
        var output = RunCapture($"ps {filter}");
        if (string.IsNullOrWhiteSpace(output)) return Array.Empty<string>();
        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public void RemoveAllContainers()
    {
        AnsiConsole.MarkupLine("[cyan]Removing all Docker containers...[/]");
        var ids = GetContainerIds(false);
        if (ids.Length == 0)
        {
            SpectrePanel.Success("No containers to remove.");
            return;
        }
        if (!AnsiConsole.Confirm($"Remove {ids.Length} container(s)?")) return;
        Helpers.ProcessRunner.Instance.Run(BinaryName, $"rm -f {string.Join(" ", ids)}");
        SpectrePanel.Success("Executed container removal.");
    }

    public void StopAllContainers()
    {
        AnsiConsole.MarkupLine("[cyan]Stopping all Docker containers...[/]");
        var ids = GetContainerIds(true);
        if (ids.Length == 0)
        {
            SpectrePanel.Success("No running containers to stop.");
            return;
        }
        if (!AnsiConsole.Confirm($"Stop {ids.Length} running container(s)?")) return;
        Helpers.ProcessRunner.Instance.Run(BinaryName, $"stop {string.Join(" ", ids)}");
        SpectrePanel.Success("Executed container stop.");
    }

    private void RunDocker(string args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Helpers.ProcessRunner.Instance.Run("cmd", $"/c docker {args}");
        }
        else
        {
            Helpers.ProcessRunner.Instance.Run("sh", $"-c \"docker {args}\"");
        }
    }

    private int RunDockerCompose(string args)
    {
        return Helpers.ProcessRunner.Instance.Run(BinaryName, $"compose {args}");
    }

    public void ShowContainers() => Helpers.ProcessRunner.Instance.Run(BinaryName, "ps");
    public int ComposeUpBuild(string? composeFile = null) => RunDockerCompose("up --build");
    public void PruneVolumes() => Helpers.ProcessRunner.Instance.Run(BinaryName, "volume prune -f");
    public void PruneImages() => Helpers.ProcessRunner.Instance.Run(BinaryName, "image prune -af");
}
