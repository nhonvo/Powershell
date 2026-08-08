using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Services;

namespace AgyTui.UI.Core.Navigation.Routers;

public class SystemCommandRouter
{
    private readonly IAgyAccountStore _accountStore;
    private readonly IThemeManager _themeManager;

    public SystemCommandRouter(IAgyAccountStore accountStore, IThemeManager themeManager)
    {
        _accountStore = accountStore;
        _themeManager = themeManager;
    }

    public bool TryHandle(string alias, string[] args, out int exitCode)
    {
        exitCode = 0;
        switch (alias.ToLowerInvariant())
        {
            case "system-info":
            case "sysinfo":
                SpectrePanel.Info($"OS: {Environment.OSVersion} | Machine: {Environment.MachineName} | User: {Environment.UserName}");
                return true;
            case "reload-profile":
                var processRunner = new AiProcessRunner(_accountStore);
                processRunner.RunInteractive("pwsh", new[] { "-NoProfile", "-Command", "pwsh -NoExit -Command '. $PROFILE'" });
                SpectrePanel.Success("PowerShell profile reloaded successfully.");
                return true;
            default:
                return false;
        }
    }
}
