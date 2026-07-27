using AgyTui.Core.Models;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.Infrastructure.Integrations.Ai;

public static class AgyAiCore
{
    public enum HermesResult
    {
        Success,
        NotInstalled,
        Error
    }

    public static string OllamaDefaultModel => OllamaProvider.OllamaDefaultModel;

    public static string GetProfileRepoRoot() => Config.GetProfileRepoRoot();

    public static string GetAiProviderMode() => Config.Current.AiProviderMode;

    public static bool IsAiOllamaEnabled() => Config.Current.EnableAiOllama;

    public static bool IsAgyEnabled() => Config.Current.EnableAgy;

    public static string GetEffectiveProviderMode()
    {
        var configured = GetAiProviderMode()?.ToLowerInvariant();
        if (configured is "cloud" or "ollama" or "hybrid") return configured;
        return "hybrid";
    }

    public static void SetAiProviderMode(string mode)
    {
        if (mode is "cloud" or "ollama" or "hybrid")
        {
            Config.Current.AiProviderMode = mode;
            Config.Save();
        }
    }

    public static bool IsPortListening(int port) => OllamaProvider.IsPortListening(port);
    public static bool IsOllamaRunning() => OllamaProvider.IsOllamaRunning();
    public static bool IsDeckRunning() => OllamaProvider.IsPortListening(18789);
    public static bool IsManagerRunning() => OllamaProvider.IsPortListening(18790);

    public static string ResolveProxyScriptPath() => AiProcessRunner.ResolveProxyScriptPath();

    public static void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
        => AiProcessRunner.RunInteractive(exe, args, env, workingDir);

    public static string RunCapture(string exe, string args) => AiProcessRunner.RunCapture(exe, args);

    // --- Provider Delegations ---
    public static void InvokeClaude(string[] argsList, string? providerModeOverride = null)
        => ClaudeProvider.InvokeClaude(argsList, providerModeOverride);

    public static void InvokeCodex(string[] argsList, string? providerModeOverride = null)
        => ClaudeProvider.InvokeCodex(argsList, providerModeOverride);

    public static void EnsureOllamaServer() => OllamaProvider.EnsureOllamaServer();
    public static void EnsureOllamaProxy() => OllamaProvider.EnsureOllamaProxy();
    public static void InvokeOllamaNative(string? model) => OllamaProvider.InvokeOllamaNative(model);
    public static void InitializeOllamaServer() => OllamaProvider.InitializeOllamaServer();
    public static void SetOllamaModel(string? modelName) => OllamaProvider.SetOllamaModel(modelName);
    public static void ShowOllamaLogs() => OllamaProvider.ShowOllamaLogs();

    public static HermesResult InvokeHermes(string[]? argsList = null)
        => (HermesResult)HermesProvider.InvokeHermes(argsList);

    public static HermesResult InvokeHermesDesktop(string[]? argsList = null)
        => (HermesResult)HermesProvider.InvokeHermesDesktop(argsList);

    public static void EnsureOpenClawGateway() => OpenClawProvider.EnsureOpenClawGateway();
    public static void InvokeOpenClaw(string[] argsList) => OpenClawProvider.InvokeOpenClaw(argsList);
    public static void InvokeClawdbot(string[] argsList) => OpenClawProvider.InvokeClawdbot(argsList);

    // --- Service Delegations ---
    public static void ShowAiDashboard() => AiDashboardView.ShowAiDashboard();
    public static void ShowAiModeCheck(string alias) => AiDashboardView.ShowAiModeCheck(alias);
    public static void AskAi(string query) => AiDashboardView.AskAi(query);
    public static void InstallAIIntegrations() => AiDashboardView.InstallAIIntegrations();

    public static string GenerateDraftDescription(string diff) => AiCommitGenerator.GenerateDraftDescription(diff);

    // --- Provider-Separated Project Scanning Facade ---
    public static ProjectScanResult[] ScanProjectsForClaude(string? baseDir = null)
        => AiProjectScanner.ScanProjectsForClaude(baseDir);

    public static ProjectScanResult[] ScanProjectsForOllama(string? baseDir = null)
        => AiProjectScanner.ScanProjectsForOllama(baseDir);

    public static ProjectScanResult[] ScanProjectsForAgy(string? baseDir = null)
        => AiProjectScanner.ScanProjectsForAgy(baseDir);

    public static ProjectScanResult[] ScanProjects(string provider, string? baseDir = null)
        => AiProjectScanner.ScanProjects(provider, baseDir);
}
