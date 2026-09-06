namespace AgyTui.Infrastructure.Integrations.Sys;

public static class AntigravityDeckHelper
{
    private static readonly string DeckPath = AppPaths.DeckDataDir;

    private static bool EnsureDeckPathExists()
    {
        if (!Directory.Exists(DeckPath))
        {
            Directory.CreateDirectory(DeckPath);
        }

        var pkgPath = Path.Combine(DeckPath, "package.json");
        if (!File.Exists(pkgPath))
        {
            ScaffoldDefaultDeckApp();
        }

        return true;
    }

    private static void ScaffoldDefaultDeckApp()
    {
        try
        {
            Directory.CreateDirectory(DeckPath);
            var pkgPath = Path.Combine(DeckPath, "package.json");
            if (!File.Exists(pkgPath))
            {
                var pkgJson = """
                {
                  "name": "antigravity-deck",
                  "version": "1.0.0",
                  "description": "Antigravity Deck Local Server",
                  "main": "server.js",
                  "scripts": {
                    "setup": "npm install --no-audit",
                    "dev": "node server.js",
                    "online": "node server.js --online"
                  },
                  "dependencies": {
                    "qrcode-terminal": "^0.12.0"
                  }
                }
                """;
                File.WriteAllText(pkgPath, pkgJson);
            }

            var serverPath = Path.Combine(DeckPath, "server.js");
            if (!File.Exists(serverPath))
            {
                var serverJs = """
                const http = require('http');
                const PORT = process.env.PORT || 18789;
                const server = http.createServer((req, res) => {
                  res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
                  res.end('<h1>🌌 Antigravity Deck Server</h1><p>Online and listening on port ' + PORT + '</p>');
                });
                server.listen(PORT, '127.0.0.1', () => {
                  console.log(`🌌 Antigravity Deck Server listening on http://127.0.0.1:${PORT}`);
                });
                """;
                File.WriteAllText(serverPath, serverJs);
            }

            AnsiConsole.MarkupLine("[green]✨ Initialized default Antigravity Deck template (package.json & server.js).[/]");
            LogHelper.Log("[AntigravityDeckHelper] Initialized default Antigravity Deck template");
        }
        catch (Exception ex)
        {
            LogHelper.LogError("Failed to scaffold Antigravity Deck", ex);
            SpectrePanel.Error($"Failed to scaffold Antigravity Deck: {ex.Message}");
        }
    }

    private static void KillDeckPorts()
    {
        SystemHelper.Instance.KillPort(9808);
        SystemHelper.Instance.KillPort(9807);
        SystemHelper.Instance.KillPort(3500);
        SystemHelper.Instance.KillPort(3000);
        SystemHelper.Instance.KillPort(18789);
    }

    public static void Setup()
    {
        LogHelper.Log("[AntigravityDeckHelper] Running Setup");
        KillDeckPorts();
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Running: npm run setup...[/]");
        RunNpmCommand("run", "setup");
    }

    public static void StartLocal()
    {
        LogHelper.Log("[AntigravityDeckHelper] Running StartLocal");
        KillDeckPorts();
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Local dev server BE:3500 FE:3000)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "dev");
    }

    public static void StartOnline()
    {
        LogHelper.Log("[AntigravityDeckHelper] Running StartOnline");
        KillDeckPorts();
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Cloudflare Tunnel)...[/]");
        RunNpmCommand("run", "online", quiet: true);
    }

    private static void RunNpmCommand(string cmd, string arg, bool quiet = false)
    {
        try
        {
            LogHelper.Log($"[AntigravityDeckHelper] RunNpmCommand: cmd='{cmd}', arg='{arg}', workingDir='{DeckPath}', quiet={quiet}");
            var argsList = new List<string>();
            IDictionary<string, string?>? env = quiet ? new Dictionary<string, string?> { { "QUIET", "1" } } : null;

            if (OperatingSystem.IsWindows())
            {
                argsList.Add("/c");
                argsList.Add("npm");
                argsList.Add(cmd);
                if (!string.IsNullOrEmpty(arg)) argsList.Add(arg);
                Helpers.ProcessRunner.Instance.RunInteractive("cmd.exe", argsList, env, workingDir: DeckPath);
            }
            else
            {
                argsList.Add(cmd);
                if (!string.IsNullOrEmpty(arg)) argsList.Add(arg);
                Helpers.ProcessRunner.Instance.RunInteractive("npm", argsList, env, workingDir: DeckPath);
            }

            PrintTunnelSummary();
            Console.WriteLine("Press any key to return...");
            SpectrePanel.SafeReadKey();
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"AntigravityDeckHelper.RunNpmCommand failed for '{cmd} {arg}'", ex);
            Console.WriteLine($"\nFailed to run npm command: {ex.Message}");
            Console.WriteLine("Press any key to return...");
            SpectrePanel.SafeReadKey();
        }
    }

    private static void PrintTunnelSummary()
    {
        try
        {
            var infoFile = Path.Combine(DeckPath, ".tunnel-info.txt");
            if (File.Exists(infoFile))
            {
                var lines = File.ReadAllLines(infoFile);
                Console.WriteLine("\n================================================================================");
                Console.WriteLine("                    🌌 ANTIGRAVITY DECK LINKS & STATUS 🌌                      ");
                Console.WriteLine("================================================================================");
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
                Console.WriteLine("================================================================================\n");
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError("Failed to print tunnel summary", ex);
        }
    }
}
