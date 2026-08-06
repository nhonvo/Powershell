using System.Text.RegularExpressions;
using AgyTui.UI.Core.Interfaces;
using Spectre.Console;

namespace AgyTui.UI.Core.Common;

public class SpectreMenuService : ISpectreMenu
{
    public int Show(string header, string[] items, int defaultIndex) => CoreShow([header], items, [], false, false);

    public int Show(string header, string[] items, int defaultIndex, bool searchEnabled) => CoreShow([header], items, [], searchEnabled, false);

    public int ShowRobust(string[] headerLines, string[] items, int defaultIndex, bool searchEnabled, bool fullScreen) => CoreShow(headerLines, items, [], searchEnabled, fullScreen);

    public string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex) => ShowDynamic(header, resolver, defaultIndex, string.Empty);

    public string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex, string initialFilter)
    {
        var items = resolver(initialFilter);
        if (items.Length == 0) return null;
        PrintHeader([header]);

        try
        {
            return AnsiConsole.Prompt(BuildPrompt(items, true));
        }
        catch
        {
            return null;
        }
    }

    private int CoreShow(string[] headerLines, string[] items, string[] cmds, bool searchEnabled, bool fullScreen)
    {
        if (items.Length == 0) return -1;
        if (Console.IsInputRedirected)
        {
            LogHelper.Log("[SpectreMenuService] Non-interactive terminal detected. Returning default index 0.", "DEBUG");
            return 0;
        }
        if (fullScreen) AnsiConsole.Clear();
        PrintHeader(headerLines);
        return PromptIndex(items, searchEnabled);
    }

    private void PrintHeader(string[] lines)
    {
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            AnsiConsole.Write(new Rule($"[bold cyan]{line.EscapeMarkup()}[/]").RuleStyle("grey"));
    }

    private int PromptIndex(string[] items, bool searchEnabled)
    {
        var prompt = BuildPrompt(items, searchEnabled);

        try
        {
            return Array.IndexOf(items, AnsiConsole.Prompt(prompt));
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[SpectreMenuService] PromptIndex non-fatal: {ex.Message}", "DEBUG");
            return -1;
        }
    }

    private SelectionPrompt<string> BuildPrompt(string[] items, bool searchEnabled)
    {
        var pageSize = Math.Min(15, Math.Max(5, Console.WindowHeight - 8));
        var prompt = new SelectionPrompt<string>()
            .PageSize(pageSize)
            .HighlightStyle(new Style(Color.Green, decoration: Decoration.Bold))
            .MoreChoicesText("[dim cyan](Move ↑/↓ or j/k to reveal more items)[/]")
            .UseConverter(item => item.EscapeMarkup());
        if (searchEnabled) prompt.SearchEnabled = true;
        prompt.AddChoices(items);
        return prompt;
    }

    public int ShowWithEscape(string title, string[] items, int defaultIndex)
    {
        if (items.Length == 0) return -1;
        var selected = defaultIndex;
        Console.CursorVisible = false;

        bool isFirstRender = true;
        while (true)
        {
            try
            {
                if (isFirstRender)
                {
                    AnsiConsole.Clear();
                    isFirstRender = false;
                }
                else
                {
                    Console.SetCursorPosition(0, 0);
                    Console.Write("\x1b[J");
                }
            }
            catch { }

            AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var pageSize = Math.Min(15, Math.Max(5, Console.WindowHeight - 8));
            int half = pageSize / 2;
            int start = Math.Max(0, Math.Min(selected - half, items.Length - pageSize));
            int end = Math.Min(items.Length, start + pageSize);

            if (start > 0)
            {
                AnsiConsole.MarkupLine($"[dim cyan]  ▲ {start} item(s) above...[/]");
            }

            for (int i = start; i < end; i++)
            {
                if (i == selected)
                {
                    AnsiConsole.MarkupLine($"[bold green] > {items[i].EscapeMarkup()}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"  [dim]{items[i].EscapeMarkup()}[/]");
                }
            }

            if (end < items.Length)
            {
                AnsiConsole.MarkupLine($"[dim cyan]  ▼ {items.Length - end} item(s) below...[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim grey]Nav: ↑/k (up)  ↓/j (down)  Enter (select)  Esc/q (back)[/]");

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow || key.KeyChar == 'k' || key.KeyChar == 'K')
            {
                selected = selected > 0 ? selected - 1 : items.Length - 1;
            }
            else if (key.Key == ConsoleKey.DownArrow || key.KeyChar == 'j' || key.KeyChar == 'J')
            {
                selected = selected < items.Length - 1 ? selected + 1 : 0;
            }
            else if (key.Key == ConsoleKey.PageUp)
            {
                selected = Math.Max(0, selected - pageSize);
            }
            else if (key.Key == ConsoleKey.PageDown)
            {
                selected = Math.Min(items.Length - 1, selected + pageSize);
            }
            else if (key.Key == ConsoleKey.Home)
            {
                selected = 0;
            }
            else if (key.Key == ConsoleKey.End)
            {
                selected = items.Length - 1;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                return selected;
            }
            else if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                return -1;
            }
        }
    }
}

