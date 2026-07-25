using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgyTui.Infrastructure.Integrations.Ai;



public static class AgyAiCore
{
    private static readonly string OllamaDefaultModelFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama_default_model");

    private static string _ollamaDefaultModel = LoadDefaultModel();
    public static string OllamaDefaultModel => _ollamaDefaultModel;

    private static string LoadDefaultModel()
    {
        try
        {
            if (File.Exists(OllamaDefaultModelFile))
            {
                var saved = File.ReadAllText(OllamaDefaultModelFile).Trim();
                if (!string.IsNullOrWhiteSpace(saved)) return saved;
            }
        }
        catch
        {
        }
        return "qwen3:1.7b";

    }

    private static void PersistDefaultModel(string model)
    {
        _ollamaDefaultModel = model;

        try
        {
            File.WriteAllText(OllamaDefaultModelFile, model);
        }
        catch
        {
        }

    }

    public static string GetProfileRepoRoot()
    {
        var asmPath = typeof(AgyAiCore).Assembly.Location;
        if (string.IsNullOrEmpty(asmPath)) return Directory.GetCurrentDirectory();
        var asmDir = Path.GetDirectoryName(asmPath);
        if (string.IsNullOrEmpty(asmDir)) return Directory.GetCurrentDirectory();
        var parent = Path.GetDirectoryName(asmDir);
        if (parent == null) return asmDir;
        var grandParent = Path.GetDirectoryName(parent);
        return grandParent ?? parent;
    }

    private static string GetConfigPath()
    {
        return Path.Combine(GetProfileRepoRoot(), "profile.config.json");
    }

