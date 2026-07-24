using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class Projects
{
    public static readonly string AgBaseDir = !string.IsNullOrEmpty(Config.Current.ProjectsBaseDir) 
        ? Config.Current.ProjectsBaseDir 
        : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "project");

    public static string? StartProxy()
    {
        var projectDir = System.IO.Path.Combine(AgBaseDir, "antigravity-claude-proxy");
        if (!Directory.Exists(projectDir))
        {
            SpectrePanel.Error($"Project not found: {projectDir}");
            return null;
        }
        AnsiConsole.MarkupLine("[cyan]🛸 Proxy env set (BASE_URL=localhost:8080)[/]");
        var env = new Dictionary<string, string?>
        {
            ["ANTHROPIC_BASE_URL"] = "http://localhost:8080",
            ["ANTHROPIC_AUTH_TOKEN"] = "test"
        };
        RunNpmSetupAndStart(projectDir, "Antigravity Proxy", env);
        return projectDir;
    }

    private static void RunNpmSetupAndStart(string projectDir, string label, IDictionary<string, string?>? env)
    {
        AnsiConsole.MarkupLine("[cyan][[1/2]] 📦 Checking dependencies...[/]");
        if (!Directory.Exists(System.IO.Path.Combine(projectDir, "node_modules")))
        {
            AnsiConsole.MarkupLine("[yellow] -> Installing (npm install)...[/]");
            RunNpm("install", projectDir, env);
        }
        else
        {
            AnsiConsole.MarkupLine("[green] -> node_modules OK.[/]");
        }
        AnsiConsole.MarkupLine($"[green][[2/2]] 🚀 Launching {label.EscapeMarkup()}...[/]");
        RunNpm("start", projectDir, env);
    }

    private static void RunNpm(string args, string workingDir, IDictionary<string, string?>? env)
    {
        Helpers.ProcessRunner.Run("npm.cmd", args, workingDir);
    }
}
