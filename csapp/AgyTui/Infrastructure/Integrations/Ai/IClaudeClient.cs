namespace AgyTui.Infrastructure.Integrations.Ai;

public interface IClaudeClient
{
    void InvokeClaude(string[] argsList, string? providerModeOverride = null);
    void InvokeCodex(string[] argsList, string? providerModeOverride = null);
}
