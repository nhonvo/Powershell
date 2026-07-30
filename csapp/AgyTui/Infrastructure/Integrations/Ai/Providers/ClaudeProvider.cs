using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

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
        EnsureSessionAccountMarker("last_claude_account.txt", "Claude");
        _processRunner.RunInteractive("claude", argsList);
    }

    public void InvokeCodex(string[] argsList, string? providerModeOverride = null)
    {
        EnsureSessionAccountMarker("last_codex_account.txt", "Codex");
        _processRunner.RunInteractive("codex", argsList);
    }

    private static void EnsureSessionAccountMarker(string filename, string agentName)
    {
        try
        {
            var store = Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>();
            var homeDir = store.AgySourceHome;
            if (string.IsNullOrEmpty(homeDir)) return;

            Directory.CreateDirectory(homeDir);
            var sessionFile = Path.Combine(homeDir, filename);
            var activeAccount = store.GetActiveAccount();

            if (File.Exists(sessionFile))
            {
                try
                {
                    var lastAccount = File.ReadAllText(sessionFile).Trim();
                    if (!string.IsNullOrEmpty(lastAccount) && !string.Equals(lastAccount, activeAccount, StringComparison.OrdinalIgnoreCase))
                    {
                        SpectrePanel.Warning($"Account changed from {lastAccount} to {activeAccount} since last {agentName} session.");
                    }
                }
                catch { }
            }

            File.WriteAllText(sessionFile, activeAccount, Encoding.UTF8);
        }
        catch { }
    }
}
