using System.Text.RegularExpressions;

namespace AgyTui.UI.Core.Common;

public static class SpectreMenu
{
    public static int Show(string header, string[] items, int defaultIndex) => CoreShow([header], items, [], false, false);

    public static int Show(string header, string[] items, int defaultIndex, bool searchEnabled) => CoreShow([header], items, [], searchEnabled, false);

    public static int ShowRobust(string[] headerLines, string[] items, int defaultIndex, bool searchEnabled, bool fullScreen) => CoreShow(headerLines, items, [], searchEnabled, fullScreen);

    public static string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex) => ShowDynamic(header, resolver, defaultIndex, string.Empty);

    public static string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex, string initialFilter)
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


    private static int CoreShow(string[] headerLines, string[] items, string[] cmds, bool searchEnabled, bool fullScreen)
    {
        if (items.Length == 0) return -1;
        if (fullScreen) AnsiConsole.Clear();
        PrintHeader(headerLines);
        return PromptIndex(items, searchEnabled);
    }

    private static void PrintHeader(string[] lines)
    {
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            AnsiConsole.Write(new Rule($"[bold cyan]{line.EscapeMarkup()}[/]").RuleStyle("grey"));
    }

    private static int PromptIndex(string[] items, bool searchEnabled)
    {
        var prompt = BuildPrompt(items, searchEnabled);

        try
        {
            return Array.IndexOf(items, AnsiConsole.Prompt(prompt));
        }
        catch
        {
            return -1;
        }
    }

    private static SelectionPrompt<string> BuildPrompt(string[] items, bool searchEnabled)
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

    public static int ShowWithEscape(string title, string[] items, int defaultIndex)
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
            var (top, end) = ScrollableListView.ComputeViewport(items.Length, selected, pageSize);

            for (var i = top; i < end; i++)
            {
                if (i == selected)
                {
                    AnsiConsole.MarkupLine($"[green bold]> {items[i].EscapeMarkup()}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"  {items[i].EscapeMarkup()}");
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[dim cyan]Item {selected + 1}/{items.Length}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine("[bold green]↑/↓/j/k[/] [dim]Navigate[/]  [bold cyan]PgDn/PgUp[/] [dim]Page[/]  [bold yellow]Home/End[/] [dim]Ends[/]  [bold green]Enter[/] [dim]Select[/]  [bold red]Esc/q[/] [dim]Cancel[/]");

            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    selected = (selected - 1 + items.Length) % items.Length;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    selected = (selected + 1) % items.Length;
                    break;
                case ConsoleKey.PageUp:
                case ConsoleKey.U:
                    selected = Math.Max(0, selected - pageSize);
                    break;
                case ConsoleKey.PageDown:
                case ConsoleKey.D:
                    selected = Math.Min(items.Length - 1, selected + pageSize);
                    break;
                case ConsoleKey.Home:
                    selected = 0;
                    break;
                case ConsoleKey.End:
                    selected = items.Length - 1;
                    break;
                case ConsoleKey.Enter:
                    return selected;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    return -1;
            }
        }
    }
}

public static class SpectrePager
{
    public static void Show(string title, string content)
    {
        var lines = (content ?? string.Empty).Split('\n');
        Show(title, lines);
    }