public static class SpectreMenu
{
    private static readonly ISpectreMenu _service = new SpectreMenuService();
    public static ISpectreMenu Instance => _service;

    public static int Show(string header, string[] items, int defaultIndex) => _service.Show(header, items, defaultIndex);
    public static int Show(string header, string[] items, int defaultIndex, bool searchEnabled) => _service.Show(header, items, defaultIndex, searchEnabled);
    public static int ShowRobust(string[] headerLines, string[] items, int defaultIndex, bool searchEnabled, bool fullScreen) => _service.ShowRobust(headerLines, items, defaultIndex, searchEnabled, fullScreen);
    public static string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex) => _service.ShowDynamic(header, resolver, defaultIndex);
    public static string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex, string initialFilter) => _service.ShowDynamic(header, resolver, defaultIndex, initialFilter);
    public static int ShowWithEscape(string title, string[] items, int defaultIndex) => _service.ShowWithEscape(title, items, defaultIndex);
}

public static class SpectrePager
{
    public static void Show(string title, string content)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var lines = content.Split('\n');
        int pageSize = Math.Max(10, Console.WindowHeight - 6);
        int totalPages = (int)Math.Ceiling(lines.Length / (double)pageSize);
        if (totalPages == 0) totalPages = 1;

        int currentPage = 0;

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/] [dim](Page {currentPage + 1}/{totalPages})[/]").RuleStyle("grey"));

            int start = currentPage * pageSize;
            int end = Math.Min(lines.Length, start + pageSize);

            for (int i = start; i < end; i++)
            {
                AnsiConsole.MarkupLine($"[dim]{i + 1,4} │[/] {lines[i].EscapeMarkup()}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim grey][Space/n] Next Page  [p] Prev Page  [q/Esc] Exit[/]");

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape || key.KeyChar == 'q' || key.KeyChar == 'Q') break;
            if (key.Key == ConsoleKey.Spacebar || key.KeyChar == 'n' || key.KeyChar == 'N' || key.Key == ConsoleKey.DownArrow)
            {
                if (currentPage < totalPages - 1) currentPage++;
            }
            else if (key.KeyChar == 'p' || key.KeyChar == 'P' || key.Key == ConsoleKey.UpArrow)
            {
                if (currentPage > 0) currentPage--;
            }
        }
    }

    public static void Show(string title, string[] lines, Func<string, bool>? onKey = null)
    {
        Show(title, string.Join('\n', lines));
    }
}

public class SpectrePanelService : ISpectrePanel
{
    public void Success(string message) => Render(message, Color.Green, "✓ Success");

    public void Error(string message) => Render(message, Color.Red, "✗ Error");

    public void Warning(string message) => Render(message, Color.Yellow, "⚠ Warning");

    public void Info(string message) => Render(message, Color.Cyan1, "ℹ Info");

    private static void Render(string message, Color border, string header) =>
        AnsiConsole.Write(new Panel(message.EscapeMarkup())
        {
            Header = new PanelHeader($"[bold]{header}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(border),
            Padding = new Padding(1, 0)
        });
}

public static class SpectrePanel
{
    private static readonly ISpectrePanel _service = new SpectrePanelService();
    public static ISpectrePanel Instance => _service;

