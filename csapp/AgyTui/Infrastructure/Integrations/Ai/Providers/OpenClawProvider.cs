namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public class OpenClawProvider : IOpenClawClient
{
    public void EnsureGateway()
    {
        if (!AgyServices.Ollama.IsPortListening(18789))
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

    public void InvokeOpenClaw(string[] argsList)
    {
        EnsureGateway();
        AgyServices.ProcessRunner.RunInteractive("openclaw", argsList);
    }

    public void InvokeClawdbot(string[] argsList) => InvokeOpenClaw(argsList);
}