    public static void Show(string title, string[] lines)
    {
        var pageSize = Math.Max(5, Console.WindowHeight - 8);
        var totalLines = lines.Length;
        var top = 0;
        var searchBuffer = "";
        var searching = false;
        Console.CursorVisible = false;

        bool isFirstRender = true;
        try
        {
            while (true)
            {
                var filtered = string.IsNullOrEmpty(searchBuffer)
                    ? lines
                    : lines.Where(l => l.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                totalLines = filtered.Length;
                if (top >= totalLines && totalLines > 0) top = totalLines - 1;

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

                AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
                if (searching)
                {
                    AnsiConsole.MarkupLine($"[yellow]Search:[/] [white]{searchBuffer.EscapeMarkup()}_[/] [dim](Esc to clear)[/]");
                }

                var (topRow, endRow) = ScrollableListView.ComputeViewport(totalLines, top, pageSize);
                for (var i = topRow; i < endRow; i++)
                    AnsiConsole.MarkupLine(filtered[i].EscapeMarkup());
                for (var p = endRow; p < topRow + pageSize; p++)
                    AnsiConsole.WriteLine();

                int currentEnd = endRow;
                int currentPage = totalLines == 0 ? 0 : (top / pageSize) + 1;
                int totalPages = totalLines == 0 ? 0 : (int)Math.Ceiling((double)totalLines / pageSize);

                AnsiConsole.Write(new Rule($"[dim cyan]Page {currentPage}/{totalPages}[/] [dim]({(totalLines == 0 ? 0 : top + 1)}–{currentEnd} of {totalLines} lines)[/]").RuleStyle("grey"));
                AnsiConsole.MarkupLine("[bold green]↑/↓/j/k[/] [dim]Scroll[/]  [bold cyan]PgDn/PgUp/d/u[/] [dim]Page[/]  [bold yellow]Home/End/g/G[/] [dim]Ends[/]  [bold magenta]/[/] [dim]Search[/]  [bold red]Esc/q[/] [dim]Back[/]");

                var key = Console.ReadKey(true);
                if (searching)
                {
                    if (key.Key == ConsoleKey.Escape)
                    {
                        searching = false;
                        searchBuffer = "";
                        continue;
                    }
                    if (key.Key == ConsoleKey.Enter)
                    {
                        searching = false;
                        continue;
                    }
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (searchBuffer.Length > 0) searchBuffer = searchBuffer[..^1];
                        top = 0;
                        continue;
                    }
                    if (!char.IsControl(key.KeyChar))
                    {
                        searchBuffer += key.KeyChar;
                        top = 0;
                        continue;
                    }
                }

                switch (key.Key)
                {
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        if (top + pageSize < totalLines) top++;
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        if (top > 0) top--;
                        break;
                    case ConsoleKey.PageDown:
                    case ConsoleKey.D:
                        top = Math.Min(totalLines - pageSize, top + pageSize);
                        if (top < 0) top = 0;
                        break;
                    case ConsoleKey.PageUp:
                    case ConsoleKey.U:
                        top = Math.Max(0, top - pageSize);
                        break;
                    case ConsoleKey.Home:
                        top = 0;
                        break;
                    case ConsoleKey.End:
                        top = Math.Max(0, totalLines - pageSize);
                        break;
                    case ConsoleKey.G:
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)) top = Math.Max(0, totalLines - pageSize);
                        else top = 0;
                        break;
                    case ConsoleKey.Oem2:
                    case ConsoleKey.Divide:
                    case ConsoleKey.F:
                        searching = true;
                        break;
                    case ConsoleKey.Escape:
                        if (!string.IsNullOrEmpty(searchBuffer)) { searchBuffer = ""; break; }
                        return;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Q:
                        return;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }
}

public static class SpectrePanel
{
    public static void Success(string message) => Render(message, Color.Green, "✓ Success");

    public static void Error(string message) => Render(message, Color.Red, "✗ Error");

    public static void Warning(string message) => Render(message, Color.Yellow, "⚠ Warning");

    public static void Info(string message) => Render(message, Color.Cyan1, "ℹ Info");

    private static void Render(string message, Color border, string header) =>
        AnsiConsole.Write(new Panel(message.EscapeMarkup())
        {
            Header = new PanelHeader($"[bold]{header}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(border),
            Padding = new Padding(1, 0)
        });
}

public static class SpectreProgress
{
    public static void Spinner(string message, Action action) =>
        AnsiConsole.Status()
            .Spinner(Spectre.Console.Spinner.Known.Dots2)
            .SpinnerStyle(new Style(AgyThemeColors.GetAccentColor()))
            .Start($"[bold white]{message.EscapeMarkup()}[/]", _ => action());

    public static T Spinner<T>(string message, Func<T> func)
    {
        T result = default!;
        AnsiConsole.Status()
            .Spinner(Spectre.Console.Spinner.Known.Dots2)
            .SpinnerStyle(new Style(AgyThemeColors.GetAccentColor()))
            .Start($"[bold white]{message.EscapeMarkup()}[/]", _ => { result = func(); });
        return result;
    }

    public static void BulkProgress(string label, string[] items, Action<int, string> action) =>
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


}

public static class SpectreTable
{
    public static void Render(string[] columns, string[][] rows, bool markup = false)
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

    public static void Live(string[] columns, Func<string[][]> dataSource, int refreshMs = 5000)
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
