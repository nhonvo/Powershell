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


public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch { }

        try
        {
            AgyTui.Registry.CommandRegistry.AssertSwitchCases();
            AgyTui.Registry.CommandRegistry.AssertAllAliasesReachable(MenuNodeBuilder.BuildTree());
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            Environment.Exit(1);
        }

        if (args.Length > 0)
        {
            RunCommand(args[0], args.Skip(1).ToArray());
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



    public static void RunCommand(string alias, string[]? args = null)
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        var lAlias = alias.ToLowerInvariant();
        var cmdEntry = Registry.CommandRegistry.GetByAlias(lAlias);
        if (cmdEntry != null)
        {
            if (cmdEntry.RequiresAiOllama && !AgyAiCore.IsAiOllamaEnabled())
            {
                SpectrePanel.Error("AI/Ollama features are disabled in config.");
                Thread.Sleep(1500);
                return;
            }
            if (cmdEntry.RequiresAgy && !AgyAiCore.IsAgyEnabled())
            {
                SpectrePanel.Error("AGY Account features are disabled in config.");
                Thread.Sleep(1500);
                return;
            }
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
                    AgyServices.Git.ShowStatus();
                    break;
                case "ga":
                    AgyServices.Git.AddAll();
                    break;
                case "gbr":
                case "gb":
                    AgyServices.Git.ShowBranches();
                    break;
                case "gcmt":
                    AgyServices.Git.ConventionalCommitWizard();
                    break;
                case "glog":
                case "glo":
                case "glg":
                    AgyServices.Git.ShowLog();
                    break;
                case "gpull":
                case "gpu":
                    AgyServices.Git.Pull();
                    break;
                case "gpush":
                case "gus":
                    AgyServices.Git.Push();
                    break;
                case "gf":
                    AgyServices.Git.Fetch();
                    break;
                case "gd":
                    GitDiffViewer.ShowDiff(Directory.GetCurrentDirectory());
                    break;
                case "git-undo":
                case "gundo":
                    AgyServices.Git.InvokeGitUndo();
                    break;
                case "dbld":
                case "db":
                    AgyServices.DotNet.Build();
                    break;
                case "dr":
                    AgyServices.DotNet.Run();
                    break;
                case "dtst":
                case "dt":
                    AgyServices.DotNet.Test();
                    break;
                case "df":
                    AgyServices.DotNet.Format();
                    break;
                case "dcl":
                    AgyServices.DotNet.Clean();
                    break;
                case "drestore":
                case "dres":
                    AgyServices.DotNet.Restore();
                    break;
                case "dpublish":
                    AgyServices.DotNet.Publish();
                    break;
                case "dpack":
                    AgyServices.DotNet.Pack();
                    break;
                case "dpubpkg":
                    AgyServices.DotNet.PublishPackage();
                    break;
                case "open-term":
                case "term":
                case "wt":
                    SystemHelper.OpenNewTerminalSession();
                    break;
                case "go":
                    var goTargetPath = ProfileNavigator.Navigate("");
                    if (!string.IsNullOrEmpty(goTargetPath))
                    {
                        AnsiConsole.MarkupLine($"Navigate target: [green]{goTargetPath}[/]");
                    }
                    break;
                case "dwatch":
                case "dw":
                    AgyServices.DotNet.Watch();
                    break;
                case "clean-build":
                case "dclean":
                    AgyServices.DotNet.RemoveBinObj(Directory.GetCurrentDirectory());
                    break;
                case "add-migration":
                case "da":
                    var migName = AnsiConsole.Ask<string>("Migration name:");
                    AgyServices.DotNet.AddMigration(migName);
                    break;
                case "update-db":
                case "du":
                    AgyServices.DotNet.UpdateDatabase();
                    break;
                case "docker-health":
                    AgyServices.Docker.ShowDockerHealthDashboard();
                    break;
                case "dkcl":
                    AgyServices.Docker.ShowCleanupDashboard();
                    break;
                case "dkrmac":
                    AgyServices.Docker.RemoveAllContainers();
                    break;
                case "dkstac":
                    AgyServices.Docker.StopAllContainers();
                    break;
                case "dimg":
                    AgyServices.Docker.ShowImages();
                    break;
                case "dlogs":
                    AgyServices.Docker.ShowContainerLogs();
                    break;
                case "dcup":
                case "dkcpu":
                    AgyServices.Docker.ComposeUp();
                    break;
                case "dcdown":
                case "dkcpd":
                    AgyServices.Docker.ComposeDown();
                    break;
                case "aws-whoami":
                    AgyServices.Aws.ShowCallerIdentity();
                    break;
                case "aws-local":
                    AgyServices.Aws.ShowLocalStackInfo();
                    break;
                case "aws-s3":
                    AgyServices.Aws.ShowS3Buckets();
                    break;
                case "aws-sqs":
                    AgyServices.Aws.ShowSQSQueues();
                    break;
                case "aws-ssm":
                    AgyServices.Aws.ShowSsmParameters();
                    break;
                case "aws-sns":
                    AgyServices.Aws.ShowSnsTopics();
                    break;
                case "aws-dynamodb":
                    AgyServices.Aws.ShowDynamoDbTables();
                    break;
                case "aws-lambda":
                    AgyServices.Aws.ShowLambdaFunctions();
                    break;
                case "rebuild":
                    AnsiConsole.MarkupLine("[cyan]Rebuilding Control Center TUI binary...[/]");
                    var projFile = Path.Combine(Directory.GetCurrentDirectory(), "AgyTuiApp", "AgyTuiApp.csproj");
                    if (!File.Exists(projFile)) projFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AgyTuiApp.csproj");
                    var buildExit = AgyServices.DotNet.Build(File.Exists(projFile) ? projFile : null);
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
                case "desk-status":
                case "deck-status":
                    {
                        var running = AgyAiCore.IsDeckRunning();
                        var statusStr = running ? "[green]Online (port 3000)[/]" : "[red]Offline[/]";
                        AnsiConsole.MarkupLine($"Antigravity Deck/Desk Status: {statusStr}");
                        if (running)
                        {
                            AnsiConsole.MarkupLine("Local App URL: [cyan]http://127.0.0.1:3000[/]");
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey(true);
                    }
                    break;
                case "desk-setup":
                case "deck-setup":
                    AntigravityDeckHelper.Setup();
                    break;
                case "desk-start":
                case "deck-start":
                    AntigravityDeckHelper.StartLocal();
                    break;
                case "desk-online":
                case "deck-online":
                    AntigravityDeckHelper.StartOnline();
                    break;

                // Antigravity Manager
                case "mgr-status":
                case "manager-status":
                case "agm-status":
                    {
                        var running = AgyAiCore.IsManagerRunning();
                        var statusStr = running ? "[green]Online (port 8045)[/]" : "[red]Offline[/]";
                        AnsiConsole.MarkupLine($"Antigravity Manager Status: {statusStr}");
                        if (running)
                        {
                            AnsiConsole.MarkupLine("Local Backend URL: [cyan]http://127.0.0.1:8045[/]");
                        }
                        Console.WriteLine("\nPress any key to return...");
                        Console.ReadKey(true);
                    }
                    break;
                case "mgr-setup":
                case "manager-setup":
                case "agm-setup":
                    Projects.SetupManager();
                    break;
                case "mgr":
                case "mgr-start":
                case "manager-start":
                case "agm":
                case "agm-start":
                    Projects.StartManager();
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
                case "layout":
                case "view":
                    {
                        var targetMode = (args != null && args.Length > 0) ? args[0].ToLowerInvariant() : null;
                        string nextMode;
                        if (targetMode == "flat-tree" || targetMode == "flat" || targetMode == "tree")
                        {
                            nextMode = "flat-tree";
                        }
                        else if (targetMode == "three-pane" || targetMode == "three" || targetMode == "pane")
                        {
                            nextMode = "three-pane";
                        }
                        else
                        {
                            var currentMode = Config.Current.UiMode;
                            nextMode = string.Equals(currentMode, "three-pane", StringComparison.OrdinalIgnoreCase) ? "flat-tree" : "three-pane";
                        }
                        Config.SetUiMode(nextMode);
                        AnsiConsole.MarkupLine($"[green bold]UI Mode updated to '{nextMode}'. Default view is flat-tree.[/]");
                        Thread.Sleep(1200);
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
                    HotkeysGuide.Show();
                    break;
                case "learn":
                    GuidedLearnFlow.Run();
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
                case "grammar":
                    var levelChoice = SpectreMenu.Show("Select Grammar Level", new[] { "N5 (Japanese)", "N4 (Japanese)", "N3 (Japanese)", "English" }, 0);
                    if (levelChoice == 0) GrammarQuiz.Run("N5");
                    else if (levelChoice == 1) GrammarQuiz.Run("N4");
                    else if (levelChoice == 2) GrammarQuiz.Run("N3");
                    else if (levelChoice == 3) GrammarQuiz.Run("English");
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
                case "learn-gen":
                case "ai-gen":
                case "deck-gen":
                    AiLearningGenerator.RunGenerator();
                    break;
                case "obsidian":
                case "vault":
                    ObsidianBridge.Run();
                    break;
                case "sync":
                    LearnRouter.RefreshData("all");
                    break;
                case "vault-open":
                    var openCfg = ObsidianBridge.LoadConfig();
                    var defaultOpenVault = System.IO.Path.Combine(LearnDataPaths.BaseDirectory, "learn");
                    var targetPath = openCfg?.VaultPath ?? (Directory.Exists(defaultOpenVault) ? defaultOpenVault : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "project", "learning"));
                    if (Directory.Exists(targetPath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = targetPath,
                            UseShellExecute = true
                        });
                        SpectrePanel.Success($"Opened vault directory: {targetPath}");
                    }
                    else
                    {
                        SpectrePanel.Warning($"Vault directory not found: {targetPath}");
                    }
                    break;
                case "obs-graph":
                    var cfg = ObsidianBridge.LoadConfig();
                    if (cfg != null && Directory.Exists(cfg.VaultPath)) ObsidianGraph.Run(cfg.VaultPath);

                    else SpectrePanel.Warning("Obsidian vault path not configured. Run 'obsidian' first.");
                    break;
                case "nexus":
                    GitNexus.ShowLiveDashboard();
                    break;
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