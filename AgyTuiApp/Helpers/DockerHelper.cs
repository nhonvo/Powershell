using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class DockerHelper
{
    private static readonly string[] CleanupOptions = [
        "Stop & remove all running containers",
        "Prune unused images and dangling layers",
        "Delete unused volumes",
        "Delete unused networks",
        "Full cleanup (all of the above)",
        "Cancel"
    ];

    public static void ShowCleanupDashboard()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Docker Cleanup Dashboard[/]").RuleStyle("grey"));
        var idx = SpectreMenu.Show("Select cleanup action", CleanupOptions, 0, false);
        switch (idx)
        {
            case 0:
                RunDocker("stop $(docker ps -q)");
                RunDocker("rm $(docker ps -aq)");
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

    public static int ComposeUp(string? composeFile = null)
    {
        var args = composeFile != null ? $"-f {composeFile} up -d" : "up -d";
        return RunDockerCompose(args);
    }

    public static int ComposeDown(string? composeFile = null)
    {
        var args = composeFile != null ? $"-f {composeFile} down" : "down";
        return RunDockerCompose(args);
    }

    private static void RunDocker(string args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Helpers.ProcessRunner.Run("cmd", $"/c docker {args}");
        }
        else
        {
            Helpers.ProcessRunner.Run("sh", $"-c \"docker {args}\"");
        }
    }

    private static int RunDockerCompose(string args)
    {
        return Helpers.ProcessRunner.Run("docker", $"compose {args}");
    }
}
