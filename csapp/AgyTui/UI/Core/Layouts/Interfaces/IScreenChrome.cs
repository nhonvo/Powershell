namespace AgyTui.UI.Core.Layouts.Interfaces;

public interface IScreenChrome
{
    void RenderHeader(string title, string subtitle = "");
    void RenderFooter(string tip = "");
    string Accent(string text);
    string Success(string text);
    string Warning(string text);
    string Error(string text);
    string Muted(string text);
}
