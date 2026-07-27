using AgyTui.Core.Models;
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

    public static string OllamaDefaultModel => AgyServices.Ollama.DefaultModel;

    public static bool IsAiOllamaEnabled() => Config.Current.EnableAiOllama;

    public static bool IsAgyEnabled() => Config.Current.EnableAgy;

    public static string GetEffectiveProviderMode()
    {
        var configured = Config.Current.AiProviderMode?.ToLowerInvariant();
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

    public static bool IsPortListening(int port) => AgyServices.Ollama.IsPortListening(port);
    public static bool IsOllamaRunning() => AgyServices.Ollama.IsRunning;
    public static bool IsDeckRunning() => AgyServices.Ollama.IsPortListening(18789);
    public static bool IsManagerRunning() => AgyServices.Ollama.IsPortListening(18790);

    public static void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
        => AgyServices.ProcessRunner.RunInteractive(exe, args, env, workingDir);

    // --- Provider Interface Delegations ---
    public static void InvokeClaude(string[] argsList, string? providerModeOverride = null)
        => AgyServices.Claude.InvokeClaude(argsList, providerModeOverride);

    public static void InvokeCodex(string[] argsList, string? providerModeOverride = null)
        => AgyServices.Claude.InvokeCodex(argsList, providerModeOverride);

    public static void EnsureOllamaServer() => AgyServices.Ollama.EnsureServer();
    public static void InvokeOllamaNative(string? model) => AgyServices.Ollama.InvokeNative(model);
    public static void InitializeOllamaServer() => AgyServices.Ollama.EnsureServer();
    public static void SetOllamaModel(string? modelName) => AgyServices.Ollama.SetModel(modelName);
    public static void ShowOllamaLogs() => AgyServices.Ollama.ShowLogs();

    public static HermesResult InvokeHermes(string[]? argsList = null)
        => (HermesResult)AgyServices.Hermes.InvokeHermes(argsList);

    public static HermesResult InvokeHermesDesktop(string[]? argsList = null)
        => (HermesResult)AgyServices.Hermes.InvokeHermesDesktop(argsList);

    public static void InvokeOpenClaw(string[] argsList) => AgyServices.OpenClaw.InvokeOpenClaw(argsList);
    public static void InvokeClawdbot(string[] argsList) => AgyServices.OpenClaw.InvokeClawdbot(argsList);

    // --- Service Interface Delegations ---
    public static void ShowAiDashboard() => AiDashboardView.ShowAiDashboard();
    public static void ShowAiModeCheck(string alias) => AiDashboardView.ShowAiModeCheck(alias);
    public static void AskAi(string query) => AiDashboardView.AskAi(query);
    public static void InstallAIIntegrations() => AiDashboardView.InstallAIIntegrations();

    public static string GenerateDraftDescription(string diff) => AgyServices.CommitGenerator.GenerateDraftDescription(diff);

    // --- Project Scanning Interface Facade ---
    public static ProjectScanResult[] ScanProjects(string provider, string? baseDir = null)
        => AgyServices.ProjectScanner.ScanProjects(provider, baseDir);
}
