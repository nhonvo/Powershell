using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;

namespace AgyTui.Components;

public static class SpectreMenu
{
    public static int Show(string header, string[] items, int defaultIndex) => CoreShow([header], items, [], false, false);

    public static int Show(string header, string[] items, int defaultIndex, bool searchEnabled) => CoreShow([header], items, [], searchEnabled, false);

    public static int ShowRobust(string[] headerLines, string[] items, int defaultIndex, bool searchEnabled, bool fullScreen) => CoreShow(headerLines, items, [], searchEnabled, fullScreen);

    public static int Show(string[] headerLines, string[] items, string[] cmds, int defaultIndex, bool searchEnabled, bool fullScreen)
    {
        if (items.Length == 0) return -1;
        if (fullScreen) AnsiConsole.Clear();
        PrintHeader(headerLines);
        if (cmds.Length > defaultIndex && !string.IsNullOrWhiteSpace(cmds[defaultIndex]))
            AnsiConsole.Write(new Panel($"[dim]{cmds[defaultIndex].EscapeMarkup()}[/]")
            {
                Header = new PanelHeader("[grey]Command[/]"),
                Border = BoxBorder.Rounded
            });
        return PromptIndex(items, searchEnabled);
    }

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

    public static void InitializeTuiColors()
    {
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
        var prompt = new SelectionPrompt<string>().PageSize(pageSize).HighlightStyle(new Style(Color.Green));
        if (searchEnabled) prompt.SearchEnabled = true;
        prompt.AddChoices(items);
        return prompt;
    }

    public static int ShowWithEscape(string title, string[] items, int defaultIndex)
    {
        if (items.Length == 0) return -1;
        var selected = defaultIndex;
        Console.CursorVisible = false;

        while (true)
        {
            try
            {
                AnsiConsole.Clear();
            }
            catch { }

            AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            for (var i = 0; i < items.Length; i++)
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
            AnsiConsole.Write(new Rule("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc/q Cancel[/]").RuleStyle("grey"));

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
        Console.CursorVisible = false;

        try
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
                for (var i = top; i < Math.Min(top + pageSize, totalLines); i++)
                    AnsiConsole.MarkupLine(lines[i].EscapeMarkup());
                for (var p = Math.Min(top + pageSize, totalLines); p < top + pageSize; p++)
                    AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim] ↑↓/jk scroll d/u page g/G ends / search q quit" + $" ({top + 1}–{Math.Min(top + pageSize, totalLines)} of {totalLines})[/]");
                var key = Console.ReadKey(true);
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
                    case ConsoleKey.G when key.Modifiers == ConsoleModifiers.Shift:
                        top = 0;
                        break;
                    case ConsoleKey.End:
                    case ConsoleKey.G:
                        top = Math.Max(0, totalLines - pageSize);
                        break;
                    case ConsoleKey.Oem2:
                    case ConsoleKey.F:
                        Console.CursorVisible = true;
                        AnsiConsole.Markup("[cyan]Search: [/]");
                        var q = Console.ReadLine() ?? "";
                        Console.CursorVisible = false;
                        if (!string.IsNullOrWhiteSpace(q))
                        {
                            var hit = Array.FindIndex(lines, top + 1, l => l.Contains(q, StringComparison.OrdinalIgnoreCase));
                            if (hit >= 0) top = Math.Max(0, hit - 2);
                            else AnsiConsole.MarkupLine("[yellow]Not found[/]");
                        }
                        break;
                    case ConsoleKey.Escape:
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
            .Spinner(Spectre.Console.Spinner.Known.Dots)
            .SpinnerStyle(new Style(Color.Yellow))
            .Start(message.EscapeMarkup(), _ => action());

    public static object? SpinnerResult(string message, Func<object?> action)
    {
        object? result = null;
        AnsiConsole.Status()
            .Spinner(Spectre.Console.Spinner.Known.Dots)
            .SpinnerStyle(new Style(Color.Yellow))
            .Start(message.EscapeMarkup(), _ =>
            {
                result = action();
            });
        return result;
    }

    public static void BulkProgress(string label, string[] items, Action<int, string> action) =>
        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask($"[green]{label.EscapeMarkup()}[/]", maxValue: items.Length);
                for (var i = 0; i < items.Length; i++)
                {
                    task.Description = $"{label.EscapeMarkup()}: {items[i].EscapeMarkup()}";
                    action(i, items[i]);
                    task.Increment(1);
                }
            });
}

public static class LogHelper
{
    public static string GetLogFilePath()
    {
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "profile.log");
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

    public static void LogError(string message, Exception? exception = null)
    {
        var text = exception != null ? $"{message} - Exception: {exception.Message}\n{exception.StackTrace}" : message;
        Log(text, "ERROR");
    }

    public static void LogWarning(string message) => Log(message, "WARNING");

    public static void StreamLogs(string? logPath = null)
    {
        if (string.IsNullOrWhiteSpace(logPath))
        {
            var candidates = Directory.GetFiles(".", "*.log").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).ToArray();
            if (candidates.Length == 0)
                candidates = Directory.GetFiles(Path.GetTempPath(), "*.log").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).ToArray();
            if (candidates.Length > 0)
                logPath = candidates[0].FullName;
        }
        if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
        {
            SpectrePanel.Error("No log files found to stream.");
            return;
        }
        AnsiConsole.MarkupLine($"[cyan]Streaming logs from: {logPath.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop streaming.[/]");

        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        stream.Seek(0, SeekOrigin.End);
        var cancelled = false;
        ConsoleCancelEventHandler handler = (_, e) =>
        {
            cancelled = true;
            e.Cancel = true;
        };
        Console.CancelKeyPress += handler;

        try
        {
            while (!cancelled)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    Thread.Sleep(300);
                    continue;
                }
                var color = Regex.IsMatch(line, @"error|fail|exception|err\b|critical", RegexOptions.IgnoreCase) ? "red" :
                            Regex.IsMatch(line, "warn|warning", RegexOptions.IgnoreCase) ? "yellow" :
                            Regex.IsMatch(line, @"success|ok\b|complete|done", RegexOptions.IgnoreCase) ? "green" : null;
                if (color != null)
                    AnsiConsole.MarkupLine($"[{color}]{line.EscapeMarkup()}[/]");
                else
                    AnsiConsole.WriteLine(line);
            }
        }
        finally
        {
            Console.CancelKeyPress -= handler;
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

    public static void ThreePane(string leftTitle, string[] leftItems, int leftSelected, string midTitle, string[] midItems, int midSelected, string rightTitle, string[] rightItems)
    {
        AnsiConsole.Write(new Columns(BuildPane(leftTitle, leftItems, leftSelected), BuildPane(midTitle, midItems, midSelected), BuildPane(rightTitle, rightItems, -1)));
    }

    private static Panel BuildPane(string title, string[] items, int selected)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < items.Length; i++)
            sb.AppendLine(i == selected ? $"[green bold]> {items[i].EscapeMarkup()}[/]" : $" {items[i].EscapeMarkup()}");
        return new Panel(sb.ToString())
        {
            Header = new PanelHeader($"[bold cyan]{title.EscapeMarkup()}[/]"),
            Border = BoxBorder.Rounded
        };
    }
}
