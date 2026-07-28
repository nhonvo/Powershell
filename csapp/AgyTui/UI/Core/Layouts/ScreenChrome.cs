namespace AgyTui.UI.Core.Layouts;

public static class ScreenChrome
{
    public static IAnsiConsole? OverrideConsole { get; set; }
    private static IAnsiConsole ConsoleInstance => OverrideConsole ?? AnsiConsole.Console;

    public static readonly Color AccentColor = Color.Cyan1;
    public static readonly Color SuccessColor = Color.Green;
    public static readonly Color WarningColor = Color.Yellow;
    public static readonly Color ErrorColor = Color.Red;
    public static readonly Color MutedColor = Color.Grey;
    public static readonly Color LiveColor = Color.Blue;

    public static string Accent(string text) => $"[cyan]{text.EscapeMarkup()}[/]";
    public static string Success(string text) => $"[green]{text.EscapeMarkup()}[/]";
    public static string Warning(string text) => $"[yellow]{text.EscapeMarkup()}[/]";
    public static string Error(string text) => $"[red]{text.EscapeMarkup()}[/]";
    public static string Muted(string text) => $"[grey]{text.EscapeMarkup()}[/]";
    public static string Live(string text) => $"[blue]{text.EscapeMarkup()}[/]";

    public static void ResetRenderState()
    {
    }

