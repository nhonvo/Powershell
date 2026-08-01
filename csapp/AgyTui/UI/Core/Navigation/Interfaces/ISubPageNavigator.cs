namespace AgyTui.UI.Core.Navigation.Interfaces;

public interface ISubPageNavigator
{
    void Run(string mode, string initialQuery = "");
    void RunScreen(IScreenView screenView, string initialQuery = "");
    string ProcessSearchKey(ConsoleKeyInfo key, string currentBuffer);
}
