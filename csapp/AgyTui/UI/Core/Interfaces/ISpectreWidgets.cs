namespace AgyTui.UI.Core.Interfaces;

public interface ISpectreMenu
{
    int Show(string header, string[] items, int defaultIndex);
    int Show(string header, string[] items, int defaultIndex, bool searchEnabled);
    int ShowRobust(string[] headerLines, string[] items, int defaultIndex, bool searchEnabled, bool fullScreen);
    string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex);
    string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex, string initialFilter);
    int ShowWithEscape(string title, string[] items, int defaultIndex);
}

public interface ISpectrePanel
{
    void Info(string message);
    void Success(string message);
    void Error(string message);
    void Warning(string message);
}

public interface ISpectreTable
{
    void Render(string[] columns, string[][] rows, bool markup = false);
    void Live(string[] columns, Func<string[][]> dataSource, int refreshMs = 5000);
}

public interface ISpectreProgress
{
    void Spinner(string message, Action action);
    T Spinner<T>(string message, Func<T> func);
    void BulkProgress(string label, string[] items, Action<int, string> action);
}

public interface IStatusWidgetRegistry
{
    IEnumerable<AgyTui.UI.Core.Common.IStatusWidget> GetAll();
    AgyTui.UI.Core.Common.IStatusWidget? GetByAlias(string alias);
}
