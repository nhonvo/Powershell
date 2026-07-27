namespace AgyTui.Infrastructure.Integrations.Ai.Abstractions;

public interface IClaudeClient
{
    void InvokeClaude(string[] argsList, string? providerModeOverride = null);
    void InvokeCodex(string[] argsList, string? providerModeOverride = null);
}
