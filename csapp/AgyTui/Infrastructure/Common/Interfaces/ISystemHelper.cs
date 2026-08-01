namespace AgyTui.Infrastructure.Common;

public interface ISystemHelper
{
    bool IsFuzzyMatch(string text, string pattern);
    string BoldFuzzyMatch(string text, string pattern);
    void OpenExplorer(string path = "");
    void OpenNewTerminalSession(string path = "", string? initialCommand = null, bool promptOptions = false);
    void ShowDiskSpace();
    string GetPublicIP();
    void KillPort(int port);
}
