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

    public static int Publish(string? projectPath = null) => RunDotnet("publish AgyTuiApp/AgyTuiApp.csproj -c Release -r win-x64 --self-contained -o dist", projectPath);

    public static int Watch(string? projectPath = null) => RunDotnet("watch run", projectPath);

    public static int AddMigration(string migrationName, string? project = null) => RunDotnet($"ef migrations add {migrationName}", project);

    public static int UpdateDatabase(string? project = null) => RunDotnet("ef database update", project);

    private static int RunDotnet(string args, string? workingDir)
    {
        return Helpers.ProcessRunner.Run("dotnet", args, workingDir);
    }
}