    public static void Success(string message) => _service.Success(message);
    public static void Error(string message) => _service.Error(message);
    public static void Warning(string message) => _service.Warning(message);
    public static void Info(string message) => _service.Info(message);

    public static void SafeReadKey()
    {
        try
        {
            if (!Console.IsInputRedirected)
            {
                Console.ReadKey(true);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[SpectrePanel] SafeReadKey ignored non-fatal exception: {ex.Message}", "DEBUG");
        }
    }
}

public class SpectreProgressService : ISpectreProgress
{
    public void Spinner(string message, Action action) =>
        AnsiConsole.Status()
            .Spinner(Spectre.Console.Spinner.Known.Dots2)
            .SpinnerStyle(new Style(AgyThemeColors.GetAccentColor()))
            .Start($"[bold white]{message.EscapeMarkup()}[/]", _ => action());

    public T Spinner<T>(string message, Func<T> func)
    {
        T result = default!;
        AnsiConsole.Status()
            .Spinner(Spectre.Console.Spinner.Known.Dots2)
            .SpinnerStyle(new Style(AgyThemeColors.GetAccentColor()))
            .Start($"[bold white]{message.EscapeMarkup()}[/]", _ => { result = func(); });
        return result;
    }

    public void BulkProgress(string label, string[] items, Action<int, string> action) =>
        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn { CompletedStyle = new Style(AgyThemeColors.GetSelectedColor()) }, new PercentageColumn(), new ElapsedTimeColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask($"[bold cyan]{label.EscapeMarkup()}[/]", maxValue: items.Length);
                for (var i = 0; i < items.Length; i++)
                {
                    task.Description = $"[bold cyan]{label.EscapeMarkup()}:[/] [white]{items[i].EscapeMarkup()}[/]";
                    action(i, items[i]);
                    task.Increment(1);
                }
            });
}

public static class SpectreProgress
{
    private static readonly ISpectreProgress _service = new SpectreProgressService();
    public static ISpectreProgress Instance => _service;

    public static void Spinner(string message, Action action) => _service.Spinner(message, action);
    public static T Spinner<T>(string message, Func<T> func) => _service.Spinner(message, func);
    public static void BulkProgress(string label, string[] items, Action<int, string> action) => _service.BulkProgress(label, items, action);
}

public static class LogHelper
{
    public static string GetLogFilePath()
    {
        return Path.Combine(AppPaths.LogsDir, "profile.log");
    }

    public static void Log(string message, string level = "INFO")
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);
        }
        catch
        {
        }
    }

    public static void LogError(string message, Exception ex)
    {
        Log($"{message}: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", "ERROR");
    }
}

public class SpectreTableService : ISpectreTable
{
    public void Render(string[] columns, string[][] rows, bool markup = false)
    {
        var t = new Table
        {
            Border = TableBorder.Rounded
        };
        foreach (var col in columns)
            t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));
        foreach (var row in rows)
            t.AddRow(markup ? row : row.Select(c => c.EscapeMarkup()).ToArray());
        AnsiConsole.Write(t);
    }

    public void Live(string[] columns, Func<string[][]> dataSource, int refreshMs = 5000)
    {
        var t = new Table
        {
            Border = TableBorder.Rounded
        };
        foreach (var col in columns)
            t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));
        AnsiConsole.Live(t).Start(ctx =>
        {
            while (true)
            {
                t.Rows.Clear();
                foreach (var row in dataSource())
                    t.AddRow(row);
                ctx.Refresh();
                if (Console.KeyAvailable)
                    break;
                Thread.Sleep(Math.Min(refreshMs, 500));
            }
            Console.ReadKey(true);
        });
    }
}

public static class SpectreTable
{
    private static readonly ISpectreTable _service = new SpectreTableService();
    public static ISpectreTable Instance => _service;

    public static void Render(string[] columns, string[][] rows, bool markup = false) => _service.Render(columns, rows, markup);
    public static void Live(string[] columns, Func<string[][]> dataSource, int refreshMs = 5000) => _service.Live(columns, dataSource, refreshMs);
}
