using AgyTui.Infrastructure.Integrations.Ai.Abstractions;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public class ClaudeProvider : IClaudeClient
{
    private readonly IAiProcessRunner _processRunner;

    public ClaudeProvider(IAiProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public void InvokeClaude(string[] argsList, string? providerModeOverride = null)
    {
        _processRunner.RunInteractive("claude", argsList);
    }

    public void InvokeCodex(string[] argsList, string? providerModeOverride = null)
    {
        _processRunner.RunInteractive("codex", argsList);
    }
}
