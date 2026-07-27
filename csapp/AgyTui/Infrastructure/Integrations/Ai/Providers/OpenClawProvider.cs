using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public static class OpenClawProvider
{
    public static void EnsureOpenClawGateway()
    {
        if (!OllamaProvider.IsPortListening(18789))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "openclaw",
                    Arguments = "gateway start",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch { }
        }
    }

    public static void InvokeOpenClaw(string[] argsList)
    {
        EnsureOpenClawGateway();
        AiProcessRunner.RunInteractive("openclaw", argsList);
    }

    public static void InvokeClawdbot(string[] argsList) => InvokeOpenClaw(argsList);
}
