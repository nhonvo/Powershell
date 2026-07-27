using AgyTui.Infrastructure.Integrations.Ai.Abstractions;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public class ClaudeProvider : IClaudeClient
{
    public void InvokeClaude(string[] argsList, string? providerModeOverride = null)
    {
        var exe = "claude";
        AgyServices.ProcessRunner.RunInteractive(exe, argsList);
    }

    public void InvokeCodex(string[] argsList, string? providerModeOverride = null)
    {
        var exe = "codex";
        AgyServices.ProcessRunner.RunInteractive(exe, argsList);
    }

    public static void InvokeClaudeStatic(string[] argsList, string? providerModeOverride = null)
        => AgyServices.Claude.InvokeClaude(argsList, providerModeOverride);

    public static void InvokeCodexStatic(string[] argsList, string? providerModeOverride = null)
        => AgyServices.Claude.InvokeCodex(argsList, providerModeOverride);
}
