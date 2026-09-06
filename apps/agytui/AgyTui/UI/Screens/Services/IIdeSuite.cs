namespace AgyTui.UI.Screens.Services;

public interface IIdeSuite
{
    void RunTerminalIde(string? initialPath = null);
    void RunDiffViewer(string workspacePath, string? filePath = null);
    void RunCodeViewer(string filePath);
    void RunSymbolSearch(string dirPath);
}
