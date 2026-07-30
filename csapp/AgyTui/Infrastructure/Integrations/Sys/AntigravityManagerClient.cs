namespace AgyTui.Infrastructure.Integrations.Sys;

public static class AntigravityManagerHelper
{
    private static readonly string ManagerPath = GetManagerPath();

    private static string GetManagerPath()
    {
        var primaryPath = Path.Combine(AppPaths.UserProfileDir, "project", "agy", "AntigravityManager");
        if (Directory.Exists(primaryPath))
        {
            return primaryPath;
        }

        // Fallback to desktop/project path
        var agBaseDir = !string.IsNullOrEmpty(Config.Current.Project.BaseDir)
            ? Config.Current.Project.BaseDir
            : AppPaths.DefaultDesktopProjectDir;
        return Path.Combine(agBaseDir, "AntigravityManager");
    }

    private static bool EnsureManagerPathExists()
    {
        if (!Directory.Exists(ManagerPath))
        {
            SpectrePanel.Error($"Antigravity Manager path not found at {ManagerPath}.");
            Thread.Sleep(2000);
            return false;
        }
        return true;
    }

    public static void Setup()
    {
        if (!EnsureManagerPathExists()) return;
        AnsiConsole.MarkupLine("[cyan]📦 Installing dependencies (npm install)...[/]");
        RunNpmCommand("install", "");
        SpectrePanel.Success("Dependencies installed successfully!");
        Thread.Sleep(2000);
    }

    public static void StartLocal()
    {
        if (!EnsureManagerPathExists()) return;

        AnsiConsole.MarkupLine("[cyan][[1/2]] 📦 Checking dependencies...[/]");
        if (!Directory.Exists(Path.Combine(ManagerPath, "node_modules")))
        {
            AnsiConsole.MarkupLine("[yellow] -> Installing (npm install)...[/]");
            RunNpmCommand("install", "");
        }
        else
        {
            AnsiConsole.MarkupLine("[green] -> node_modules OK.[/]");
        }

        AnsiConsole.MarkupLine("[green][[2/2]] 🚀 Launching Antigravity Manager...[/]");
        RunNpmCommand("start", "");
    }

    private static void RunNpmCommand(string cmd, string arg)
    {
        try
        {
            var argsList = new List<string>();
            if (OperatingSystem.IsWindows())
            {
                argsList.Add("/c");
                argsList.Add("npm");
                argsList.Add(cmd);
                if (!string.IsNullOrEmpty(arg)) argsList.Add(arg);
                Helpers.ProcessRunner.RunInteractive("cmd.exe", argsList, workingDir: ManagerPath);
            }
            else
            {
                argsList.Add(cmd);
                if (!string.IsNullOrEmpty(arg)) argsList.Add(arg);
                Helpers.ProcessRunner.RunInteractive("npm", argsList, workingDir: ManagerPath);
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to run npm command: {ex.Message}");
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
        }
    }
}
