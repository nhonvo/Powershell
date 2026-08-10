using System.Text.Json;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using Microsoft.Extensions.DependencyInjection;
namespace AgyTui.UI.Core.Navigation;

public class CommandRouter : ICommandRouter
{
    private readonly IOllamaClient _ollama;
    private readonly IClaudeClient _claude;
    private readonly IHermesClient _hermes;
    private readonly IOpenClawClient _openClaw;
    private readonly IAwsClient _aws;
    private readonly IDockerClient _docker;
    private readonly IDotNetClient _dotNet;
    private readonly IGitClient _git;
    private readonly IAiLearningGenerator _learningGenerator;
    private readonly IAgyAccountStore? _accountStore;
    private readonly IAgyQuotaEngine? _quotaEngine;
    private readonly IThemeManager? _themeManager;
    private readonly IObsidianBridge? _obsidianBridge;
    private readonly IAiProcessRunner? _processRunner;

    public CommandRouter(
        IOllamaClient ollama,
        IClaudeClient claude,
        IHermesClient hermes,
        IOpenClawClient openClaw,
        IAwsClient aws,
        IDockerClient docker,
        IDotNetClient dotNet,
        IGitClient git,
        IAiLearningGenerator learningGenerator,
        IAgyAccountStore? accountStore = null,
        IAgyQuotaEngine? quotaEngine = null,
        IThemeManager? themeManager = null,
        IObsidianBridge? obsidianBridge = null,
        IAiProcessRunner? processRunner = null)
    {
        _ollama = ollama;
        _claude = claude;
        _hermes = hermes;
        _openClaw = openClaw;
        _aws = aws;
        _docker = docker;
        _dotNet = dotNet;
        _git = git;
        _learningGenerator = learningGenerator;
        _accountStore = accountStore ?? new Infrastructure.Integrations.AgyClient.AgyAccountStore();
        _quotaEngine = quotaEngine ?? new Infrastructure.Integrations.AgyClient.AgyQuotaEngine(_accountStore);
        _themeManager = themeManager ?? new Infrastructure.Common.ThemeManager();
        _obsidianBridge = obsidianBridge ?? new Infrastructure.Integrations.Obsidian.ObsidianBridge();
        _processRunner = processRunner ?? new Infrastructure.Integrations.Ai.Services.AiProcessRunner(_accountStore);
    }

    public static int Route(string alias, params string[]? args)
    {
        var router = Bootstrapper.ServiceProvider?.GetService<ICommandRouter>();
        return router != null ? router.Execute(alias, args) : 0;
    }

