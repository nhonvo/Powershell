using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class ProjectScaffolder
{
    private static readonly string[] Templates = [
        "webapi (.NET Core Web API)",
        "mvc (.NET Core MVC Web App)",
        "razor (.NET Core Razor Pages Web App)",
        "blazor (Blazor Web App)",
        "blazorwasm (Blazor WebAssembly)",
        "console (.NET Console App)",
        "classlib (.NET Class Library)",
        "worker (.NET Worker Service)",
        "xunit (xUnit Test Project)",
        "react (Vite React + TS)",
        "vue (Vite Vue + TS)",
        "nextjs (Next.js React + TS App)"
    ];

    public static void Scaffold()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Project Scaffolder[/]").RuleStyle("grey"));
        var idx = SpectreMenu.Show("Select template", Templates, 0, false);
        if (idx < 0) return;
        
        var selectedItem = Templates[idx];
        var template = selectedItem.Split(' ')[0].ToLowerInvariant();
        
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
            if (template == "react" || template == "vue")
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
                psi.ArgumentList.Add(template == "react" ? "react-ts" : "vue-ts");

                using var proc = Process.Start(psi);
                proc?.WaitForExit();
            }
            else if (template == "nextjs")
            {
                var psi = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "npx.cmd" : "npx",
                    WorkingDirectory = outputDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add("create-next-app@latest");
                psi.ArgumentList.Add(name);
                psi.ArgumentList.Add("--ts");
                psi.ArgumentList.Add("--eslint");
                psi.ArgumentList.Add("--tailwind");
                psi.ArgumentList.Add("--src-dir");
                psi.ArgumentList.Add("--app");
                psi.ArgumentList.Add("--import-alias");
                psi.ArgumentList.Add("@/*");
                psi.ArgumentList.Add("--use-npm");

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
    }
}
