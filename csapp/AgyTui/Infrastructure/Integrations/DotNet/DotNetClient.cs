namespace AgyTui.Infrastructure.Integrations.DotNet;

public class DotNetClient : CliToolWrapper, IDotNetClient
{
    public DotNetClient() : base("dotnet")
    {
    }

    public void RemoveBinObj(string rootPath)
    {
        if (!Directory.Exists(rootPath)) return;

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
        }).Concat(failed.Select(f => new[]
        {
            "[red]Failed[/]", f.EscapeMarkup()
        }))], markup: true);
    }

    private int RunDotnetWithTarget(string command, string? targetOrWorkingDir)
    {
        string FormatCommand(string cmd, string target)
        {
            if (target.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || target.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                if (cmd.Equals("run", StringComparison.OrdinalIgnoreCase) || cmd.StartsWith("watch", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{cmd} --project \"{target}\"";
                }
                return $"{cmd} \"{target}\"";
            }
            return cmd;
        }

        if (!string.IsNullOrEmpty(targetOrWorkingDir))
        {
            if (File.Exists(targetOrWorkingDir) || targetOrWorkingDir.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || targetOrWorkingDir.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                return RunDotnet(FormatCommand(command, targetOrWorkingDir), null);
            }
            if (Directory.Exists(targetOrWorkingDir))
            {
                var csInDir = Directory.GetFiles(targetOrWorkingDir, "*.csproj", SearchOption.TopDirectoryOnly);
                if (csInDir.Length > 0) return RunDotnet(FormatCommand(command, csInDir[0]), targetOrWorkingDir);
            }
        }

        var cwd = Directory.GetCurrentDirectory();
        var csInCwd = Directory.GetFiles(cwd, "*.csproj", SearchOption.TopDirectoryOnly);
        if (csInCwd.Length == 0)
        {
            var csSub = Directory.GetFiles(cwd, "*.csproj", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("/obj/") && !f.Contains("\\bin\\") && !f.Contains("/bin/"))
                .OrderBy(f => f.Length)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(csSub))
            {
                return RunDotnet(FormatCommand(command, csSub), cwd);
            }
        }

        return RunDotnet(command, targetOrWorkingDir);
    }

    public int Build(string? projectPath = null) => RunDotnetWithTarget("build", projectPath);

    public int Run(string? projectPath = null) => RunDotnetWithTarget("run", projectPath);

    public int Test(string? projectPath = null) => RunDotnetWithTarget("test", projectPath);

    public int Format(string? projectPath = null) => RunDotnetWithTarget("format", projectPath);

    public int Clean(string? projectPath = null) => RunDotnetWithTarget("clean", projectPath);

    public int Restore(string? projectPath = null) => RunDotnetWithTarget("restore", projectPath);

    public int Publish(string? projectPath = null) => RunDotnet("publish csapp/AgyTui/AgyTui.csproj -c Release -r win-x64 --self-contained -o csapp/AgyTui/dist", projectPath);

    public int Pack(string? projectPath = null, string outputDir = "nupkg")
    {
        SpectrePanel.Info("Packing NuGet package...");
        var exitCode = RunDotnetWithTarget($"pack -c Release -o {outputDir}", projectPath);
        if (exitCode == 0) SpectrePanel.Success($"Package generated in ./{outputDir}/ directory.");
        else SpectrePanel.Error($"dotnet pack failed (exit {exitCode}).");
        return exitCode;
    }

    public int PublishPackage(string? nupkgPath = null, string? apiKey = null, string source = "https://api.nuget.org/v3/index.json")
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

        SpectrePanel.Info($"Pushing package {Path.GetFileName(nupkgPath)} to {source}...");
        var env = new Dictionary<string, string?>();
        if (!string.IsNullOrEmpty(apiKey))
        {
            env["NUGET_API_KEY"] = apiKey;
        }
        var pushArgs = new List<string> { "nuget", "push", nupkgPath, "--source", source, "--skip-duplicate" };
        RunInteractive(pushArgs, env);
        SpectrePanel.Success("NuGet package publish command completed.");
        return 0;
    }

    public int Watch(string? projectPath = null) => RunDotnetWithTarget("watch run", projectPath);

    public int AddMigration(string migrationName, string? project = null, string? context = null)
    {
        var args = new List<string> { "ef", "migrations", "add", migrationName };
        if (!string.IsNullOrEmpty(project)) { args.Add("--project"); args.Add(project); }
        if (!string.IsNullOrEmpty(context)) { args.Add("--context"); args.Add(context); }
        RunInteractive(args);
        return 0;
    }

    public int UpdateDatabase(string? project = null, string? context = null)
    {
        var args = new List<string> { "ef", "database", "update" };
        if (!string.IsNullOrEmpty(project)) { args.Add("--project"); args.Add(project); }
        if (!string.IsNullOrEmpty(context)) { args.Add("--context"); args.Add(context); }
        RunInteractive(args);
        return 0;
    }

    public int AddAllProjectsToSolution()
    {
        var cwd = Directory.GetCurrentDirectory();
        var projects = Directory.GetFiles(cwd, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\obj\\") && !f.Contains("/obj/") && !f.Contains("\\bin\\") && !f.Contains("/bin/"))
            .ToList();

        if (projects.Count == 0)
        {
            SpectrePanel.Warning("No .csproj projects found in directory structure.");
            return 0;
        }

        foreach (var proj in projects)
        {
            SpectrePanel.Info($"Adding {Path.GetFileName(proj)} to solution...");
            RunDotnet($"sln add \"{proj}\"", cwd);
        }

        SpectrePanel.Success($"Added {projects.Count} project(s) to solution.");
        return 0;
    }

    public int NewSolution(string name) => RunDotnet($"new sln -n \"{name}\"", null);
    public int NewConsole(string name) => RunDotnet($"new console -n \"{name}\"", null);
    public int NewWebApi(string name) => RunDotnet($"new webapi -n \"{name}\"", null);

    private int RunDotnet(string args, string? workingDir)
    {
        return Helpers.ProcessRunner.Instance.Run(BinaryName, args, workingDir);
    }
}