    public static int Route(string alias, object? rawArgs)
    {
        string[]? args = rawArgs switch
        {
            null => null,
            string s => [s],
            string[] sa => sa,
            System.Collections.IEnumerable ie => ie.Cast<object>().Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToArray(),
            _ => [rawArgs.ToString() ?? ""]
        };
        return Route(alias, args);
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

    public int Execute(string alias, string[]? args = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool success = true;
        string? errorType = null;
        int exitCode = 0;

        try
        {
            try
            {
                AnsiConsole.Clear();
            }
            catch
            {
            }
            var lAlias = (alias ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(lAlias))
            {
                lAlias = "cc";
            }
            var cmdEntry = CommandRegistry.GetByAlias(lAlias);
            if (cmdEntry != null)
            {
                if (cmdEntry.RequiresAiOllama && !Config.Current.Ai.EnableOllama)
                {
                    SpectrePanel.Error("AI/Ollama features are disabled in config.");
                    Thread.Sleep(1500);
                    return 1;
                }
                if (cmdEntry.RequiresAgy && !Config.Current.Ai.EnableAgy)
                {
                    SpectrePanel.Error("AGY Account features are disabled in config.");
                    Thread.Sleep(1500);
                    return 1;
                }
            }

            try
            {
                switch (lAlias)
                {
                    case "ai":
                    case "cai":
                        var aiQuery = args != null && args.Length > 0 ? string.Join(" ", args) : null;
                        if (!string.IsNullOrWhiteSpace(aiQuery))
                        {
                            _claude.InvokeClaude(args!);
                        }
                        else
                        {
                            SubPageNavigator.Run("ask-ai");
                        }
                        break;
                    case "ai-mode-check":
                        var targetAlias = args != null && args.Length > 0 ? args[0] : "claude";
                        AiDashboardView.ShowAiModeCheck(targetAlias);
                        break;
                    case "proj":
                    case "prj":
                    case "p":
                    case "cnav":
                        var q = args != null && args.Length > 0 ? string.Join(" ", args) : "";
                        SubPageNavigator.Run("proj", q);
                        break;
                    case "f":
                        SystemHelper.Instance.OpenExplorer();
                        break;
                    case "gs":
                        _git.ShowStatusNative(args);
                        break;
                    case "gsu":
                    case "gsi":
                    case "+gs":
                        _git.ShowStatus();
                        break;
                    case "ga":
                        _git.AddAll();
                        break;
                    case "gbr":
                    case "gb":
                        _git.ShowBranches();
                        break;
                    case "gbu":
                    case "gbi":
                    case "+gb":
                        _git.ShowBranches();
                        break;
                    case "glo":
                    case "glg":
                        _git.ShowLogGraph();
                        break;
                    case "glou":
                    case "gloi":
                    case "+glo":
                        _git.ShowLog();
                        break;
                    case "gpull":
                    case "gpu":
                        _git.Pull();
                        break;
                    case "gpush":
                    case "gus":
                        _git.Push();
                        break;
                    case "gf":
                        _git.Fetch();
                        break;
                    case "gd":
                        _git.ShowDiff();
                        break;
                    case "glog":
                        _git.ShowLogPretty();
                        break;
                    case "cob":
                        _git.NewBranch(args != null && args.Length > 0 ? args[0] : null);
                        break;
                    case "gbd":
                        _git.RemoveBranch(args != null && args.Length > 0 ? args[0] : null);
                        break;
                    case "gunstage":
                        _git.UnstageAll();
                        break;
                    case "gca":
                        _git.CommitAmend(args);
                        break;
                    case "gr":
                        _git.ResetSoft();
                        break;
                    case "grh":
                        _git.ResetHard();
                        break;
                    case "guf":
                        _git.PushForce(args);
                        break;
                    case "co":
                        _git.Checkout(args != null && args.Length > 0 ? args[0] : null);
                        break;
                    case "gcommit":
                    case "gcmt":
                    case "+gcmt":
                        _git.ConventionalCommitWizard();
                        break;
                    case "gclone":
                    case "gcloneu":
                        _git.CloneProject(args != null && args.Length > 0 ? args[0] : null, args != null && args.Length > 1 ? args[1] : null);
                        break;
                    case "gremote":
                    case "grt":
                        _git.ShowRemotesNative(args);
                        break;
                    case "gremoteu":
                    case "grtu":
                        _git.ShowRemotes();
                        break;
                    case "gco-remote":
                    case "cor":
                        _git.CheckoutRemoteBranch(args != null && args.Length > 0 ? args[0] : null);
                        break;
                    case "git-undo":
                    case "gundo":
                        _git.InvokeGitUndo();
                        break;
                    case "gmerge":
                    case "gm":
                        _git.MergeBranch(args != null && args.Length > 0 ? args[0] : null);
                        break;
                    case "gmergeu":
                    case "gmi":
                        _git.ShowMergeWizard();
                        break;
                    case "gconflict":
                    case "gcf":
                    case "gconflictu":
                    case "gcfu":
                        _git.ShowConflictResolver();
                        break;
                    case "gstash":
                    case "gst":
                    case "gstashu":
                    case "gstu":
                        _git.ShowStashManager();
                        break;
                    case "grebase":
                    case "grb":
                        _git.ShowRebaseWizard(args != null && args.Length > 0 ? args[0] : null);
                        break;
                    case "dkps":
                        _docker.ShowContainers();
                        break;
                    case "dcupb":
                        _docker.ComposeUpBuild();
                        break;
                    case "dkprunev":
                        _docker.PruneVolumes();
                        break;
                    case "dkprunei":
                        _docker.PruneImages();
                        break;
                    case "sln":
                        _dotNet.NewSolution(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "console":
                        _dotNet.NewConsole(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "webapi":
                        _dotNet.NewWebApi(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "s3mb":
                        _aws.CreateS3Bucket(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "sqsmb":
                        _aws.CreateSQSQueue(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "sqspurge":
                        _aws.PurgeSQSQueue(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "sqssend":
                        _aws.SendSQSMessage(args != null && args.Length > 0 ? args[0] : "", args != null && args.Length > 1 ? args[1] : "", args != null && args.Length > 2 ? args[2] : null);
                        break;
                    case "sqsrecv":
                        _aws.ReceiveSQSMessage(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "sqsattr":
                        _aws.GetSQSAttributes(args != null && args.Length > 0 ? args[0] : "");
                        break;
                    case "dbld":
                    case "db":
                    case "dbldu":
                    case "dbu":
                        _dotNet.Build();
                        break;
                    case "dr":
                    case "dru":
                        _dotNet.Run();
                        break;
                    case "dtst":
                    case "dt":
                    case "dtstu":
                    case "dtu":
                        _dotNet.Test();
                        break;
                    case "dk":
                    case "dku":
                    case "dki":
                        _docker.ShowContainers();
                        break;
                    case "dimgu":
                        _docker.ShowImages();
                        break;
                    case "dlogsu":
                        _docker.ShowContainerLogs();
                        break;
                    case "aws-whoamiu":
                        _aws.ShowCallerIdentity();
                        break;
                    case "aws-s3u":
                        _aws.ShowS3Buckets();
                        break;
                    case "df":
                        _dotNet.Format();
                        break;
                    case "dcl":
                        _dotNet.Clean();
                        break;
                    case "drestore":
                    case "dres":
                        _dotNet.Restore();
                        break;
                    case "dpublish":
                        _dotNet.Publish();
                        break;
                    case "dpack":
                        _dotNet.Pack();
                        break;
                    case "dpubpkg":
                        _dotNet.PublishPackage();
                        break;
                    case "sln-add":
                        _dotNet.AddAllProjectsToSolution();
                        break;
                    case "open-term":
                    case "term":
                    case "wt":
                        SystemHelper.Instance.OpenNewTerminalSession();
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
                        _dotNet.Watch();
                        break;
                    case "clean-build":
                    case "dclean":
                        _dotNet.RemoveBinObj(Directory.GetCurrentDirectory());
                        break;
                    case "add-migration":
                    case "da":
                        var migName = AnsiConsole.Ask<string>("Migration name:");
                        var addCtx = Console.IsInputRedirected ? null : AnsiConsole.Prompt(new TextPrompt<string>("DbContext name [dim](optional, press Enter to skip)[/]:").AllowEmpty());
                        if (string.IsNullOrWhiteSpace(addCtx)) addCtx = null;
                        _dotNet.AddMigration(migName, context: addCtx);
                        break;
                    case "update-db":
                    case "du":
                        var upCtx = Console.IsInputRedirected ? null : AnsiConsole.Prompt(new TextPrompt<string>("DbContext name [dim](optional, press Enter to skip)[/]:").AllowEmpty());
                        if (string.IsNullOrWhiteSpace(upCtx)) upCtx = null;
                        _dotNet.UpdateDatabase(context: upCtx);
                        break;
                    case "docker-health":
                        _docker.ShowDockerHealthDashboard();
                        break;
                    case "dkcl":
                        _docker.ShowCleanupDashboard();
                        break;
                    case "dkrmac":
                        _docker.RemoveAllContainers();
                        break;
                    case "dkstac":
                        _docker.StopAllContainers();
                        break;
                    case "dimg":
                        _docker.ShowImages();
                        break;
                    case "dlogs":
                        _docker.ShowContainerLogs();
                        break;
                    case "dcup":
                    case "dkcpu":
                        _docker.ComposeUp();
                        break;
                    case "dcdown":
                    case "dkcpd":
                        _docker.ComposeDown();
                        break;
                    case "aws-whoami":
                        _aws.ShowCallerIdentity();
                        break;
                    case "aws-local":
                        _aws.ShowLocalStackInfo();
                        break;
                    case "aws-s3":
                        _aws.ShowS3Buckets();
                        break;
                    case "aws-sqs":
                        _aws.ShowSQSQueues();
                        break;
                    case "aws-ssm":
                        _aws.ShowSsmParameters();
                        break;
                    case "aws-sns":
                        _aws.ShowSnsTopics();
                        break;
                    case "aws-dynamodb":
                        _aws.ShowDynamoDbTables();
                        break;
                    case "aws-lambda":
                        _aws.ShowLambdaFunctions();
                        break;
                    case "rebuild-tui":
                        AnsiConsole.MarkupLine("[cyan]Rebuilding Control Center TUI binary...[/]");
                        var projFile = Path.Combine(Config.GetProfileRepoRoot(), "csapp", "AgyTui", "AgyTui.csproj");
                        if (!File.Exists(projFile)) projFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "AgyTui.csproj");
                        var buildExit = _dotNet.Build(File.Exists(projFile) ? projFile : null);
                        if (buildExit == 0)
                        {
                            try
                            {
                                var binDir = Path.Combine(Config.GetProfileRepoRoot(), "csapp", "AgyTui", "bin", "Release", "net9.0");
                                var distDir = Path.Combine(Config.GetProfileRepoRoot(), "csapp", "AgyTui", "dist");
                                if (Directory.Exists(binDir) && Directory.Exists(distDir))
                                {
                                    foreach (var file in Directory.GetFiles(binDir, "*.dll"))
                                    {
                                        var dest = Path.Combine(distDir, Path.GetFileName(file));
                                        File.Copy(file, dest, overwrite: true);
                                    }
                                }
                            }
                            catch {}
                            SpectrePanel.Success("Control Center TUI recompiled successfully! Run 'go' or open a new terminal window to apply binary changes.");
                        }
                        else SpectrePanel.Warning("Build note: If running directly inside AgyTui.exe, Windows locks the executable while in-use. Exit TUI and run 'dbld' or run via PowerShell wrapper to refresh binary.");
                        break;
                    case "claude":
                        _claude.InvokeClaude([], "cloud");
                        break;
                    case "claude-cloud":
                        _claude.InvokeClaude([], "cloud");
                        break;
                    case "claude-ollama":
                        _claude.InvokeClaude([], "local");
                        break;
                    case "codex":
                        _claude.InvokeCodex([], "cloud");
                        break;
                    case "codex-cloud":
                        _claude.InvokeCodex([], "cloud");
                        break;
                    case "codex-ollama":
                        _claude.InvokeCodex([], "local");
                        break;
                    case "openclaw":
                        _openClaw.InvokeOpenClaw([]);
                        break;
                    case "ollama-models":
                        _ollama.ManageModels();
                        break;
                    case "ollama-pull":
                        _ollama.PullModel();
                        break;
                    case "ollama-start":
                        _ollama.StartDaemon();
                        break;
                    case "ollama-logs":
                        _ollama.ShowLogs();
                        break;
                    case "ollama-benchmark":
                        _ollama.BenchmarkModels();
                        break;
                    case "ollama-status":
                        OllamaStatusWidgetCache.Invalidate();
                        break;
                    case "desk-status":
                    case "deck-status":
                        try
                        {
                            var running = _ollama.IsPortListening(18789);
                            var statusStr = running ? "[green]Online (port 18789)[/]" : "[red]Offline[/]";
                            AnsiConsole.MarkupLine($"Antigravity Deck/Desk Status: {statusStr}");
                            if (running)
                            {
                                AnsiConsole.MarkupLine("Local App URL: [cyan]http://127.0.0.1:18789[/]");
                            }
                            Console.WriteLine("\nPress any key to return...");
                            SpectrePanel.SafeReadKey();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter desk-status failed", ex);
                            SpectrePanel.Error($"desk-status error: {ex.Message}");
                        }
                        break;
                    case "desk-setup":
                    case "deck-setup":
                        try
                        {
                            AntigravityDeckHelper.Setup();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter desk-setup failed", ex);
                            SpectrePanel.Error($"desk-setup error: {ex.Message}");
                        }
                        break;
                    case "desk-start":
                    case "deck-start":
                        try
                        {
                            AntigravityDeckHelper.StartLocal();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter desk-start failed", ex);
                            SpectrePanel.Error($"desk-start error: {ex.Message}");
                        }
                        break;
                    case "desk-online":
                    case "deck-online":
                        try
                        {
                            AntigravityDeckHelper.StartOnline();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter desk-online failed", ex);
                            SpectrePanel.Error($"desk-online error: {ex.Message}");
                        }
                        break;

                    // Antigravity Manager
                    case "mgr-status":
                    case "manager-status":
                    case "agm-status":
                        try
                        {
                            var running = _ollama.IsPortListening(18790);
                            var statusStr = running ? "[green]Online (port 18790)[/]" : "[red]Offline[/]";
                            AnsiConsole.MarkupLine($"Antigravity Manager Status: {statusStr}");
                            if (running)
                            {
                                AnsiConsole.MarkupLine("Local Backend URL: [cyan]http://127.0.0.1:18790[/]");
                            }
                            Console.WriteLine("\nPress any key to return...");
                            SpectrePanel.SafeReadKey();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter mgr-status failed", ex);
                            SpectrePanel.Error($"mgr-status error: {ex.Message}");
                        }
                        break;
                    case "mgr-setup":
                    case "manager-setup":
                    case "agm-setup":
                        try
                        {
                            AntigravityManagerHelper.Setup();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter mgr-setup failed", ex);
                            SpectrePanel.Error($"mgr-setup error: {ex.Message}");
                        }
                        break;
                    case "mgr":
                    case "mgr-start":
                    case "manager-start":
                    case "agm":
                    case "agm-start":
                        try
                        {
                            AntigravityManagerHelper.StartLocal();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogError("CommandRouter mgr-start failed", ex);
                            SpectrePanel.Error($"mgr-start error: {ex.Message}");
                        }
                        break;
                    case "agy-cli":
                        if (!Config.Current.Ai.EnableAgy)
                        {
                            _claude.InvokeClaude([]);
                            break;
                        }
                        Helpers.ProcessRunner.Instance.Run("cmd.exe", "/c agy");
                        break;
                    case "ai-history":
                        {
                            var accStoreHist = _accountStore;
                            if (accStoreHist == null) break;
                            var logPath = Path.Combine(accStoreHist.AgySourceHome, "ai_activity_log.jsonl");
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
                        if (_hermes.InvokeHermes([]) == HermesResult.NotInstalled)
                        {
                            SpectrePanel.Warning("Hermes CLI is not installed on PATH.");
                            var choice = SpectreMenu.Show("Hermes Action Fallback", ["Launch local Ollama chat with default model", "View Hermes setup guide"], 0);
                            if (choice == 0)
                            {
                                _ollama.InvokeNative(null);
                            }
                            else if (choice == 1)
                            {
                                AnsiConsole.MarkupLine("[cyan]To install Nous Hermes Agent:[/] pip install hermes-agent (or npm install -g @nous/hermes)");
                            }
                        }
                        break;
                    case "hermesd":
                        if (_hermes.InvokeHermesDesktop([]) == HermesResult.NotInstalled)
                        {
                            SpectrePanel.Warning("Hermes Desktop is not installed on PATH.");
                            var choice = SpectreMenu.Show("Hermes Desktop Fallback", ["Launch local Ollama chat with default model", "View Hermes Desktop setup guide"], 0);
                            if (choice == 0)
                            {
                                _ollama.InvokeNative(null);
                            }
                            else if (choice == 1)
                            {
                                AnsiConsole.MarkupLine("[cyan]To install Hermes Desktop:[/] Download installer from https://github.com/nousresearch/hermes-desktop");
                            }
                        }
                        break;
                    case "tailscale-status":
                        SpectrePanel.Info("Tailscale status widget is available in Control Center dashboard.");
                        break;
                    case "ssh-qr":
                        SpectrePanel.Info("SSH QR code widget is available in Control Center dashboard.");
                        break;
                    case "disk":
                    case "usage":
                        SystemHelper.Instance.ShowDiskSpace();
                        break;
                    case "public-ip":
                    case "myip":
                        AnsiConsole.MarkupLine($"Public IP: [green]{SystemHelper.Instance.GetPublicIP()}[/]");
                        break;
                    case "kill-port":
                        var portStr = AnsiConsole.Ask<string>("Port number:");
                        if (int.TryParse(portStr, out var port)) SystemHelper.Instance.KillPort(port);
                        break;
                    case "ssh-info":
                        SpectrePanel.Info("SSH connection info widget is available in Control Center dashboard.");
                        break;
                    case "system-reload":
                    case "sys-reload":
                        ShowSystemReloadMenu();
                        break;
                    case "reload-cc":
                    case "rcc":
                        ReloadControlCenter();
                        break;
                    case "reload-term":
                    case "rterm":
                    case "reload-profile":
                        ReloadTerminalProfile();
                        break;
                    case "tui-log":
                    case "show-log":
                    case "tui-logs":
                        ShowExecutionLogs();
                        break;
                    case "reload-all":
                    case "rall":
                        ReloadAll();
                        break;
                    case "vault":
                    case "agyswitch":
                        SubPageNavigator.Run("agyswitch");
                        break;
                    case "ask-ai":
                        SubPageNavigator.Run("ask-ai");
                        break;
                    case "exit":
                        return 0;
                    case "prune-workspaces":
                        WorkspaceRegistry.PruneWorkspaces();
                        break;
                    case "discover-workspaces":
                        WorkspaceRegistry.AutoDiscoverWorkspaces();
                        break;
                    case "daily-note":
                        _obsidianBridge?.ShowDailyNote(AppPaths.DefaultLearningVaultDir);
                        break;
                    case "orphan-notes":
                        var obsidianCfg = ObsidianBridge.LoadConfig();
                        var vaultDir = obsidianCfg?.VaultPath ?? AppPaths.DefaultLearningVaultDir;
                        var graphNodes = ObsidianGraph.BuildGraph(vaultDir);
                        ObsidianGraph.ShowOrphans(graphNodes);
                        break;
                    case "mastery-tree":
                        var studyLog = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
                        ProgressDashboard.ShowMasteryTree(studyLog?.Sessions ?? []);
                        break;
                    case "agyquota":
                        AgyAccountDisplay.ShowAccountTree(_accountStore, _quotaEngine);
                        break;
                    case "account-tree":
                        AgyAccountDisplay.ShowAccountTree(_accountStore, _quotaEngine);
                        break;
                    case "quota-chart":
                        AgyAccountDisplay.ShowQuotaChart(_accountStore?.GetActiveAccount() ?? "", _quotaEngine);
                        break;
                    case "live-dashboard":
                        var store = _accountStore;
                        var qEngine = _quotaEngine;
                        if (store != null && qEngine != null)
                        {
                            SpectreTable.Live(["Account", "Login", "Quota W", "Quota 5h", "Last Used"], () => store.GetAccounts().Select(a =>
                            {
                                var s = qEngine.GetAccountStats(a);
                                var act = store.GetActiveAccount();
                                var n = a == act ? $"[green bold]* {a}[/]" : a;
                                var st = s.TokenStatus == "Logged In" ? "[green]●[/]" : "[red]○[/]";
                                var lu = s.LastUsed.Length >= 10 && s.LastUsed != "Never" ? s.LastUsed[..10] : "Never";
                                return new[]
                                {
                                n, st,$"{(int)Math.Round(s.GeminiWeekly)}%",$"{(int)Math.Round(s.GeminiFiveHour)}%", lu
                                };
                            }).ToArray(), 5000);
                        }
                        break;
                    case "autoswitch":
                        _accountStore?.ToggleAutoSwitch();
                        break;
                    case "no-auto-commit":
                    case "autocommit":
                        _accountStore?.ToggleNoAutoCommit();
                        break;
                    case "reset-agy":
                    case "purge-accounts":
                        SubPageAccountNavigator.PurgeAccounts(_accountStore);
                        break;
                    case "dotnet-info":
                        Helpers.ProcessRunner.Instance.RunInteractive("dotnet", ["--info"]);
                        break;
                    case "scaffold":
                        AnsiConsole.MarkupLine("[bold yellow]Project scaffolding is managed via ask-ai or template commands.[/]");
                        break;
                    case "help":
                        ProfileHelp.Show();
                        break;
                    case "mobile-setup":
                    case "mobile":
                        {
                            ThemeManager.ToggleMobileMode();
                            var currDensity = Config.GetDensity();
                            var newDensity = currDensity == "compact" ? "comfortable" : "compact";
                            Config.SetDensity(newDensity);
                            SpectrePanel.Success($"Mobile setup toggled: Prompt Mobile Mode = {ThemeManager.IsMobileModeActive()}, TUI Density = {newDensity}");
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
                            var tm = _themeManager ?? new ThemeManager();
                            string? newThemePath = null;

                            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                            {
                                newThemePath = tm.SetTheme(tPath, args[0]);
                            }
                            else if (string.Equals(Environment.GetEnvironmentVariable("NON_INTERACTIVE"), "1", StringComparison.OrdinalIgnoreCase) || Console.IsInputRedirected)
                            {
                                var currTheme = Environment.GetEnvironmentVariable("THEME") ?? "1_shell";
                                newThemePath = tm.SetTheme(tPath, currTheme);
                            }
                            else
                            {
                                var currTheme = Environment.GetEnvironmentVariable("THEME");
                                newThemePath = tm.SelectThemeInteractive(tPath, currTheme);
                            }

                            if (!string.IsNullOrEmpty(newThemePath) && _accountStore != null)
                            {
                                var agyHome = _accountStore.AgySourceHome;
                                if (!string.IsNullOrEmpty(agyHome))
                                {
                                    Directory.CreateDirectory(agyHome);
                                    var selThemeFile = Path.Combine(agyHome, "selected_theme.txt");
                                    File.WriteAllText(selThemeFile, newThemePath);
                                }
                            }
                        }
                        break;
                    case "":
                    case "cc":
                    case "control-center":
                    case "menu":
                        CcNavigator.Run();
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
                                var currentMode = Config.Current.Ui.Mode;
                                nextMode = string.Equals(currentMode, "three-pane", StringComparison.OrdinalIgnoreCase) ? "flat-tree" : "three-pane";
                            }
                            Config.SetUiMode(nextMode);
                            AnsiConsole.MarkupLine($"[green bold]UI Mode updated to '{nextMode}'. Default view is flat-tree.[/]");
                            Thread.Sleep(1200);
                        }
                        break;
                    case "density":
                        {
                            var currentDensity = Config.Current.Ui.Density;
                            var nextDensity = currentDensity == "comfortable" ? "compact" : "comfortable";
                            Config.SetDensity(nextDensity);
                            AnsiConsole.MarkupLine($"[green]UI Density toggled to '{nextDensity}'. Switch will apply next time you launch Control Center.[/]");
                            Thread.Sleep(1500);
                        }
                        break;
                    case "favorite":
                        {
                            var subAction = (args != null && args.Length > 0) ? args[0].Trim().ToLowerInvariant() : null;
                            var secondaryArg = (args != null && args.Length > 1) ? args[1].Trim().ToLowerInvariant() : null;

                            if (subAction == "reset")
                            {
                                Config.Current.Ui.FavoriteAliases = [.. Config.DefaultFavoriteAliases];
                                Config.Save();
                                SpectrePanel.Success($"Reset Favorites to default list: [{string.Join(", ", Config.DefaultFavoriteAliases)}]");
                                break;
                            }

                            if (subAction == "list" || subAction == "ls")
                            {
                                var favList = Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases;
                                AnsiConsole.Write(new Rule("[bold cyan]Pinned Favorite Aliases[/]").RuleStyle("grey"));
                                if (favList.Length == 0)
                                {
                                    AnsiConsole.MarkupLine("[yellow]No favorite aliases pinned. Use '/favorite <alias>' to pin.[/]");
                                }
                                else
                                {
                                    foreach (var f in favList)
                                    {
                                        var c = CommandRegistry.GetByAlias(f);
                                        AnsiConsole.MarkupLine($"  [cyan]•[/] [bold white]{f}[/] - {(c != null ? c.DisplayName : "Custom Alias")}");
                                    }
                                }
                                break;
                            }

                            if (subAction == "add" && !string.IsNullOrEmpty(secondaryArg))
                            {
                                var currentFavs = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
                                if (!currentFavs.Contains(secondaryArg, StringComparer.OrdinalIgnoreCase))
                                {
                                    currentFavs.Add(secondaryArg);
                                    Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                    Config.Save();
                                    SpectrePanel.Success($"Added '{secondaryArg}' to Favorites (saved to SQLite DB).");
                                }
                                else
                                {
                                    SpectrePanel.Info($"'{secondaryArg}' is already in Favorites.");
                                }
                                break;
                            }

                            if ((subAction == "remove" || subAction == "rm" || subAction == "delete") && !string.IsNullOrEmpty(secondaryArg))
                            {
                                var currentFavs = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
                                if (currentFavs.Contains(secondaryArg, StringComparer.OrdinalIgnoreCase))
                                {
                                    currentFavs.RemoveAll(a => string.Equals(a, secondaryArg, StringComparison.OrdinalIgnoreCase));
                                    Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                    Config.Save();
                                    SpectrePanel.Success($"Removed '{secondaryArg}' from Favorites (saved to SQLite DB).");
                                }
                                else
                                {
                                    SpectrePanel.Warning($"'{secondaryArg}' is not in Favorites.");
                                }
                                break;
                            }

                            if (string.IsNullOrEmpty(subAction))
                            {
                                if (Console.IsInputRedirected) break;

                                var currentFavs = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
                                var choices = new List<string>();

                                foreach (var favAlias in currentFavs)
                                {
                                    var cmd = CommandRegistry.GetByAlias(favAlias);
                                    var aliasCol = favAlias.PadRight(22);
                                    var nameCol = cmd != null ? Markup.Escape(cmd.DisplayName) : "Custom Alias";
                                    choices.Add($"⚡ {aliasCol} │ {nameCol}");
                                }

                                choices.Add("──────────────────────────────────────────────────────");
                                choices.Add("➕ Add New Favorite");
                                choices.Add("🔄 Reset Favorites to Defaults");
                                choices.Add("❌ Exit");

                                var selectedChoice = AnsiConsole.Prompt(
                                    new SelectionPrompt<string>()
                                        .Title("[bold cyan]⭐️ Favorites Manager[/]\n[grey]Left: Command Alias | Right: Description / Management Action:[/]")
                                        .PageSize(15)
                                        .AddChoices(choices));

                                if (selectedChoice == "❌ Exit" || selectedChoice.StartsWith("───"))
                                {
                                    break;
                                }

                                if (selectedChoice.StartsWith("⚡ "))
                                {
                                    var rawAlias = selectedChoice.Substring(2).Split('│')[0].Trim();
                                    var subPrompt = AnsiConsole.Prompt(
                                        new SelectionPrompt<string>()
                                            .Title($"Favorite Command: [bold cyan]/{Markup.Escape(rawAlias)}[/]")
                                            .AddChoices([
                                                $"🚀 Launch /{rawAlias}",
                                                $"✏️ Edit / Replace /{rawAlias} with Another Command",
                                                $"🗑️ Remove /{rawAlias} from Favorites",
                                                "❌ Back"
                                            ]));

                                    if (subPrompt.StartsWith("🚀 Launch"))
                                    {
                                        Execute(rawAlias, args != null && args.Length > 1 ? args[1..] : Array.Empty<string>());
                                    }
                                    else if (subPrompt.StartsWith("✏️ Edit"))
                                    {
                                        var availableCmds = CommandRegistry.All
                                            .Where(c => c.ShowInTree && !currentFavs.Contains(c.Alias, StringComparer.OrdinalIgnoreCase) && c.Alias != "favorite")
                                            .OrderBy(c => c.Alias)
                                            .ToList();

                                        if (availableCmds.Count == 0)
                                        {
                                            SpectrePanel.Info("No other available commands to swap.");
                                            break;
                                        }

                                        var editChoices = availableCmds
                                            .Select(c => $"{c.Alias.PadRight(22)} │ {Markup.Escape(c.DisplayName)}")
                                            .Concat(["❌ Cancel"])
                                            .ToList();

                                        var editSelected = AnsiConsole.Prompt(
                                            new SelectionPrompt<string>()
                                                .Title($"Select replacement command for [cyan]/{Markup.Escape(rawAlias)}[/]:")
                                                .PageSize(15)
                                                .AddChoices(editChoices));

                                        if (editSelected != "❌ Cancel")
                                        {
                                            var newAlias = editSelected.Split('│')[0].Trim();
                                            int idx = currentFavs.FindIndex(a => string.Equals(a, rawAlias, StringComparison.OrdinalIgnoreCase));
                                            if (idx >= 0)
                                            {
                                                currentFavs[idx] = newAlias;
                                            }
                                            else
                                            {
                                                currentFavs.Add(newAlias);
                                            }
                                            Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                            Config.Save();
                                            SpectrePanel.Success($"Replaced '{rawAlias}' with '{newAlias}' in Favorites (saved to SQLite DB).");
                                        }
                                    }
                                    else if (subPrompt.StartsWith("🗑️ Remove"))
                                    {
                                        currentFavs.RemoveAll(a => string.Equals(a, rawAlias, StringComparison.OrdinalIgnoreCase));
                                        Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                        Config.Save();
                                        SpectrePanel.Success($"Removed '{rawAlias}' from Favorites (saved to SQLite DB).");
                                    }
                                    break;
                                }

                                if (selectedChoice.StartsWith("➕ Add"))
                                {
                                    var availableCmds = CommandRegistry.All
                                        .Where(c => c.ShowInTree && !currentFavs.Contains(c.Alias, StringComparer.OrdinalIgnoreCase) && c.Alias != "favorite")
                                        .OrderBy(c => c.Alias)
                                        .ToList();

                                    if (availableCmds.Count == 0)
                                    {
                                        SpectrePanel.Info("All available commands are already in Favorites.");
                                        break;
                                    }

                                    var addChoices = availableCmds
                                        .Select(c => $"{c.Alias.PadRight(22)} │ {Markup.Escape(c.DisplayName)}")
                                        .Concat(["❌ Cancel"])
                                        .ToList();

                                    var addSelected = AnsiConsole.Prompt(
                                        new SelectionPrompt<string>()
                                            .Title("Select command to [green]add[/] to Favorites:")
                                            .PageSize(15)
                                            .AddChoices(addChoices));

                                    if (addSelected != "❌ Cancel")
                                    {
                                        var selectedAlias = addSelected.Split('│')[0].Trim();
                                        if (!currentFavs.Contains(selectedAlias, StringComparer.OrdinalIgnoreCase))
                                        {
                                            currentFavs.Add(selectedAlias);
                                            Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                            Config.Save();
                                            SpectrePanel.Success($"Added '{selectedAlias}' to Favorites (saved to SQLite DB).");
                                        }
                                    }
                                }
                                else if (selectedChoice.StartsWith("🔄 Reset"))
                                {
                                    Config.Current.Ui.FavoriteAliases = [.. Config.DefaultFavoriteAliases];
                                    Config.Save();
                                    SpectrePanel.Success($"Reset Favorites to default list: [{string.Join(", ", Config.DefaultFavoriteAliases)}]");
                                }
                                break;
                            }

                            // Single-alias toggle via CLI argument (e.g. /favorite proj)
                            var favTargetAlias = subAction;
                            if (!string.IsNullOrEmpty(favTargetAlias))
                            {
                                var cmd = CommandRegistry.GetByAlias(favTargetAlias);
                                if (cmd == null)
                                {
                                    SpectrePanel.Warning($"Unknown command alias '{favTargetAlias}'.");
                                }
                                else
                                {
                                    var currentFavs = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
                                    if (currentFavs.Contains(favTargetAlias, StringComparer.OrdinalIgnoreCase))
                                    {
                                        currentFavs.RemoveAll(a => string.Equals(a, favTargetAlias, StringComparison.OrdinalIgnoreCase));
                                        Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                        Config.Save();
                                        SpectrePanel.Success($"Removed '{favTargetAlias}' from Favorites (saved to SQLite DB).");
                                    }
                                    else
                                    {
                                        currentFavs.Add(favTargetAlias);
                                        Config.Current.Ui.FavoriteAliases = [.. currentFavs];
                                        Config.Save();
                                        SpectrePanel.Success($"Added '{favTargetAlias}' to Favorites (saved to SQLite DB).");
                                    }
                                }
                            }
                        }
                        break;
                    case "favorites":
                        {
                            var favs = Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases;
                            AnsiConsole.Write(new Rule("[bold cyan]Pinned Favorite Aliases[/]").RuleStyle("grey"));
                            if (favs.Length == 0)
                            {
                                AnsiConsole.MarkupLine("[yellow]No favorite aliases pinned. Use '/favorite <alias>' to pin.[/]");
                            }
                            else
                            {
                                var table = new Table().Border(TableBorder.Rounded);
                                table.AddColumn("[bold cyan]Alias[/]");
                                table.AddColumn("[bold cyan]Name[/]");
                                table.AddColumn("[bold cyan]Description[/]");
                                foreach (var f in favs)
                                {
                                    var c = CommandRegistry.GetByAlias(f);
                                    table.AddRow($"[green]{f}[/]", c?.DisplayName ?? "?", c?.Description ?? "");
                                }
                                AnsiConsole.Write(table);
                            }
                        }
                        break;
                    case "learn":
                        LearnRouter.StartLearning("jp");
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
                        var levelChoice = SpectreMenu.Show("Select Grammar Level", ["N5 (Japanese)", "N4 (Japanese)", "N3 (Japanese)", "English"], 0);
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
                        _learningGenerator.RunGenerator();
                        break;
                    case "obsidian":
                    case "obs-vault":
                        _obsidianBridge?.Run();
                        break;
                    case "sync":
                        LearnRouter.RefreshData("all");
                        break;
                    case "vault-open":
                        var openCfg = ObsidianBridge.LoadConfig();
                        var defaultOpenVault = System.IO.Path.Combine(LearnDataPaths.BaseDirectory, "learn");
                        var targetPath = openCfg?.VaultPath ?? (Directory.Exists(defaultOpenVault) ? defaultOpenVault : AppPaths.DefaultLearningVaultDir);
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
                    case "secret-set":
                        var sKey = args != null && args.Length > 0 ? args[0] : AnsiConsole.Ask<string>("Key:");
                        var sVal = args != null && args.Length > 1 ? args[1] : AnsiConsole.Ask<string>("Value:");
                        AgySecretVault.SetSecret(sKey, sVal);
                        break;
                    case "secret-get":
                        var gKey = args != null && args.Length > 0 ? args[0] : AnsiConsole.Ask<string>("Key:");
                        var secVal = AgySecretVault.GetSecret(gKey);
                        if (!string.IsNullOrEmpty(secVal)) AnsiConsole.WriteLine(secVal);
                        break;
                    case "secret-list":
                        AgySecretVault.ListSecrets();
                        break;
                    case "secret-remove":
                        var rKey = args != null && args.Length > 0 ? args[0] : AnsiConsole.Ask<string>("Key:");
                        AgySecretVault.RemoveSecret(rKey);
                        break;
                    default:
                        SpectrePanel.Warning($"Command alias '{alias}' is not implemented for direct TUI routing.");
                        exitCode = 1;
                        break;
                }
            }
            catch (Exception ex)
            {
                success = false;
                errorType = ex.GetType().Name;
                exitCode = 1;
                var displayMsg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                LogHelper.LogError($"CommandRouter.HandleCommand failed for '{alias}'", ex);
                SpectrePanel.Error($"Error running command: {displayMsg}");
            }
            AnsiConsole.WriteLine();
        }
        finally
        {
            CommandInvocationLog.Record(alias, sw.Elapsed, success, errorType);
        }
        return exitCode;
    }

    private void ShowSystemReloadMenu()
    {
        while (true)
        {
            var options = new[]
            {
                "🔄  Reload Control Center TUI (rebuild & restart session)",
                "⚡  Reload Terminal & PowerShell Profile ($PROFILE)",
                "🚀  Reload All (Full System & TUI Refresh)",
                "📂  Open Execution Log File (tui_execution.log)",
                "⬅️  Back"
            };

            var idx = SpectreMenu.ShowWithEscape("System & Terminal Reload", options, 0);
            if (idx < 0 || idx == 4) break;

            if (idx == 0)
            {
                ReloadControlCenter();
                break;
            }
            if (idx == 1)
            {
                ReloadTerminalProfile();
            }
            if (idx == 2)
            {
                ReloadAll();
                break;
            }
            if (idx == 3)
            {
                ShowExecutionLogs();
            }
        }
    }

    private void ShowExecutionLogs()
    {
        var logDir = AppPaths.LogsDir;
        var logFile = Path.Combine(logDir, "tui_execution.log");
        if (File.Exists(logFile))
        {
            try
            {
                var lines = File.ReadAllLines(logFile).TakeLast(35).ToArray();
                AnsiConsole.Clear();
                SpectrePanel.Info($"Execution Log ({logFile}) — Last {lines.Length} entries:");
                foreach (var line in lines)
                {
                    if (line.Contains("[FAIL]")) AnsiConsole.MarkupLine($"[red]{line.EscapeMarkup()}[/]");
                    else if (line.Contains("[END]")) AnsiConsole.MarkupLine($"[green]{line.EscapeMarkup()}[/]");
                    else AnsiConsole.MarkupLine($"[cyan]{line.EscapeMarkup()}[/]");
                }
                AnsiConsole.MarkupLine("\n[dim yellow]Press any key to return to menu...[/]");
                Console.ReadKey(true);
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to read log file: {ex.Message}");
            }
        }
        else
        {
            SpectrePanel.Warning($"Execution log file does not exist at '{logFile}'.");
        }
    }

    private void ReloadControlCenter()
    {
        SpectrePanel.Info("Initiating clean kill, rebuild and restart sequence for Control Center TUI & agy...");
        try
        {
            var csproj = Path.Combine(AppPaths.ProjectRoot, "AgyTui.csproj");
            var binDir = Path.Combine(AppPaths.ProjectRoot, "bin", "Debug", "net9.0");
            var exePath = Path.Combine(binDir, "AgyTui.exe");

            var script = $"Stop-Process -Name 'agy' -Force -ErrorAction SilentlyContinue; Stop-Process -Name 'AgyTui' -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 600; dotnet build '{csproj}' -c Debug; if ($?) {{ Start-Process '{exePath}' }}";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{script}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi);

            SpectrePanel.Success("Restart launcher spawned cleanly. Exiting current session...");
            Thread.Sleep(500);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to reload Control Center TUI: {ex.Message}");
        }
    }

    private void ReloadTerminalProfile()
    {
        SpectrePanel.Info("Reloading PowerShell Profile ($PROFILE)...");
        try
        {
            if (_processRunner != null)
            {
                _processRunner.RunInteractive("pwsh", new[] { "-NoProfile", "-Command", "pwsh -NoExit -Command '. $PROFILE'" });
            }
            else
            {
                Helpers.ProcessRunner.Instance.RunInteractive("pwsh", new[] { "-NoProfile", "-Command", "pwsh -NoExit -Command '. $PROFILE'" });
            }
            SpectrePanel.Success("PowerShell profile reloaded successfully.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to reload terminal profile: {ex.Message}");
        }
    }

    private void ReloadAll()
    {
        SpectrePanel.Info("Reloading PowerShell Profile and initiating Control Center TUI rebuild...");
        ReloadControlCenter();
    }
}
