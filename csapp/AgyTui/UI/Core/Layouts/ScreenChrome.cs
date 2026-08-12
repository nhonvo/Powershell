namespace AgyTui.UI.Core.Layouts;

public class ScreenChromeService : IScreenChrome
{
    private readonly IAgyAccountStore? _accountStore;

    public ScreenChromeService(IAgyAccountStore? accountStore = null)
    {
        _accountStore = accountStore;
    }

    public IAnsiConsole? OverrideConsole { get; set; }
    private IAnsiConsole ConsoleInstance => OverrideConsole ?? AnsiConsole.Console;

    public string Accent(string text) => $"[cyan]{text.EscapeMarkup()}[/]";
    public string Success(string text) => $"[green]{text.EscapeMarkup()}[/]";
    public string Warning(string text) => $"[yellow]{text.EscapeMarkup()}[/]";
    public string Error(string text) => $"[red]{text.EscapeMarkup()}[/]";
    public string Muted(string text) => $"[grey]{text.EscapeMarkup()}[/]";
    public string Live(string text) => $"[blue]{text.EscapeMarkup()}[/]";

    public void RenderHeader(string title, string subtitle = "")
    {
        RenderBanner(title, subtitle);
    }

    public void RenderFooter(string tip = "")
    {
        MarkupLineEl(AgyUiComponents.RenderFooterNote(tip).ToString() ?? string.Empty);
    }

    public void HideCursor()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.CursorVisible = false;
            Console.Write("\x1b[?25l");
        }
        catch { }
    }

    public void ShowCursor()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.CursorVisible = true;
            Console.Write("\x1b[?25h");
        }
        catch { }
    }

    public void ClearTrailingLines()
    {
        if (OverrideConsole != null) return;
        try
        {
            Console.Write("\x1b[J");
        }
        catch { }
    }

    public void WriteLineSmooth(string markup) => MarkupLineEl(markup);

    public void WriteSmooth(string text)
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

    public void WriteSmooth(Spectre.Console.Rendering.IRenderable renderable)
    {
        try { ConsoleInstance.Write(renderable); } catch { }
    }

    public void RenderFrame(Action drawBody, bool forceClear = false)
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

    private void MarkupLineEl(string markup)
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

    public void RenderBanner(string? category = null, string? activeItem = null, bool forceClear = false, string? footerHint = null)
    {
        HideCursor();
        var acc = _accountStore?.GetActiveAccount() ?? "default";
        var displayAcc = acc;
        if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = _accountStore?.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) displayAcc = $"default ({email})";
        }
        var now = DateTime.Now;

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
        var accText = $"[dim]Account:[/] [green bold]{displayAcc.EscapeMarkup()}[/]";
        var timeText = $"[dim]Time:[/] [yellow]{now:HH:mm}[/]";

        var headerLine = $" [bold green]{titleIcon} Powershell Profile Control Center v3.0[/] | {accText} | {timeText}";
        if (!string.IsNullOrEmpty(category))
        {
            headerLine += $" | [bold cyan]Home[/] [dim]>[/] [bold green]{category.EscapeMarkup()}[/]";
            if (!string.IsNullOrEmpty(activeItem)) headerLine += $" [dim]>[/] [yellow]{activeItem.EscapeMarkup()}[/]";
        }

        MarkupLineEl(headerLine);
    }
}

public static class ScreenChrome
{
    private static readonly ScreenChromeService _service = new();
    public static IScreenChrome Instance => _service;

    public static IAnsiConsole? OverrideConsole
    {
        get => _service.OverrideConsole;
        set => _service.OverrideConsole = value;
    }

    public static readonly Color AccentColor = Color.Cyan1;
    public static readonly Color SuccessColor = Color.Green;
    public static readonly Color WarningColor = Color.Yellow;
    public static readonly Color ErrorColor = Color.Red;
    public static readonly Color MutedColor = Color.Grey;
    public static readonly Color LiveColor = Color.Blue;

    public static string Accent(string text) => _service.Accent(text);
    public static string Success(string text) => _service.Success(text);
    public static string Warning(string text) => _service.Warning(text);
    public static string Error(string text) => _service.Error(text);
    public static string Muted(string text) => _service.Muted(text);
    public static string Live(string text) => _service.Live(text);

    public static void ResetRenderState() { }
    public static void HideCursor() => _service.HideCursor();
    public static void ShowCursor() => _service.ShowCursor();
    public static void EnableMouseTracking() { }
    public static void DisableMouseTracking() { }
    public static bool CopyToClipboard(string text) => true;
    public static (ConsoleKeyInfo Key, bool IsScrollUp, bool IsScrollDown) ReadKeyWithMouse() => (Console.ReadKey(true), false, false);
    public static void ClearTrailingLines() => _service.ClearTrailingLines();
    public static void WriteLineSmooth(string markup) => _service.WriteLineSmooth(markup);
    public static void WriteSmooth(string text) => _service.WriteSmooth(text);
    public static void WriteSmooth(Spectre.Console.Rendering.IRenderable renderable) => _service.WriteSmooth(renderable);
    public static void RenderFrame(Action drawBody, bool forceClear = false) => _service.RenderFrame(drawBody, forceClear);
    public static void RenderBanner(string? category = null, string? activeItem = null, bool forceClear = false, string? footerHint = null) => _service.RenderBanner(category, activeItem, forceClear, footerHint);
}

