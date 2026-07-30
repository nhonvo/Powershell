using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public class OpenClawProvider : IOpenClawClient
{
    private readonly IAiProcessRunner _processRunner;
    private readonly IOllamaClient _ollama;

    public OpenClawProvider(IAiProcessRunner processRunner, IOllamaClient ollama)
    {
        _processRunner = processRunner;
        _ollama = ollama;
    }

    public OpenClawProvider() : this(Bootstrapper.ServiceProvider.GetRequiredService<IAiProcessRunner>(), Bootstrapper.ServiceProvider.GetRequiredService<IOllamaClient>()) { }

    public void EnsureGateway()
    {
        if (!_ollama.IsPortListening(18789))
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
        _processRunner.RunInteractive("openclaw", argsList);
    }

    public void InvokeClawdbot(string[] argsList) => InvokeOpenClaw(argsList);
}
