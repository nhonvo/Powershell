namespace AgyTui.Infrastructure.Integrations.Sys;

public static class AntigravityDeckHelper
{
    private static readonly string DeckPath = AppPaths.DeckDataDir;

    private static bool EnsureDeckPathExists()
    {
        if (!Directory.Exists(DeckPath))
        {
            SpectrePanel.Error($"Antigravity Deck path not found at {DeckPath}. Please install it first.");
            Thread.Sleep(2000);
            return false;
        }
        return true;
    }

    public static void Setup()
    {
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Running: npm run setup...[/]");
        RunNpmCommand("run", "setup");
    }

    public static void StartLocal()
    {
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Local dev server on port 3000)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "dev");
    }

    public static void StartOnline()
    {
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Cloudflare Tunnel)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "online");
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
                Helpers.ProcessRunner.RunInteractive("cmd.exe", argsList, workingDir: DeckPath);
            }
            else
            {
                argsList.Add(cmd);
                if (!string.IsNullOrEmpty(arg)) argsList.Add(arg);
                Helpers.ProcessRunner.RunInteractive("npm", argsList, workingDir: DeckPath);
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
