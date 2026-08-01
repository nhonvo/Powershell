namespace AgyTui.UI.Screens.Interfaces;

public interface IIdeSuite
{
    void RunTerminalIde(string? initialPath = null);
    void RunDiffViewer(string workspacePath, string? filePath = null);
    void RunCodeViewer(string filePath);
    void RunSymbolSearch(string dirPath);
}