    public static void HideCursor()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.CursorVisible = false;
            Console.Write("\x1b[?25l");
        }
        catch { }
    }

    public static void ShowCursor()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.CursorVisible = true;
            Console.Write("\x1b[?25h");
        }
        catch { }
    }

    public static void EnableMouseTracking()
    {
        // Keep mouse tracking disabled by default so users can select and copy text with mouse
    }

    public static void DisableMouseTracking()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.Write("\x1b[?1000l\x1b[?1006l");
        }
        catch { }
    }

    public static bool CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var base64 = Convert.ToBase64String(bytes);
            Console.Write($"\x1b]52;c;{base64}\a");

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"Set-Clipboard -Value '{text.Replace("'", "''")}'\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var proc = System.Diagnostics.Process.Start(psi);
                    proc?.WaitForExit(500);
                }
                catch { }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static (ConsoleKeyInfo Key, bool IsScrollUp, bool IsScrollDown) ReadKeyWithMouse()
    {
        var key = Console.ReadKey(true);
        bool isScrollUp = false;
        bool isScrollDown = false;

        if (key.KeyChar == '\x1b' && Console.KeyAvailable)
        {
            var sb = new System.Text.StringBuilder("\x1b");
            while (Console.KeyAvailable)
            {
                var next = Console.ReadKey(true);
                sb.Append(next.KeyChar);
                if (next.KeyChar == 'M' || next.KeyChar == 'm' || sb.Length > 20) break;
            }
            var seq = sb.ToString();
            if (seq.Contains("[<64;") || seq.Contains("[<0;") || seq.Contains("[M "))
            {
                isScrollUp = true;
            }
            else if (seq.Contains("[<65;") || seq.Contains("[<1;") || seq.Contains("[M!"))
            {
                isScrollDown = true;
            }
        }

        return (key, isScrollUp, isScrollDown);
    }

    public static void ClearTrailingLines()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.Write("\x1b[J");
        }
        catch { }
    }

    public static void WriteLineSmooth(string markup)
    {
        MarkupLineEl(markup);
    }

    public static void WriteSmooth(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (OverrideConsole != null)
        {
            ConsoleInstance.Write(text);
            return;
        }
        var smoothText = text.Replace("\r\n", "\x1b[K\r\n").Replace("\n", "\x1b[K\n");
        Console.Write(smoothText);
    }

    public static void WriteSmooth(Spectre.Console.Rendering.IRenderable renderable)
    {
        try { ConsoleInstance.Write(renderable); } catch { }
    }

    public static void RenderFrame(Action drawBody, bool forceClear = false)
    {
        HideCursor();
        try
        {
            try
            {
                if (forceClear)
                {
                    if (OverrideConsole != null) OverrideConsole.Clear();
                    else Console.Write("\x1b[2J\x1b[H");
                }
                else
                {
                    if (OverrideConsole == null) Console.SetCursorPosition(0, 0);
                }
            }
            catch
            {
                try { ConsoleInstance.Clear(); } catch { }
            }
            drawBody();
            ClearTrailingLines();
        }
        finally
        {
            ShowCursor();
        }
    }

    private static void MarkupLineEl(string markup)
    {
        try
        {
            ConsoleInstance.Markup(markup);
            if (OverrideConsole != null) ConsoleInstance.WriteLine();
            else Console.Write("\x1b[K\n");
        }
        catch
        {
            try { Console.WriteLine(); } catch { }
        }
    }

    public static void RenderBanner(string? category = null, string? activeItem = null, bool forceClear = false, string? footerHint = null)
    {
        HideCursor();
        var acc = AgyAccountCore.GetActiveAccount() ?? "default";
        var displayAcc = acc;
        if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) displayAcc = $"default ({email})";
        }
        var now = DateTime.Now;
        var winWidth = 80;
        var winHeight = 30;
        try
        {
            winWidth = Console.WindowWidth;
            winHeight = Console.WindowHeight;
        }
        catch { }

        var w = Math.Max(50, winWidth - 2);
        var sep = new string('=', w);

        if (forceClear)
        {
            if (OverrideConsole != null)
            {
                try { OverrideConsole.Clear(); } catch { }
            }
            else
            {
                try { Console.Write("\x1b[2J\x1b[H"); } catch { try { ConsoleInstance.Clear(); } catch { } }
            }
        }

        var titleIcon = (Icons.IsUtf8Supported ? "🛸" : "[AGY]").EscapeMarkup();

        if ((winHeight > 0 && winHeight < 45) || (winWidth > 0 && winWidth < 60))
        {
            MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
            var accText = $"[dim]Account:[/] [green bold]{displayAcc.EscapeMarkup()}[/]";
            var timeText = $"[dim]Time:[/] [yellow]{now:HH:mm}[/]";
            MarkupLineEl($" [bold green]{titleIcon} Control Center v3.0[/] | {accText} | {timeText}");
            if (!string.IsNullOrEmpty(category))
            {
                var breadcrumb = $" [bold cyan]Home[/] [dim]>[/] [bold green]{category.EscapeMarkup()}[/]";
                if (!string.IsNullOrEmpty(activeItem)) breadcrumb += $" [dim]>[/] [yellow]{activeItem.EscapeMarkup()}[/]";
                MarkupLineEl(breadcrumb);
            }
            MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
            return;
        }

        MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
        MarkupLineEl($"[cyan] ▄████▄  ▄████▄ [/] [bold green]{titleIcon} Powershell Profile Control Center v3.0 {titleIcon}[/]");
        MarkupLineEl("[cyan] █▀  ▀   █▀  ▀  [/] [dim]System dashboard and control suite.[/]");
        MarkupLineEl("[cyan] █       █      [/]");
        MarkupLineEl($"[cyan] █▄  ▄   █▄  ▄  [/] [dim]Active Account:[/] [green bold]{displayAcc.EscapeMarkup()}[/]");
        MarkupLineEl($"[cyan] ▀████▀  ▀████▀ [/] [dim]Time:[/] [yellow]{now:yyyy-MM-dd HH:mm}[/]");

        if (!string.IsNullOrEmpty(category))
        {
            var breadcrumb = $" [bold cyan]Home[/] [dim]>[/] [bold green]{category.EscapeMarkup()}[/]";
            if (!string.IsNullOrEmpty(activeItem))
            {
                breadcrumb += $" [dim]>[/] [yellow]{activeItem.EscapeMarkup()}[/]";
            }
            MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
            MarkupLineEl(breadcrumb);
        }

        MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
        MarkupLineEl(footerHint ?? "[dim] [[Tab/→]] Navigate Panes | [[←/Esc]] Go Back | [[Enter]] Select & Run[/]");
        MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
    }
}
