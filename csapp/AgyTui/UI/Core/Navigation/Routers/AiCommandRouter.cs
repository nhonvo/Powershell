using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.UI.Core.Navigation.Routers;

public class AiCommandRouter
{
    private readonly IAiProcessRunner _processRunner;
    private readonly IAgyAccountStore _accountStore;
    private readonly IAgyQuotaEngine _quotaEngine;

    public AiCommandRouter(IAiProcessRunner processRunner, IAgyAccountStore accountStore, IAgyQuotaEngine quotaEngine)
    {
        _processRunner = processRunner;
        _accountStore = accountStore;
        _quotaEngine = quotaEngine;
    }

    public bool TryHandle(string alias, string[] args, out int exitCode)
    {
        exitCode = 0;
        switch (alias.ToLowerInvariant())
        {
            case "ai-dashboard":
            case "ai":
                AiDashboardView.ShowAiDashboard(new OllamaClient());
                return true;
            case "ask-ai":
                if (args.Length > 0)
                {
                    AiDashboardView.AskAi(string.Join(" ", args));
                }
                else
                {
                    SpectrePanel.Warning("Usage: ask-ai <query>");
                }
                return true;
            case "hermes":
                var hermes = new HermesProvider(_processRunner);
                hermes.InvokeHermes(args);
                return true;
            case "openclaw":
                var claw = new OpenClawProvider(_processRunner, new OllamaClient());
                claw.InvokeOpenClaw(args);
                return true;
            default:
                return false;
        }
    }
}
