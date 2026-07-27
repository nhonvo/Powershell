using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public static class ClaudeProvider
{
    public static void InvokeClaude(string[] argsList, string? providerModeOverride = null)
    {
        var exe = "claude";
        AiProcessRunner.RunInteractive(exe, argsList);
    }

    public static void InvokeCodex(string[] argsList, string? providerModeOverride = null)
    {
        var exe = "codex";
        AiProcessRunner.RunInteractive(exe, argsList);
    }
}
