using System;
using System.IO;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class ProjectScaffolder
{
    private static readonly string[] Templates = ["webapi", "console", "react (Vite)", "blazorwasm", "classlib", "worker"];

    public static void Scaffold()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Project Scaffolder[/]").RuleStyle("grey"));
        var idx = SpectreMenu.Show("Select template", Templates, 0, false);
        if (idx < 0) return;
        var template = Templates[idx];
        var name = AnsiConsole.Ask<string>("[cyan]Project name:[/]").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SpectrePanel.Warning("Project name cannot be empty.");
            return;
        }
        var outputDir = AnsiConsole.Ask<string>("[dim]Output directory[/] (Enter for current):", Directory.GetCurrentDirectory()).Trim();
        SpectreProgress.Spinner($"Scaffolding {template} project '{name}'…", () =>
        {
            if (template == "react (Vite)")
            {
                Directory.CreateDirectory(Path.Combine(outputDir, name));
                Helpers.ProcessRunner.Run("npm.cmd", $"create vite@latest {name} -- --template react-ts");
            }
            else
            {
                Helpers.ProcessRunner.Run("dotnet", $"new {template} -n {name} -o \"{Path.Combine(outputDir, name)}\"");
            }
        });
        SpectrePanel.Success($"Project '{name}' created at {Path.Combine(outputDir, name)}");

        if (AnsiConsole.Confirm("Do you want to launch Claude immediately to write initial tests and features in this new project?"))
        {
            var targetDir = Path.Combine(outputDir, name);
            Directory.SetCurrentDirectory(targetDir);
            AgyAiCore.InvokeClaude(["--prompt", "Implement the first feature and write initial tests for this scaffolded " + template + " project"]);
        }
    }
}