    public static string GetAiProviderMode()
    {
        if (Config.Current.Ai != null && !string.IsNullOrEmpty(Config.Current.Ai.ProviderMode))
        {
            return Config.Current.Ai.ProviderMode;
        }
        var path = GetConfigPath();
        if (!File.Exists(path)) return "auto";
        try
        {
            var content = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("AiProviderMode", out var prop))
            {
                return prop.GetString() ?? "auto";
            }
        }
        catch { }
        return "auto";
    }

    public static bool IsAiOllamaEnabled()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return true;
        try
        {
            var content = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("EnableAiOllama", out var prop))
            {
                return prop.ValueKind != System.Text.Json.JsonValueKind.False;
            }
        }
        catch { }
        return true;
    }

    public static bool IsAgyEnabled()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return true;
        try
        {
            var content = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("EnableAgy", out var prop))
            {
                return prop.ValueKind != System.Text.Json.JsonValueKind.False;
            }
        }
        catch { }
        return true;
    }

    public static Func<bool> IsOllamaRunningProvider { get; set; } = IsOllamaRunning;

    public static string GetEffectiveProviderMode()
    {
        var mode = GetAiProviderMode();
        if (mode == "auto")
        {
            return IsOllamaRunningProvider() ? "local" : "cloud";
        }
        return mode;
    }

    public static (string Mode, string Reason) ResolveAiMode(string alias, string? providerModeOverride = null)
    {
        if (!string.IsNullOrEmpty(providerModeOverride))
        {
            return (providerModeOverride, "explicit providerModeOverride parameter");
        }

        var modeSetting = GetAiProviderMode();
        if (modeSetting != "auto")
        {
            return (modeSetting, $"configured Ai.ProviderMode setting ('{modeSetting}')");
        }

        if (!IsAiOllamaEnabled())
        {
            return ("cloud", "AI/Ollama features disabled in config");
        }

        bool isRunning = IsOllamaRunningProvider();
        if (isRunning)
        {
            return ("local", "auto-detected local Ollama server running");
        }

        return ("cloud", "auto-detected cloud (Ollama server not running)");
    }

    public static void ShowAiModeCheck(string alias)
    {
        var (mode, reason) = ResolveAiMode(alias);
        AnsiConsole.WriteLine();
        SpectrePanel.Info($"[bold cyan]AI Mode Diagnostic for '{alias}':[/]");
        AnsiConsole.MarkupLine($"  [bold]Resolved Mode:[/] [yellow]{mode}[/]");
        AnsiConsole.MarkupLine($"  [bold]Reason:[/] {reason}");
        AnsiConsole.WriteLine();
    }

    private static void InvokeWithPipeline(string agentName, string? providerModeOverride, Action<string> executeAction)
    {
        AgyAccountCore.AutoSwitchOnQuotaExceeded();
        var activeAccount = AgyAccountCore.GetActiveAccount();
        var mode = providerModeOverride ?? GetEffectiveProviderMode();

        if (mode == "cloud" && AgyAiCore.IsAgyEnabled())
        {
            var stats = AgyAccountCore.GetAccountStats(activeAccount);
            if (stats.QuotaStatus == "Exceeded" || stats.GeminiFiveHour >= 98.0)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Account '{activeAccount}' quota is close to or has exceeded limits (5h: {stats.GeminiFiveHour}%).[/]");
                bool shouldFallback = Console.IsInputRedirected
                    ? true
                    : AnsiConsole.Confirm("Would you like to auto-fallback to local Ollama execution?");
                if (shouldFallback)
                {
                    mode = "local";
                    AnsiConsole.MarkupLine("[green]Falling back to local Ollama daemon...[/]");
                    Thread.Sleep(1000);
                }
                else
                {
                    AnsiConsole.MarkupLine("[cyan]Continuing with cloud execution...[/]");
                }
            }
        }

        var startTime = DateTime.UtcNow;
        bool success = true;

        try
        {
            executeAction(mode);
        }
        catch (Exception ex)
        {
            success = false;
            SpectrePanel.Error($"Error running AI tool {agentName}: {ex.Message}");
            throw;
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            RecordAiActivity(agentName, mode, duration, success);
            if (AgyAccountCore.CheckQuotaAfterRun(activeAccount))
            {
                AgyAccountCore.AutoSwitchOnQuotaExceeded();
            }
        }
    }

    private static void RecordAiActivity(string agentName, string mode, TimeSpan duration, bool success)
    {
        try
        {
            var logPath = Path.Combine(AgyAccountCore.AgySourceHome, "ai_activity_log.jsonl");
            var record = new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Agent = agentName,
                Mode = mode,
                DurationMs = duration.TotalMilliseconds,
                Success = success,
                Account = AgyAccountCore.GetActiveAccount()
            };
            var line = JsonSerializer.Serialize(record) + "\n";
            File.AppendAllText(logPath, line);
        }
        catch { }
    }

    public static void SetAiProviderMode(string mode)
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return;
        try
        {
            var content = File.ReadAllText(path);
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            if (dict != null)
            {
                dict["AiProviderMode"] = mode;
                var updated = System.Text.Json.JsonSerializer.Serialize(dict, options);
                File.WriteAllText(path, updated);
            }
        }
        catch { }
    }

    private static string ResolveProxyScriptPath()
    {
        var asmDir = System.IO.Path.GetDirectoryName(typeof(AgyAiCore).Assembly.Location) ?? Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(asmDir);
        for (var i = 0;
        i < 5 && dir != null;
        i++, dir = dir.Parent)
        {
            var candidate1 = System.IO.Path.Combine(dir.FullName, "Tests", "Mocks", "ollama-proxy.js");
            if (File.Exists(candidate1)) return candidate1;
            var candidate2 = System.IO.Path.Combine(dir.FullName, "Tests", "ollama-proxy.js");
            if (File.Exists(candidate2)) return candidate2;
        }
        return System.IO.Path.Combine(asmDir, "ollama-proxy.js");

    }

    private static bool IsPortListening(int port) => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e => e.Port == port);

    private static bool IsPortResponding(int port, string? pattern)
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
            var resp = HttpClientProvider.Client.GetStringAsync($"http://127.0.0.1:{port}/", cts.Token).GetAwaiter().GetResult();
            return string.IsNullOrEmpty(pattern) || resp.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is System.Net.Sockets.SocketException se)
                {
                    if (se.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                    {
                        return IsPortListening(port);
                    }
                    return false;
                }
                current = current.InnerException;
            }
            return false;
        }
    }

    private static readonly TtlCache<string, bool> _ollamaStatusCache = new(TimeSpan.FromSeconds(3));

    public static bool IsDeckRunning()
    {
        return IsPortListening(3000);
    }

    public static bool IsManagerRunning()
    {
        return IsPortListening(8045);
    }


    public static bool IsOllamaRunning()
    {
        if (_ollamaStatusCache.TryGet("status", out var cached)) return cached;
        var status = IsPortResponding(11434, "*Ollama is running*");
        _ollamaStatusCache.Set("status", status);
        return status;
    }

    public static void EnsureOllamaProxy()
    {
        const int proxyPort = 11435;
        if (IsPortResponding(proxyPort, null)) return;
        AnsiConsole.MarkupLine($"[yellow][[AI]] Ollama Proxy is not running on port {proxyPort}. Starting...[/]");
        var proxyScriptPath = ResolveProxyScriptPath();
        if (!File.Exists(proxyScriptPath))
        {
            SpectrePanel.Error($"Ollama proxy script not found at {proxyScriptPath}.");
            return;
        }
        var stdoutPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ollama_proxy_out.log");
        var stderrPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ollama_proxy_err.log");

        try
        {
            File.Delete(stdoutPath);
        }
        catch
        {
        }
        try
        {
            File.Delete(stderrPath);
        }
        catch
        {
        }
        try
        {
            var psi = new ProcessStartInfo("node", $"\"{proxyScriptPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
            ;
            var proc = Process.Start(psi);
            if (proc != null)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        using var f = File.Create(stdoutPath);
                        proc.StandardOutput.BaseStream.CopyTo(f);
                    }
                    catch
                    {
                    }
                }
                );
                _ = Task.Run(() =>
                {
                    try
                    {
                        using var f = File.Create(stderrPath);
                        proc.StandardError.BaseStream.CopyTo(f);
                    }
                    catch
                    {
                    }
                }
                );
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red][[AI]] Failed to start Ollama Proxy: {ex.Message.EscapeMarkup()}[/]");
        }
        Thread.Sleep(1000);

    }

    public static void EnsureOllamaServer()
    {
        if (!IsOllamaRunningProvider()) InitializeOllamaServer();
        EnsureOllamaProxy();

    }

    public static void InvokeOllamaNative(string? model)
    {
        EnsureOllamaServer();
        var activeModel = string.IsNullOrWhiteSpace(model) ? OllamaDefaultModel : model;
        AnsiConsole.MarkupLine($"[cyan]Starting native Ollama interactive session for '{activeModel.EscapeMarkup()}'...[/]");
        RunInteractive("ollama", ["run", activeModel]);

    }

    private static string AppendNodeOption(string? existing) => string.IsNullOrEmpty(existing) ? "--dns-result-order=ipv4first" : $"{existing} --dns-result-order=ipv4first";

    public static void InvokeCliAgent(
        string agentName,
        string cloudCmd,
        string[] argsList,
        string? providerModeOverride = null,
        Action<List<string>, string>? onCloudArgs = null,
        Action<List<string>, Dictionary<string, string?>>? onLocalSetup = null)
    {
        var finalArgs = new List<string>(argsList);
        if (File.Exists(".agy-context.md"))
        {
            try
            {
                var contextText = File.ReadAllText(".agy-context.md").Trim();
                if (!string.IsNullOrEmpty(contextText))
                {
                    AnsiConsole.MarkupLine("[green][[AGY]] Shared context handoff (.agy-context.md) found and appended to prompt.[/]");
                    finalArgs.Add("--append-system-prompt");
                    finalArgs.Add(contextText);
                }
            }
            catch { }
        }

        onCloudArgs?.Invoke(finalArgs, agentName);

        InvokeWithPipeline(agentName, providerModeOverride, mode =>
        {
            if (mode == "cloud")
            {
                RunInteractive(cloudCmd, finalArgs);
            }
            else
            {
                EnsureOllamaServer();
                var env = new Dictionary<string, string?>
                {
                    ["OLLAMA_HOST"] = "127.0.0.1:11434",
                    ["NODE_OPTIONS"] = AppendNodeOption(Environment.GetEnvironmentVariable("NODE_OPTIONS"))
                };
                var localArgs = new List<string>();
                onLocalSetup?.Invoke(localArgs, env);

                if (localArgs.Count > 0)
                {
                    RunInteractive("ollama.exe", localArgs, env);
                }
                else
                {
                    RunInteractive("ollama.exe", finalArgs, env);
                }
            }
        });
    }

    public static void InvokeClaude(string[] argsList, string? providerModeOverride = null)
    {
        var activeAccount = AgyAccountCore.GetActiveAccount();
        var sessionFile = Path.Combine(AgyAccountCore.AgySourceHome, "last_claude_account.txt");
        if (File.Exists(sessionFile))
        {
            var lastAccount = File.ReadAllText(sessionFile).Trim();
            if (lastAccount != activeAccount)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Account changed from {lastAccount} to {activeAccount} since last Claude session.[/]");
                if (!AnsiConsole.Confirm("Do you want to continue with this new account?"))
                {
                    return;
                }
            }
        }
        File.WriteAllText(sessionFile, activeAccount);

        InvokeCliAgent("Claude", "claude.cmd", argsList, providerModeOverride, onLocalSetup: (localArgs, env) =>
        {
            env["ANTHROPIC_BASE_URL"] = "http://127.0.0.1:11434";
            localArgs.Add("launch");
            localArgs.Add("claude");
            if (!argsList.Contains("--model"))
            {
                localArgs.Add("--model");
                localArgs.Add(OllamaDefaultModel);
            }
            localArgs.AddRange(argsList);
        });
    }

    public static void InvokeCodex(string[] argsList, string? providerModeOverride = null)
    {
        InvokeWithPipeline("Codex", providerModeOverride, mode =>
        {
            if (mode == "cloud")
            {
                RunInteractive("codex.cmd", argsList);
            }
            else
            {
                EnsureOllamaServer();
                var model = OllamaDefaultModel;
                var newArgsList = new List<string>();
                for (var i = 0;
                i < argsList.Length;
                i++)
                {
                    if ((argsList[i] == "--model" || argsList[i] == "-m") && i < argsList.Length - 1)
                    {
                        model = Regex.Replace(argsList[i + 1], "^ollama_custom/", "");
                        newArgsList.Add(argsList[i]);
                        newArgsList.Add(model);
                        i++;
                    }
                    else newArgsList.Add(argsList[i]);
                }
                var sandboxPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), ".codex_local_ollama");
                Directory.CreateDirectory(sandboxPath);
                var emptySkillsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity", "scratch", "empty_skills");

                try
                {
                    Directory.CreateDirectory(emptySkillsDir);
                }
                catch
                {
                }
                var configToml = $"# Temp sandbox configuration generated at {sandboxPath}/config.toml\nmodel = \"{model}\"\n\n[codex]\nskills_directory = \"{emptySkillsDir}\"\n\n[mcp_servers]\n# Intentionally empty to disable external tool description loads\n".Replace('\\', '/');
                File.WriteAllText(System.IO.Path.Combine(sandboxPath, "config.toml"), configToml);
                var env = new Dictionary<string, string?>
                {
                    ["OLLAMA_HOST"] = "127.0.0.1:11435",
                    ["NODE_OPTIONS"] = AppendNodeOption(Environment.GetEnvironmentVariable("NODE_OPTIONS")),
                    ["OPENAI_BASE_URL"] = null,
                    ["OPENAI_API_KEY"] = null,
                    ["CODEX_HOME"] = sandboxPath
                };
                var flags = new List<string>();
                if (!newArgsList.Contains("--model") && !newArgsList.Contains("-m"))
                {
                    flags.Add("--model");
                    flags.Add(model);
                }
                flags.Add("--oss");
                flags.Add("--local-provider");
                flags.Add("ollama");
                var argList = new List<string>(flags);
                argList.AddRange(newArgsList);
                RunInteractive("codex.cmd", argList, env);
            }
        });
    }

    public static void EnsureOpenClawGateway()
    {
        const int port = 18789;
        if (IsPortListening(port)) return;
        AnsiConsole.MarkupLine("[yellow][[AI]] OpenClaw Gateway is not running. Starting...[/]");

        try
        {
            Process.Start(new ProcessStartInfo("openclaw", "gateway start")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }
            );
        }
        catch
        {
        }
        Thread.Sleep(2000);

    }

    public static void InvokeOpenClaw(string[] argsList)
    {
        InvokeWithPipeline("OpenClaw", "local", _ =>
        {
            EnsureOllamaServer();
            EnsureOpenClawGateway();
            string? model = null;
            var cleanArgs = new List<string>();
            for (var i = 0;
            i < argsList.Length;
            i++)
            {
                if (argsList[i] == "--model" && i < argsList.Length - 1)
                {
                    model = argsList[i + 1];
                    i++;
                }
                else cleanArgs.Add(argsList[i]);
            }
            model ??= OllamaDefaultModel;
            var cleanModel = Regex.Replace(model, "^ollama/", "");
            RunInteractive("openclaw.cmd", ["config", "set", "agents.defaults.model.primary", $"ollama/{cleanModel}"]);
            var argList2 = cleanArgs.Count == 0 ? new List<string>
            {
                "chat"
            }
            : cleanArgs;
            var env = new Dictionary<string, string?>
            {
                ["OLLAMA_HOST"] = "127.0.0.1:11434"
            }
            ;
            RunInteractive("openclaw.cmd", argList2, env);
        });
    }

    public static void InvokeClawdbot(string[] argsList) => InvokeOpenClaw(argsList);

    public enum HermesResult
    {
        Launched, NotInstalled

    }

    public static List<string> CleanHermesArguments(string[] argsList, string defaultModel)
    {
        var result = new List<string> { "chat" };
        string? modelValue = null;
        var extraArgs = new List<string>();

        for (int i = 0; i < argsList.Length; i++)
        {
            if ((argsList[i] == "--model" || argsList[i] == "-m") && i < argsList.Length - 1)
            {
                modelValue = argsList[i + 1];
                i++;
            }
            else
            {
                extraArgs.Add(argsList[i]);
            }
        }

        if (!string.IsNullOrEmpty(modelValue))
        {
            result.Add("--model");
            result.Add(modelValue);
        }

        result.AddRange(extraArgs);
        return result;
    }

    public static HermesResult InvokeHermes(string[] argsList)
    {
        EnsureOllamaServer();
        var bin = FindHermesBinary("hermes", [System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes", "bin", "hermes.exe"), System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes", "bin", "hermes.cmd"), System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "Programs", "Hermes", "bin", "hermes.exe")]);
        if (bin == null) return HermesResult.NotInstalled;
        var configPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes", "config.toml");
        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, "");
        }
        var configContent = File.ReadAllText(configPath);
        if (!configContent.Contains("127.0.0.1:11434"))
        {
            AnsiConsole.MarkupLine("[yellow][[AI]] Configuring local Ollama endpoint in Hermes config.toml...[/]");
            File.AppendAllText(configPath, "\n[model_providers.ollama_custom]\nname = \"Ollama Custom\"\nbase_url = \"http://127.0.0.1:11434/v1\"\n");
        }
        var argList = CleanHermesArguments(argsList, OllamaDefaultModel);
        AnsiConsole.MarkupLine("[cyan]Starting Hermes Agent TUI...[/]");

        var result = HermesResult.NotInstalled;
        InvokeWithPipeline("Hermes", "local", _ =>
        {
            RunInteractive(bin, argList);
            result = HermesResult.Launched;
        });
        return result;
    }

    public static HermesResult InvokeHermesDesktop(string[] argsList)
    {
        EnsureOllamaServer();
        var bin = FindHermesBinary("hermes-desktop", [System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes", "bin", "hermes-desktop.exe"), System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? "", "Programs", "Hermes", "bin", "hermes-desktop.exe")]);
        if (bin != null)
        {
            AnsiConsole.MarkupLine("[cyan]Starting Hermes Desktop...[/]");
            var result = HermesResult.NotInstalled;
            InvokeWithPipeline("HermesDesktop", "local", _ =>
            {
                RunInteractive(bin, []);
                result = HermesResult.Launched;
            });
            return result;
        }
        var cliBin = FindOnPath("hermes");
        if (cliBin != null)
        {
            AnsiConsole.MarkupLine("[cyan]Starting Hermes Desktop...[/]");
            var result = HermesResult.NotInstalled;
            InvokeWithPipeline("HermesDesktop", "local", _ =>
            {
                RunInteractive(cliBin, ["desktop"]);
                result = HermesResult.Launched;
            });
            return result;
        }
        return HermesResult.NotInstalled;

    }

    private static string? FindHermesBinary(string exeNameOnPath, string[] localPaths)
    {
        var onPath = FindOnPath(exeNameOnPath);
        if (onPath != null) return onPath;
        foreach (var p in localPaths) if (File.Exists(p)) return p;
        return null;

    }

    internal static string? FindOnPath(string exe)
    {
        try
        {
            var psi = new ProcessStartInfo("where", exe)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
            ;

            using var p = Process.Start(psi);
            var output = p?.StandardOutput.ReadToEnd().Trim();
            p?.WaitForExit();
            if (p?.ExitCode == 0 && !string.IsNullOrWhiteSpace(output)) return output.Split('\n')[0].Trim();
        }
        catch
        {
        }
        return null;

    }

    public static void InitializeOllamaServer()
    {
        const int port = 11434;
        AnsiConsole.MarkupLine("[cyan][[Ollama]] Resetting port 11434...[/]");
        if (IsPortListening(port))
        {
            SystemHelper.KillPort(port);
            Thread.Sleep(1000);
        }
        else
        {
            AnsiConsole.MarkupLine("[green][[Ollama]] Port 11434 is free.[/]");
        }
        AnsiConsole.MarkupLine("[cyan][[Ollama]] Starting Ollama server...[/]");
        var logPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? System.IO.Path.GetTempPath(), "Ollama", "server.log");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);

        try
        {
            var psi = new ProcessStartInfo("ollama", "serve")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
            ;
            psi.Environment["OLLAMA_HOST"] = $"127.0.0.1:{port}";
            var proc = Process.Start(psi);
            if (proc != null)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        using var f = File.Create(logPath);
                        proc.StandardOutput.BaseStream.CopyTo(f);
                    }
                    catch
                    {
                    }
                }
                );
                _ = Task.Run(() =>
                {
                    try
                    {
                        proc.StandardError.BaseStream.CopyTo(Stream.Null);
                    }
                    catch
                    {
                    }
                }
                );
            }
        }
        catch
        {
        }
        for (var retry = 0;
        retry < 10;
        retry++)
        {
            Thread.Sleep(1000);
            if (IsPortResponding(port, "*Ollama is running*"))
            {
                AnsiConsole.MarkupLine("[green][[Ollama]] Ollama server is running and ready![/]");
                return;
            }
        }
        AnsiConsole.MarkupLine("[yellow][[Ollama]] Failed to verify if Ollama started successfully after 10 seconds.[/]");

    }

    public static void InstallAIIntegrations()
    {
        InstallIfMissing("claude", "@anthropic-ai/claude-code", "Claude Code");
        InstallIfMissing("codex", "@openai/codex", "Codex CLI");
        InstallIfMissing("openclaw", "openclaw", "OpenClaw");

    }

    private static void InstallIfMissing(string command, string npmPackage, string label)
    {
        if (FindOnPath(command) != null)
        {
            AnsiConsole.MarkupLine($"[green][[AI]] {label.EscapeMarkup()} is already installed.[/]");
            return;
        }
        AnsiConsole.MarkupLine($"[cyan][[AI]] Installing {label.EscapeMarkup()} via npm...[/]");
        RunInteractive(FindOnPath("npm.cmd") ?? "npm.cmd", ["install", "-g", npmPackage]);

    }

    public static void SetOllamaModel(string? modelName)
    {
        if (FindOnPath("ollama") == null)
        {
            SpectrePanel.Error("Ollama is not installed or not in PATH.");
            return;
        }
        var listOutput = RunCapture("ollama", "list");
        var localModels = listOutput.Split('\n').Skip(1).Select(l => Regex.Split(l.Trim(), @"\s+").FirstOrDefault()).Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m!).ToArray();
        if (localModels.Length == 0)
        {
            SpectrePanel.Error("No local Ollama models found. Please download one using 'ollama pull'.");
            return;
        }
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            if (localModels.Contains(modelName))
            {
                PersistDefaultModel(modelName);
                AnsiConsole.MarkupLine($"[green]🟢 Default Ollama model set to '{modelName.EscapeMarkup()}'.[/]");
            }
            else
            {
                SpectrePanel.Error($"Model '{modelName}' is not available locally. Available models: {string.Join(", ", localModels)}");
            }
            return;
        }
        var menuItems = new string[localModels.Length];
        var defaultIdx = 0;
        for (var i = 0;
        i < localModels.Length;
        i++)
        {
            menuItems[i] = localModels[i] == OllamaDefaultModel ? $"{localModels[i]} (Active)" : localModels[i];
            if (localModels[i] == OllamaDefaultModel) defaultIdx = i;
        }
        var selected = SpectreMenu.Show("Select Default Ollama Model", menuItems, defaultIdx);
        if (selected >= 0)
        {
            PersistDefaultModel(localModels[selected]);
            AnsiConsole.MarkupLine($"[green]🟢 Default Ollama model set to '{localModels[selected].EscapeMarkup()}'.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
        }

    }

    public static void ShowOllamaLogs()
    {
        EnsureOllamaServer();
        var logPath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? System.IO.Path.GetTempPath(), "Ollama", "server.log");
        if (File.Exists(logPath))
        {
            AnsiConsole.MarkupLine("[cyan]--- Ollama Server Log (Last 50 lines) ---[/]");
            foreach (var line in File.ReadLines(logPath).TakeLast(50)) Console.WriteLine(line);
            AnsiConsole.MarkupLine("[cyan]----------------------------------------[/]");
        }
        else
        {
            SpectrePanel.Warning($"Ollama server log not found at {logPath}.");
        }

    }

    public static void ShowAiDashboard()
    {
        while (true)
        {
            var statusInfo = (OllamaStatus)SpectreProgress.SpinnerResult("[AI] Loading Ollama server configuration...", () =>
            {
                var status = "Offline";
                var models = new List<string>();

                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(1));
                    HttpClientProvider.Client.GetStringAsync("http://127.0.0.1:11434/", cts.Token).GetAwaiter().GetResult();
                    status = "Running";
                }
                catch
                {
                }
                if (status == "Running")
                {
                    try
                    {
                        var list = RunCapture("ollama", "list");
                        var lines = list.Split('\n');
                        for (var i = 1;
                        i < lines.Length;
                        i++)
                        {
                            var parts = Regex.Split(lines[i].Trim(), @"\s+");
                            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) models.Add(parts[0]);
                        }
                    }
                    catch
                    {
                    }
                }
                return (object)new OllamaStatus(status, models);
            }
            )!;
            var cHalf = (char)0x2584;
            var cFull = (char)0x2588;
            var cTop = (char)0x2580;
            var aiHeaders = new[]
            {
            $" {cHalf}{cFull}{cFull}{cFull}{cFull}{cHalf} {cHalf}{cFull}{cFull}{cFull}{cFull}{cHalf} Powershell Profile CLI v2.0",$" {cFull}{cTop} {cTop} {cFull}{cTop} {cTop} Ollama Local AI Hub",$" {cFull} {cFull} Ollama Status: {statusInfo.Status}",$" {cFull}{cHalf} {cHalf} {cFull}{cHalf} {cHalf} Active Model: {OllamaDefaultModel}",$" {cTop}{cFull}{cFull}{cFull}{cFull}{cTop} {cTop}{cFull}{cFull}{cFull}{cFull}{cTop} Select an agent to run. Esc to back.","============================================="
        }
            ;
            var providerMode = GetAiProviderMode();
            var modeLabel = providerMode switch
            {
                "cloud" => "Cloud API (Normal)",
                "local" => "Local Ollama",
                "auto" => "Auto (Local if online, else Cloud)",
                _ => "Cloud API (Normal)"
            };
            var (claudeMode, _) = ResolveAiMode("claude");
            var (codexMode, _) = ResolveAiMode("codex");

            var menuItems = new[]
            {
                $"[Agent] Claude CLI [{claudeMode}] (Interactive coding chat)",
                "[Agent] Hermes TUI (Autonomous workspace assistant)",
                $"[Agent] Codex CLI [{codexMode}] (Natural language command tool)",
                "[Agent] OpenClaw CLI (Local agent router)",
                "[Agent] Clawdbot TUI (Interactive helper)",
                $"[Setting] Provider Mode: {modeLabel}",
                "[Model] Select / Set Default Local Model",
                "[Action] Auto-Install missing LLM CLI tools",
                "[x] Return to Main Menu"
            };
            while (Console.KeyAvailable) Console.ReadKey(true);
            var selected = SpectreMenu.ShowRobust(aiHeaders, menuItems, 0, false, true);
            if (selected < 0 || selected == menuItems.Length - 1) break;
            switch (selected)
            {
                case 0:
                    InvokeClaude([]);
                    break;
                case 1:
                    InvokeHermes(OllamaDefaultModel is
                    {
                        Length: > 0
                    }
                ? ["--model", OllamaDefaultModel] : []);
                    break;
                case 2:
                    InvokeCodex(OllamaDefaultModel is
                    {
                        Length: > 0
                    }
                ? ["--model", OllamaDefaultModel] : []);
                    break;
                case 3:
                    InvokeOpenClaw([]);
                    break;
                case 4:
                    InvokeClawdbot(OllamaDefaultModel is
                    {
                        Length: > 0
                    }
                ? ["--model", OllamaDefaultModel] : []);
                    break;
                case 5:
                    var choices = new[] { "cloud", "local", "auto" };
                    var labels = new[] { "Cloud API (Normal)", "Local Ollama", "Auto (Local if online, else Cloud)" };
                    var defaultIdx = Array.IndexOf(choices, providerMode);
                    if (defaultIdx < 0) defaultIdx = 0;
                    var chosenIdx = SpectreMenu.Show("Select AI Provider Mode", labels, defaultIdx);
                    if (chosenIdx >= 0)
                    {
                        var chosenMode = choices[chosenIdx];
                        SetAiProviderMode(chosenMode);
                        AnsiConsole.MarkupLine($"[green][[AI]] Switched Provider Mode to '{labels[chosenIdx]}'.[/]");
                        Thread.Sleep(1000);
                    }
                    break;
                case 6:
                    SetOllamaModel(null);
                    break;
                case 7:
                    InstallAIIntegrations();
                    break;
            }

        }

    }

    private sealed record OllamaStatus(string Status, List<string> Models);

    public static void AskAi(string resolvedQueryOrError)
    {
        if (string.IsNullOrWhiteSpace(resolvedQueryOrError))
        {
            AnsiConsole.MarkupLine("[yellow]No recent console errors found to explain.[/]");
            return;

        }
        AnsiConsole.MarkupLine("[cyan]🤖 Querying local AI for explanation/fix...[/]");
        var prompt = $"Analyze the following PowerShell error or question and provide a brief explanation and a clear, copy-pasteable fix:\n\n{resolvedQueryOrError}";
        var body = JsonSerializer.Serialize(new
        {
            model = OllamaDefaultModel,
            prompt,
            stream = false

        }
        );

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            var resp = HttpClientProvider.Client.PostAsync("http://127.0.0.1:11434/api/generate", new StringContent(body, Encoding.UTF8, "application/json"), cts.Token).GetAwaiter().GetResult();
            var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("response", out var respProp))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]🤖 AI Explanation:[/]");
                Console.WriteLine(respProp.GetString()?.Trim());
            }

        }
        catch
        {
            SpectrePanel.Error("Failed to connect to local Ollama. Ensure Ollama server is running.");

        }

    }

    public static string GenerateDraftDescription(string diff)
    {
        try
        {
            var prompt = $"Analyze this git diff and output ONLY a single short (under 72 chars), clear description of the changes (no prefix, no markdown, no quotes, no boilerplate) suitable for a git commit message:\n\n{diff}";
            var body = JsonSerializer.Serialize(new
            {
                model = OllamaDefaultModel,
                prompt,
                stream = false
            });
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
            var resp = HttpClientProvider.Client.PostAsync("http://127.0.0.1:11434/api/generate", new StringContent(body, Encoding.UTF8, "application/json"), cts.Token).GetAwaiter().GetResult();
            var text = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("response", out var respProp))
            {
                return respProp.GetString()?.Trim().Trim('"', '\'') ?? "";
            }
        }
        catch { }
        return "";
    }

    private static void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        var activeAccount = AgyAccountCore.GetActiveAccount();
        var accountDir = AgyAccountCore.GetAccountDirectory(activeAccount);
        var fullEnv = env != null ? new Dictionary<string, string?>(env) : new Dictionary<string, string?>();
        if (!fullEnv.ContainsKey("GEMINI_HOME"))
        {
            fullEnv["GEMINI_HOME"] = accountDir;
        }
        if (AgyAccountCore.IsNoAutoCommitEnabled() && !fullEnv.ContainsKey("AGY_AUTO_COMMIT"))
        {
            fullEnv["AGY_AUTO_COMMIT"] = "false";
        }

        var argList = new List<string>(args);
        bool targetsClaudeOrCodexOrAgy = exe.Contains("agy") || exe.Contains("claude") || exe.Contains("codex") || args.Any(a => a is "claude" or "codex" or "agy" || a.Contains("claude", StringComparison.OrdinalIgnoreCase));
        if (AgyAccountCore.IsNoAutoCommitEnabled() && targetsClaudeOrCodexOrAgy)
        {
            if (!argList.Contains("--no-auto-commit") && !argList.Contains("--no-commit"))
            {
                argList.Add("--no-auto-commit");
            }
        }

        ProcessRunner.RunInteractive(exe, argList, fullEnv, workingDir);
    }

    private static string RunCapture(string exe, string args)
    {
        return ProcessRunner.RunCapture(exe, args);
    }
}
