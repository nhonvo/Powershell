using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AgyTui.Components;
using Spectre.Console;

namespace AgyTui;

public static class DotNetHelper
{
    public static void RemoveBinObj(string rootPath)
    {
        var targets = new[] { "bin", "obj" };
        var deleted = new List<string>();
        var failed = new List<string>();
        foreach (var dir in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
        {
            if (!targets.Contains(Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase)) continue;

            try
            {
                Directory.Delete(dir, recursive: true);
                deleted.Add(dir);
            }
            catch
            {
                failed.Add(dir);
            }
        }
        SpectreTable.Render(["Status", "Path"], [.. deleted.Select(d => new[]
        {
            "[green]Deleted[/]", d.EscapeMarkup()
        }
        ).Concat(failed.Select(f => new[]
        {
            "[red]Failed[/]", f.EscapeMarkup()
        }
        ))], markup: true);
    }

    public static int Build(string? projectPath = null) => RunDotnet("build", projectPath);

    public static int Run(string? projectPath = null) => RunDotnet("run", projectPath);

    public static int Test(string? projectPath = null) => RunDotnet("test", projectPath);

    public static int Format(string? projectPath = null) => RunDotnet("format", projectPath);

    public static int Clean(string? projectPath = null) => RunDotnet("clean", projectPath);

    public static int Restore(string? projectPath = null) => RunDotnet("restore", projectPath);

    public static int Publish(string? projectPath = null) => RunDotnet("publish csapp/AgyTuiApp/AgyTuiApp.csproj -c Release -r win-x64 --self-contained -o csapp/AgyTuiApp/dist", projectPath);

    public static int Pack(string? projectPath = null, string outputDir = "nupkg")
    {
        SpectrePanel.Info("Packing NuGet package...");
        var exitCode = RunDotnet($"pack -c Release -o {outputDir}", projectPath);
        if (exitCode == 0) SpectrePanel.Success($"Package generated in ./{outputDir}/ directory.");
        else SpectrePanel.Error($"dotnet pack failed (exit {exitCode}).");
        return exitCode;
    }

    public static int PublishPackage(string? nupkgPath = null, string? apiKey = null, string source = "https://api.nuget.org/v3/index.json")
    {
        if (string.IsNullOrEmpty(nupkgPath))
        {
            var packages = Directory.Exists("nupkg") ? Directory.GetFiles("nupkg", "*.nupkg") : Array.Empty<string>();
            if (packages.Length == 0)
            {
                SpectrePanel.Warning("No .nupkg files found in ./nupkg directory. Running dotnet pack first...");
                var packExit = Pack();
                if (packExit != 0) return packExit;
                packages = Directory.Exists("nupkg") ? Directory.GetFiles("nupkg", "*.nupkg") : Array.Empty<string>();
            }

            if (packages.Length == 0)
            {
                SpectrePanel.Error("No .nupkg package found to publish.");
                return 1;
            }

            if (packages.Length == 1)
            {
                nupkgPath = packages[0];
            }
            else
            {
                var idx = SpectreMenu.Show("Select NuGet Package to Push", packages, 0);
                if (idx < 0) return 0;
                nupkgPath = packages[idx];
            }
        }

        var keyArg = string.IsNullOrEmpty(apiKey) ? "" : $"--api-key {apiKey}";
        SpectrePanel.Info($"Pushing package {Path.GetFileName(nupkgPath)} to {source}...");
        var exit = RunDotnet($"nuget push \"{nupkgPath}\" --source \"{source}\" {keyArg} --skip-duplicate", null);
        if (exit == 0) SpectrePanel.Success("NuGet package published successfully!");
        else SpectrePanel.Error($"dotnet nuget push failed (exit {exit}).");
        return exit;
    }

    public static int Watch(string? projectPath = null) => RunDotnet("watch run", projectPath);

    public static int AddMigration(string migrationName, string? project = null) => RunDotnet($"ef migrations add {migrationName}", project);

    public static int UpdateDatabase(string? project = null) => RunDotnet("ef database update", project);

    private static int RunDotnet(string args, string? workingDir)
    {
        return Helpers.ProcessRunner.Run("dotnet", args, workingDir);
    }
}
