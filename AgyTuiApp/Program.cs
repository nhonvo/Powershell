using System;

using System.Buffers;

using System.Collections.Frozen;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Linq;

using System.Net;

using System.Net.Http;

using System.Net.NetworkInformation;

using System.Net.Sockets;

using System.Runtime.InteropServices;

using System.Security.AccessControl;

using System.Security.Cryptography;

using System.Text;

using System.Text.Json;

using System.Text.Json.Serialization;

using System.Text.RegularExpressions;

using System.Threading;

using Spectre.Console;

namespace AgyTui;

using AgyTui.Components;


public sealed record PaletteCommand(string Alias, string Description, string Category);

public static class CommandPalette
{
    public static readonly PaletteCommand[] Commands = AgyTui.Registry.CommandRegistry.All
        .Select(c => new PaletteCommand(c.Alias, c.Description, c.HelpCategory))
        .ToArray();

    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Command Palette[/]").RuleStyle("grey"));
        var categories = Commands.Select(c => c.Category).Distinct().ToArray();
        var catIdx = SpectreMenu.Show("Category", ["All", .. categories], 0, true);
        IEnumerable<PaletteCommand> filtered = catIdx <= 0 ? Commands : Commands.Where(c => c.Category == categories[catIdx - 1]);
        var items = filtered.Select(c => $"{c.Alias,-20} {c.Description}").ToArray();
        var cmds = filtered.Select(c => c.Alias).ToArray();
        var selected = SpectreMenu.Show(["Command Palette", "Select a command to view details"], items, cmds, 0, true, false);
        if (selected >= 0)
        {
            var cmd = filtered.ElementAt(selected);
            AnsiConsole.Write(new Panel($"[bold]Alias:[/] {cmd.Alias.EscapeMarkup()}\n" + $"[bold]Category:[/] {cmd.Category.EscapeMarkup()}\n" + $"[bold]Description:[/] {cmd.Description.EscapeMarkup()}")
            {
                Header = new PanelHeader("[bold cyan]Command Details[/]"),
                Border = BoxBorder.Rounded
            }
            );
        }

    }

}
public static class ProfileHelp
{
    private static readonly FrozenDictionary<string, string[]> HelpTopics = AgyTui.Registry.CommandRegistry.All
        .Where(c => c.HelpLines.Length > 0)
        .GroupBy(c => c.HelpCategory)
        .ToDictionary(
            g => g.Key,
            g => g.SelectMany(c => c.HelpLines).ToArray(),
            StringComparer.OrdinalIgnoreCase
        )
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static void Show()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Help Browser[/]").RuleStyle("grey"));
        var topics = HelpTopics.Keys.ToArray();
        var idx = SpectreMenu.Show("Help Topics", topics, 0, true);
        if (idx < 0) return;
        var topic = topics[idx];
        SpectrePager.Show($"Help: {topic}", HelpTopics[topic]);

    }

    public static Dictionary<string, Dictionary<string, CommandDoc[]>> GetCommands(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return new();

        try
        {
            var raw = File.ReadAllText(jsonPath);
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
            ;
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, CommandDoc[]>>>(raw, opts) ?? new();
        }
        catch
        {
            return new();
        }

    }

    public static CommandDoc? ShowInteractive(string jsonPath, string initialFilter)
    {
        var cmdsNested = GetCommands(jsonPath);
        var cmds = new Dictionary<string, CommandDoc[]>();
        foreach (var (_, subDict) in cmdsNested) foreach (var (sub, docs) in subDict) cmds[sub] = docs;
        var categories = cmds.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var allCommands = categories.SelectMany(c => cmds[c]).ToArray();
        var categoryLookup = new Dictionary<string, string>();
        foreach (var c in categories) categoryLookup[$"{c} ({cmds[c].Length} commands)"] = c;
        var commandLookup = new Dictionary<string, CommandDoc>();
        foreach (var c in allCommands) commandLookup[$"{c.Alias,-10} - {c.Desc}"] = c;
        string[] TopResolver(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return categories.Select(c => $"{c} ({cmds[c].Length} commands)").ToArray();
            return allCommands.Where(c => c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase) || c.Desc.Contains(filter, StringComparison.OrdinalIgnoreCase) || c.Command.Contains(filter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
        }
        var filter = initialFilter;
        while (true)
        {
            var selectedLabel = SpectreMenu.ShowDynamic("Select Help Category", TopResolver, 0, filter);
            filter = "";
            if (selectedLabel == null) return null;
            if (commandLookup.TryGetValue(selectedLabel, out var cmdObj)) return cmdObj;
            if (categoryLookup.TryGetValue(selectedLabel, out var catName))
            {
                var catCmds = cmds[catName];
                var subLookup = new Dictionary<string, CommandDoc>();
                foreach (var c in catCmds) subLookup[$"{c.Alias,-10} - {c.Desc}"] = c;
                string[] SubResolver(string subFilter) => catCmds.Where(c => string.IsNullOrWhiteSpace(subFilter) || c.Alias.Contains(subFilter, StringComparison.OrdinalIgnoreCase) || c.Desc.Contains(subFilter, StringComparison.OrdinalIgnoreCase) || c.Command.Contains(subFilter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
                while (true)
                {
                    var selectedSubLabel = SpectreMenu.ShowDynamic($"Category: {catName}", SubResolver, 0);
                    if (selectedSubLabel == null) break;
                    if (subLookup.TryGetValue(selectedSubLabel, out var subCmd)) return subCmd;
                }
            }
        }

    }

}

public sealed record CommandDoc(string Alias, string FullName, string Desc, string Command);

public static class AgyHeader
{
    public static void ShowSplash()
    {
        AnsiConsole.Clear();
        var splashW = Math.Min(65, Math.Max(50, Console.WindowWidth - 2));
        var sep = new string('=', splashW);
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.Write(new FigletText("AGY TUI").Centered().Color(Color.Green));
        AnsiConsole.Write(new Rule("[bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        var active = AgyAccountCore.GetActiveAccount();
        var stats = AgyAccountCore.GetAccountStats(active);
        var quota = AgyAccountCore.CalculateRollingQuotas(active);
        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadLeft(4));
        grid.AddRow($"[cyan]Active account[/] : [green bold]{active.EscapeMarkup()}[/]");
        grid.AddRow($"[cyan]Login status[/] : {(stats.TokenStatus == "Logged In" ? "[green]● Logged In[/]" : "[red]○ Not Logged In[/]")}");
        grid.AddRow($"[cyan]Weekly quota[/] : {AgyAccountCore.GetProgressBar(quota.RemainingWeekly).EscapeMarkup()}");
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        try
        {
            var w = WordOfDay.Pick();
            if (w != null) WordOfDay.Render(w);
        }
        catch
        {
        }
        try
        {
            StudyStreak.ShowPanel();
        }
        catch
        {
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim] Press Enter to continue[/]");
        Console.ReadKey(true);
        AnsiConsole.Clear();

    }

}
public static class CcNavigator
{
    public static void Run()
    {
        var root = MenuNodeBuilder.BuildTree();
        IMenuRenderer renderer = Config.Current.UiMode == "three-pane"
            ? new ThreePaneRenderer()
            : new FlatTreeRenderer();
        renderer.Run(root);
    }
}


public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            AgyTui.Registry.CommandRegistry.AssertSwitchCases();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            Environment.Exit(1);
        }

        if (args.Length > 0)
        {
            RunCommand(args[0]);
            return;
        }
        CcNavigator.Run();

        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        AnsiConsole.MarkupLine("[dim]Goodbye.[/]");

    }

    public static string? SelectTopicInteractive(string promptTitle)
    {
        var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
        var index = SpectreMenu.ShowWithEscape(promptTitle, topics, 0);
        if (index < 0) return null;
        if (index == topics.Length - 1)
        {
            Console.CursorVisible = true;
            var custom = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
            Console.CursorVisible = false;
            return string.IsNullOrEmpty(custom) ? null : custom;
        }
        return topics[index].Split(' ')[0];
    }

    public static void ShowHotkeysGuide()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
        table.Title("[bold cyan]🛸 PowerShell Profile Hotkeys Guide[/]");
        table.AddColumn(new TableColumn("[bold yellow]Domain / Category[/]"));
        table.AddColumn(new TableColumn("[bold green]Shortcut[/]"));
        table.AddColumn(new TableColumn("[bold]Command & Description[/]"));

        table.AddRow("📁 Workspace & Dev", "[bold green]cnav[/]", "/proj — Navigate registered workspace");
        table.AddRow("", "[green]ide[/]", "/ide — Launch terminal IDE session");
        table.AddRow("", "[green]ide-diff[/]", "/ide-diff — Git diff viewer");
        table.AddRow("", "[green]dbld[/]", "/dbld — [[.NET]] Build project");
        table.AddRow("", "[green]dtst[/]", "/dtst — [[.NET]] Test project");

        table.AddRow("🌿 Git Operations", "[bold green]cg[/]", "/gs — Git status summary & conventional commit");
        table.AddRow("", "[green]gcmt[/]", "/gcmt — Conventional commit wizard");
        table.AddRow("", "[green]git-undo[/]", "/git-undo — Soft-reset last local commit");
        table.AddRow("", "[green]nexus[/]", "/nexus — Git Nexus multi-repo dashboard");

        table.AddRow("🐳 Docker & DB", "[bold green]cdk[/]", "/dkcl — Docker cleanup TUI dashboard");
        table.AddRow("", "[green]dcup[/]", "/dcup — docker compose up -d");
        table.AddRow("", "[green]dcdown[/]", "/dcdown — docker compose down");
        table.AddRow("", "[green]db-tui[/]", "/db-tui — SQLite database browser");

        table.AddRow("☁ AWS & Cloud", "[bold green]caws[/]", "/aws-local — LocalStack sandbox diagnostics");

        table.AddRow("🌐 System & Network", "[bold green]cnet[/]", "/public-ip — Public IP address & network status");
        table.AddRow("", "[bold green]csys[/]", "/disk — Disk space & system health");
        table.AddRow("", "[bold green]cssh[/]", "/ssh-info — SSH connection info & QR generator");
        table.AddRow("", "[green]kill-port[/]", "/kill-port — Kill process by port number");

        table.AddRow("🤖 AI Assistants", "[bold green]cai[/]", "/claude — Launch Claude Code CLI");
        table.AddRow("", "[green]codex[/]", "/codex — Launch Codex CLI");
        table.AddRow("", "[green]openclaw[/]", "/openclaw — Launch OpenClaw via Ollama");
        table.AddRow("", "[green]ollama-status[/]", "/ollama-status — Check local Ollama daemon");

        table.AddRow("👤 AGY Accounts", "[green]agyswitch[/]", "/agyswitch — Switch active account context");
        table.AddRow("", "[green]agyquota[/]", "/agyquota — View quota usage across accounts");
        table.AddRow("", "[green]autoswitch[/]", "/autoswitch — Toggle automatic workspace account switch");

        table.AddRow("📚 Learn & Study", "[green]learn[/]", "/learn — Interactive learning topic browser");
        table.AddRow("", "[green]vocab[/]", "/vocab — English vocabulary drill");
        table.AddRow("", "[green]kana[/]", "/kana — Japanese Kana quiz");
        table.AddRow("", "[green]kanji[/]", "/kanji — Kanji lookup & stroke detail");
        table.AddRow("", "[green]algo[/]", "/algo — Algorithm visualizer");

        table.AddRow("📈 Track & Progress", "[green]session[/]", "/session — Start Pomodoro study session");
        table.AddRow("", "[green]stats[/]", "/stats — Weekly study statistics");
        table.AddRow("", "[green]streak[/]", "/streak — Study streak display");

        table.AddRow("🎨 Theme & Settings", "[green]cc[/]", "/cc — Open Command Palette");
        table.AddRow("", "[green]theme[/]", "/theme — Select Shell theme");
        table.AddRow("", "[green]ui-mode[/]", "/ui-mode — Toggle three-pane / flat-tree layout");
        table.AddRow("", "[green]mobile-setup[/]", "/mobile-setup — Toggle mobile setup mode");

        AnsiConsole.Write(table);
    }

    public static void RunCommand(string alias)
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        var lAlias = alias.ToLowerInvariant();
        if ((lAlias == "claude" || lAlias == "codex" || lAlias == "openclaw" || lAlias == "hermes" || lAlias == "hermesd" || lAlias == "claude-cloud" || lAlias == "claude-ollama" || lAlias == "codex-cloud" || lAlias == "codex-ollama") && !AgyAiCore.IsAiOllamaEnabled())
        {
            SpectrePanel.Error("AI/Ollama features are disabled in config.");
            Thread.Sleep(1500);
            return;
        }
        if ((lAlias == "agyswitch" || lAlias == "agyquota" || lAlias == "account-tree" || lAlias == "quota-chart" || lAlias == "live-dashboard" || lAlias == "autoswitch") && !AgyAiCore.IsAgyEnabled())
        {
            SpectrePanel.Error("AGY Account features are disabled in config.");
            Thread.Sleep(1500);
            return;
        }

        try
        {
            switch (alias.ToLowerInvariant())
            {
                case "proj":
                case "prj":
                case "p":
                    var projPath = ProfileNavigator.Navigate("");
                    if (!string.IsNullOrEmpty(projPath))
                    {
                        AnsiConsole.MarkupLine($"Navigate target: [green]{projPath}[/]");
                    }
                    break;
                case "f":
                    SystemHelper.OpenExplorer();
                    break;
                case "gs":
                    GitHelper.ShowStatus();
                    break;
                case "ga":
                    GitHelper.AddAll();
                    break;
                case "gbr":
                case "gb":
                    GitHelper.ShowBranches();
                    break;
                case "gcmt":
                    GitHelper.ConventionalCommitWizard();
                    break;
                case "glog":
                case "glo":
                case "glg":
                    GitHelper.ShowLog();
                    break;
                case "gpull":
                case "gpu":
                    GitHelper.Pull();
                    break;
                case "gpush":
                case "gus":
                    GitHelper.Push();
                    break;
                case "gf":
                    GitHelper.Fetch();
                    break;
                case "gd":
                    GitDiffViewer.ShowDiff(Directory.GetCurrentDirectory());
                    break;
                case "git-undo":
                case "gundo":
                    GitHelper.InvokeGitUndo();
                    break;
                case "dbld":
                case "db":
                    DotNetHelper.Build();
                    break;
                case "dr":
                    DotNetHelper.Run();
                    break;
                case "dtst":
                case "dt":
                    DotNetHelper.Test();
                    break;
                case "df":
                    DotNetHelper.Format();
                    break;
                case "dcl":
                    DotNetHelper.Clean();
                    break;
                case "drestore":
                case "dres":
                    DotNetHelper.Restore();
                    break;
                case "dpublish":
                    DotNetHelper.Publish();
                    break;
                case "dwatch":
                case "dw":
                    DotNetHelper.Watch();
                    break;
                case "clean-build":
                case "dclean":
                    DotNetHelper.RemoveBinObj(Directory.GetCurrentDirectory());
                    break;
                case "add-migration":
                case "da":
                    var migName = AnsiConsole.Ask<string>("Migration name:");
                    DotNetHelper.AddMigration(migName);
                    break;
                case "update-db":
                case "du":
                    DotNetHelper.UpdateDatabase();
                    break;
                case "docker-health":
                    DockerHelper.ShowDockerHealthDashboard();
                    break;
                case "dkcl":
                    DockerHelper.ShowCleanupDashboard();
                    break;
                case "dkrmac":
                    DockerHelper.RemoveAllContainers();
                    break;
                case "dkstac":
                    DockerHelper.StopAllContainers();
                    break;
                case "dimg":
                    DockerHelper.ShowImages();
                    break;
                case "dlogs":
                    DockerHelper.ShowContainerLogs();
                    break;
                case "dcup":
                case "dkcpu":
                    DockerHelper.ComposeUp();
                    break;
                case "dcdown":
                case "dkcpd":
                    DockerHelper.ComposeDown();
                    break;
                case "aws-whoami":
                    AwsHelper.ShowCallerIdentity();
                    break;
                case "aws-local":
                    AwsHelper.ShowLocalStackInfo();
                    break;
                case "aws-s3":
                    AwsHelper.ShowS3Buckets();
                    break;
                case "aws-sqs":
                    AwsHelper.ShowSQSQueues();
                    break;
                case "aws-ssm":
                    AwsHelper.ShowSsmParameters();
                    break;
                case "aws-sns":
                    AwsHelper.ShowSnsTopics();
                    break;
                case "aws-dynamodb":
                    AwsHelper.ShowDynamoDbTables();
                    break;
                case "aws-lambda":
                    AwsHelper.ShowLambdaFunctions();
                    break;
                case "rebuild":
                    AnsiConsole.MarkupLine("[cyan]Rebuilding Control Center TUI binary...[/]");
                    var projFile = Path.Combine(Directory.GetCurrentDirectory(), "AgyTuiApp", "AgyTuiApp.csproj");
                    if (!File.Exists(projFile)) projFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AgyTuiApp.csproj");
                    var buildExit = DotNetHelper.Build(File.Exists(projFile) ? projFile : null);
                    if (buildExit == 0) SpectrePanel.Success("Control Center TUI recompiled successfully!");
                    else SpectrePanel.Warning("Build note: If running directly inside AgyTuiApp.exe, Windows locks the executable while in-use. Exit TUI and run 'dbld' or run via PowerShell wrapper to refresh binary.");
                    break;
                case "claude":
                    AgyAiCore.InvokeClaude([]);
                    break;
                case "claude-cloud":
                    AgyAiCore.InvokeClaude([], "cloud");
                    break;
                case "claude-ollama":
                    AgyAiCore.InvokeClaude([], "local");
                    break;
                case "codex":
                    AgyAiCore.InvokeCodex([]);
                    break;
                case "codex-cloud":
                    AgyAiCore.InvokeCodex([], "cloud");
                    break;
                case "codex-ollama":
                    AgyAiCore.InvokeCodex([], "local");
                    break;
                case "openclaw":
                    AgyAiCore.InvokeOpenClaw([]);
                    break;
                case "ollama-models":
                    OllamaHelper.ManageOllamaModels();
                    break;
                case "ollama-pull":
                    OllamaHelper.PullOllamaModel();
                    break;
                case "ollama-start":
                    OllamaHelper.StartOllamaDaemon();
                    break;
                case "ollama-logs":
                    OllamaHelper.ShowOllamaLogs();
                    break;
                case "ollama-benchmark":
                    OllamaHelper.BenchmarkOllamaModels();
                    break;
                case "ollama-status":
                    OllamaStatusWidgetCache.Invalidate();
                    break;
                case "deck-status":
                    {
                        var running = AgyAiCore.IsDeckRunning();
                        var statusStr = running ? "[green]Online (port 3000)[/]" : "[red]Offline[/]";
                        AnsiConsole.MarkupLine($"Antigravity Deck Status: {statusStr}");
                        if (running)
                        {
                            AnsiConsole.MarkupLine("Local App URL: [cyan]http://127.0.0.1:3000[/]");
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey(true);
                    }
                    break;
                case "deck-setup":
                    AntigravityDeckHelper.Setup();
                    break;
                case "deck-start":
                    AntigravityDeckHelper.StartLocal();
                    break;
                case "deck-online":
                    AntigravityDeckHelper.StartOnline();
                    break;
                case "agy-cli":
                    if (!AgyAiCore.IsAgyEnabled())
                    {
                        AgyAiCore.InvokeClaude([]);
                        break;
                    }
                    Helpers.ProcessRunner.Run("cmd.exe", "/c agy");
                    break;
                case "ai-history":
                    {
                        var logPath = Path.Combine(AgyAccountCore.AgySourceHome, "ai_activity_log.jsonl");
                        if (!File.Exists(logPath))
                        {
                            AnsiConsole.MarkupLine("[yellow]No AI activity log found yet.[/]");
                            Console.WriteLine("\nPress any key to return...");
                            Console.ReadKey(true);
                            break;
                        }
                        var lines = File.ReadAllLines(logPath);
                        var table = new Table().Border(TableBorder.Rounded);
                        table.AddColumn("Timestamp");
                        table.AddColumn("Agent");
                        table.AddColumn("Mode");
                        table.AddColumn("Duration (s)");
                        table.AddColumn("Status");
                        table.AddColumn("Account");

                        foreach (var line in lines.TakeLast(30))
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            try
                            {
                                using var doc = JsonDocument.Parse(line);
                                var root = doc.RootElement;
                                var ts = root.GetProperty("Timestamp").GetString() ?? "";
                                if (ts.Length > 19) ts = ts[..19].Replace("T", " ");
                                var agent = root.GetProperty("Agent").GetString() ?? "";
                                var modeVal = root.GetProperty("Mode").GetString() ?? "";
                                var dur = root.GetProperty("DurationMs").GetDouble() / 1000.0;
                                var status = root.GetProperty("Success").GetBoolean() ? "[green]Success[/]" : "[red]Failed[/]";
                                var acc = root.GetProperty("Account").GetString() ?? "";
                                table.AddRow(ts, agent, modeVal, dur.ToString("F2"), status, acc);
                            }
                            catch { }
                        }
                        AnsiConsole.Write(table);
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey(true);
                    }
                    break;
                case "hermes":
                    if (AgyAiCore.InvokeHermes([]) == AgyAiCore.HermesResult.NotInstalled)
                    {
                        SpectrePanel.Warning("Hermes CLI is not installed on PATH.");
                        var choice = SpectreMenu.Show("Hermes Action Fallback", ["Launch local Ollama chat with default model", "View Hermes setup guide"], 0);
                        if (choice == 0)
                        {
                            AgyAiCore.InvokeOllamaNative(null);
                        }
                        else if (choice == 1)
                        {
                            AnsiConsole.MarkupLine("[cyan]To install Nous Hermes Agent:[/] pip install hermes-agent (or npm install -g @nous/hermes)");
                        }
                    }
                    break;
                case "hermesd":
                    if (AgyAiCore.InvokeHermesDesktop([]) == AgyAiCore.HermesResult.NotInstalled)
                    {
                        SpectrePanel.Warning("Hermes Desktop is not installed on PATH.");
                        var choice = SpectreMenu.Show("Hermes Desktop Fallback", ["Launch local Ollama chat with default model", "View Hermes Desktop setup guide"], 0);
                        if (choice == 0)
                        {
                            AgyAiCore.InvokeOllamaNative(null);
                        }
                        else if (choice == 1)
                        {
                            AnsiConsole.MarkupLine("[cyan]To install Hermes Desktop:[/] Download installer from https://github.com/nousresearch/hermes-desktop");
                        }
                    }
                    break;
                case "tailscale-status":
                    SshHelper.ShowTailscaleStatus();
                    break;
                case "ssh-qr":
                    SshHelper.ShowSshQrCode();
                    break;
                case "disk":
                case "usage":
                    SystemHelper.ShowDiskSpace();
                    break;
                case "public-ip":
                case "myip":
                    AnsiConsole.MarkupLine($"Public IP: [green]{SystemHelper.GetPublicIP()}[/]");
                    break;
                case "kill-port":
                    var portStr = AnsiConsole.Ask<string>("Port number:");
                    if (int.TryParse(portStr, out var port)) SystemHelper.KillPort(port);
                    break;
                case "ssh-info":
                    SshHelper.ShowSshInfo();
                    break;
                case "db-tui":
                    var dbPath = AnsiConsole.Ask<string>("SQLite DB path:");
                    DatabaseHelper.ShowDatabaseTui(dbPath);
                    break;
                case "agyswitch":
                    var accs = AgyAccountCore.GetAccounts();
                    var activeAcc = AgyAccountCore.GetActiveAccount();
                    var accItems = accs.Select(a => a == activeAcc ? $"{a} (Active)" : a).ToArray();
                    var defaultIdx = Array.IndexOf(accs, activeAcc);
                    if (defaultIdx < 0) defaultIdx = 0;
                    var accIdx = SpectreMenu.ShowWithEscape("Select Account to Switch", accItems, defaultIdx);
                    if (accIdx >= 0)
                    {
                        var targetAcc = accs[accIdx];
                        AgyAccountCore.SetActiveAccount(targetAcc, false);
                        Thread.Sleep(1000);
                    }
                    break;
                case "agyquota":
                    AgyAccountCore.ShowAllAccountsSummary();
                    break;
                case "account-tree":
                    AgyAccountDisplay.ShowAccountTree();
                    break;
                case "quota-chart":
                    AgyAccountDisplay.ShowQuotaChart(AgyAccountCore.GetActiveAccount());
                    break;
                case "live-dashboard":
                    SpectreTable.Live(["Account", "Login", "Quota W", "Quota 5h", "Last Used"], () => AgyAccountCore.GetAccounts().Select(a =>
                {
                    var s = AgyAccountCore.GetAccountStats(a);
                    var act = AgyAccountCore.GetActiveAccount();
                    var n = a == act ? $"[green bold]* {a}[/]" : a;
                    var st = s.TokenStatus == "Logged In" ? "[green]●[/]" : "[red]○[/]";
                    var lu = s.LastUsed.Length >= 10 && s.LastUsed != "Never" ? s.LastUsed[..10] : "Never";
                    return new[]
                    {
                        n, st,$"{(int)Math.Round(s.GeminiWeekly)}%",$"{(int)Math.Round(s.GeminiFiveHour)}%", lu
                    }
                    ;
                }
                ).ToArray(), 5000);
                    break;
                case "autoswitch":
                    AgyAccountCore.ToggleAutoSwitch();
                    break;
                case "no-auto-commit":
                case "autocommit":
                    AgyAccountCore.ToggleNoAutoCommit();
                    break;
                case "scaffold":
                    ProjectScaffolder.Scaffold();
                    break;
                case "help":
                    ProfileHelp.Show();
                    break;
                case "mobile-setup":
                case "mobile":
                    {
                        ThemeHelper.ToggleMobileMode();
                        var currDensity = Config.GetDensity();
                        var newDensity = currDensity == "compact" ? "comfortable" : "compact";
                        Config.SetDensity(newDensity);
                        SpectrePanel.Success($"Mobile setup toggled: Prompt Mobile Mode = {ThemeHelper.IsMobileModeActive()}, TUI Density = {newDensity}");
                    }
                    break;
                case "theme":
                    {
                        var tPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
                        if (string.IsNullOrEmpty(tPath))
                        {
                            tPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
                            if (!Directory.Exists(tPath))
                            {
                                tPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
                            }
                        }
                        var currTheme = Environment.GetEnvironmentVariable("THEME");
                        var newThemePath = ThemeHelper.SelectThemeInteractive(tPath, currTheme);
                        if (!string.IsNullOrEmpty(newThemePath))
                        {
                            var selThemeFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_theme.txt");
                            File.WriteAllText(selThemeFile, newThemePath);
                        }
                    }
                    break;
                case "cc":
                    CommandPalette.Show();
                    break;
                case "ui-mode":
                    {
                        var currentMode = Config.Current.UiMode;
                        var nextMode = currentMode == "three-pane" ? "flat-tree" : "three-pane";
                        Config.SetUiMode(nextMode);
                        AnsiConsole.MarkupLine($"[green]UI Mode toggled to '{nextMode}'. Switch will apply next time you launch Control Center.[/]");
                        Thread.Sleep(1500);
                    }
                    break;
                case "density":
                    {
                        var currentDensity = Config.Current.Density;
                        var nextDensity = currentDensity == "comfortable" ? "compact" : "comfortable";
                        Config.SetDensity(nextDensity);
                        AnsiConsole.MarkupLine($"[green]UI Density toggled to '{nextDensity}'. Switch will apply next time you launch Control Center.[/]");
                        Thread.Sleep(1500);
                    }
                    break;
                case "hotkeys":
                case "hotkey":
                    ShowHotkeysGuide();
                    break;
                case "learn":
                    var learnTopic = SelectTopicInteractive("Select Topic to Learn");
                    if (!string.IsNullOrEmpty(learnTopic)) LearnRouter.StartLearning(learnTopic);
                    break;
                case "flashcard":
                    FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                    break;
                case "vocab":
                    VocabDrill.Run("Intermediate");
                    break;
                case "kana":
                    KanaQuiz.Run("hiragana");
                    break;
                case "kanji":
                    KanjiLookup.Run();
                    break;
                case "jlpt":
                    JlptVocabDrill.Run("N5");
                    break;
                case "algo":
                    AlgoVisualizer.PickAndRun();
                    break;
                case "complexity":
                    ComplexitySheet.Run();
                    break;
                case "problems":
                    ProblemTracker.Run();
                    break;
                case "snippets":
                    SnippetLibrary.Run();
                    break;
                case "sheets":
                    CheatSheetBrowser.Run();
                    break;
                case "quiz":
                    CsharpQuiz.Run();
                    break;
                case "interview":
                    InterviewBank.Run();
                    break;
                case "star":
                    StarBuilder.Run();
                    break;
                case "mock":
                    MockInterviewTimer.Run();
                    break;
                case "word-of-day":
                    var word = WordOfDay.Pick();
                    if (word != null) WordOfDay.Render(word);

                    else SpectrePanel.Warning("No word of the day available.");
                    break;
                case "session":
                    var sessionTopic = SelectTopicInteractive("Select Topic for Study Session");
                    if (!string.IsNullOrEmpty(sessionTopic)) StudySession.Run(sessionTopic);
                    break;
                case "stats":
                    StudyStats.Run();
                    break;
                case "goals":
                    DailyGoals.Show();
                    break;
                case "streak":
                    StudyStreak.ShowPanel();
                    break;
                case "due":
                    LearnDataPaths.EnsureDirectories();
                    int dueCount = 0;
                    if (Directory.Exists(LearnDataPaths.DecksDir))
                    {
                        foreach (var deckFile in Directory.GetFiles(LearnDataPaths.DecksDir, "*.json"))
                        {
                            var deck = LearnDataPaths.LoadJson<DeckFile>(deckFile);
                            if (deck != null)
                            {
                                dueCount += deck.Cards.Count(c => SpacedRepetitionEngine.IsDueToday(c.Sr));
                            }
                        }
                    }
                    AnsiConsole.MarkupLine($"Due spaced repetition reviews today: [yellow]{dueCount}[/]");
                    break;
                case "progress":
                    ProgressDashboard.Show();
                    break;
                case "weak":
                    var weakTopic = SelectTopicInteractive("Select Topic for Weak Items");
                    if (!string.IsNullOrEmpty(weakTopic)) WeakItemsQueue.ShowPreSessionReview(weakTopic);
                    break;
                case "obsidian":
                    ObsidianBridge.Run();
                    break;
                case "obs-graph":
                    var cfg = ObsidianBridge.LoadConfig();
                    if (cfg != null && Directory.Exists(cfg.VaultPath)) ObsidianGraph.Run(cfg.VaultPath);

                    else SpectrePanel.Warning("Obsidian vault path not configured. Run 'obsidian' first.");
                    break;
                case "nexus":
                case "repo-graph":
                    RepoGraph.Show(RepoGraph.Build());
                    break;
                case "nexus-stats":
                    GitNexusStats.Run();
                    break;
                case "ide":
                    TerminalIde.Open();
                    break;
                case "ide-diff":
                    GitDiffViewer.ShowDiff(Directory.GetCurrentDirectory());
                    break;
                case "ide-search":
                    SymbolSearch.BrowseWorkspaceSymbols(Directory.GetCurrentDirectory());
                    break;
                case "refresh":
                    LearnRouter.RefreshData("all");
                    break;
                case "add-resource":
                    var path = AnsiConsole.Ask<string>("Resource path/URL:");
                    var tags = AnsiConsole.Ask<string>("Tags (comma separated):").Split(',').Select(t => t.Trim()).ToArray();
                    ResourceRegistry.AddResource(path, tags);
                    SpectrePanel.Success("Resource registered.");
                    break;
                default:
                    SpectrePanel.Warning($"Command alias '{alias}' is not implemented for direct TUI routing.");
                    break;
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Error running command: {ex.Message}");
        }
        AnsiConsole.WriteLine();
        if (!Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[dim]Press any key to return to menu...[/]");

            try
            {
                Console.ReadKey(true);
            }
            catch
            {
            }
        }

    }

}