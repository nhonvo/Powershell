namespace AgyTui.Infrastructure.Integrations.Ai.Abstractions;

public interface IOllamaClient
{
    string DefaultModel { get; }
    bool IsRunning { get; }
    bool IsPortListening(int port);
    void EnsureServer();
    void InvokeNative(string? model = null);
    void SetModel(string? modelName);
    void ShowLogs();
    void ManageModels();
    void BenchmarkModels();
    void PullModel();
    void StartDaemon();
}
