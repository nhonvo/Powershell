namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public static class OllamaProvider
{
    public static string OllamaDefaultModel => AgyServices.Ollama.DefaultModel;

    public static bool IsPortListening(int port) => AgyServices.Ollama.IsPortListening(port);

    public static bool IsOllamaRunning() => AgyServices.Ollama.IsRunning;

    public static void EnsureOllamaServer() => AgyServices.Ollama.EnsureServer();

    public static void InvokeOllamaNative(string? model) => AgyServices.Ollama.InvokeNative(model);

    public static void InitializeOllamaServer() => AgyServices.Ollama.EnsureServer();

    public static void SetOllamaModel(string? modelName) => AgyServices.Ollama.SetModel(modelName);

    public static void ShowOllamaLogs() => AgyServices.Ollama.ShowLogs();

    public static void ManageOllamaModels() => AgyServices.Ollama.ManageModels();

    public static void BenchmarkOllamaModels() => AgyServices.Ollama.BenchmarkModels();

    public static void PullOllamaModel() => AgyServices.Ollama.PullModel();

    public static void StartOllamaDaemon() => AgyServices.Ollama.StartDaemon();
}
