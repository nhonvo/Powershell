using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        var rawName = AnsiConsole.Ask<string>("[cyan]Project name:[/]").Trim();
        if (string.IsNullOrWhiteSpace(rawName))
        {
            SpectrePanel.Warning("Project name cannot be empty.");
            return;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var name = new string(rawName.Where(c => !invalidChars.Contains(c)).ToArray());
        if (string.IsNullOrWhiteSpace(name))
        {
            SpectrePanel.Warning("Project name contains invalid characters.");
            return;
        }

        var outputDir = AnsiConsole.Ask<string>("[dim]Output directory[/] (Enter for current):", Directory.GetCurrentDirectory()).Trim();
        Directory.CreateDirectory(outputDir);

        SpectreProgress.Spinner($"Scaffolding {template} project '{name}'…", () =>
        {
            if (template == "react (Vite)")
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "npm.cmd" : "npm",
                    WorkingDirectory = outputDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("create");
                psi.ArgumentList.Add("vite@latest");
                psi.ArgumentList.Add(name);
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add("--template");
                psi.ArgumentList.Add("react-ts");

                using var proc = Process.Start(psi);
                proc?.WaitForExit();
            }
            else
            {
                var targetPath = Path.Combine(outputDir, name);
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    WorkingDirectory = outputDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("new");
                psi.ArgumentList.Add(template);
                psi.ArgumentList.Add("-n");
                psi.ArgumentList.Add(name);
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(targetPath);

                using var proc = Process.Start(psi);
                proc?.WaitForExit();
            }
        });
        SpectrePanel.Success($"Project '{name}' created at {Path.Combine(outputDir, name)}");

        if (AnsiConsole.Confirm("Do you want to launch Claude immediately to write initial tests and features in this new project?"))
        {
            var targetDir = Path.Combine(outputDir, name);
            if (Directory.Exists(targetDir))
            {
                Directory.SetCurrentDirectory(targetDir);
                AgyAiCore.InvokeClaude(["--prompt", "Implement the first feature and write initial tests for this scaffolded " + template + " project"]);
            }
        }
    }
}
