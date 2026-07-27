using AgyTui.Infrastructure.Integrations.Ai.Providers;

namespace AgyTui.Infrastructure.Integrations.Ai;

public static class OllamaClient
{
    public static void ShowOllamaLogs() => OllamaProvider.ShowOllamaLogs();
    public static void ManageOllamaModels() => OllamaProvider.ManageOllamaModels();
    public static void BenchmarkOllamaModels() => OllamaProvider.BenchmarkOllamaModels();
    public static void PullOllamaModel() => OllamaProvider.PullOllamaModel();
    public static void StartOllamaDaemon() => OllamaProvider.StartOllamaDaemon();
}
