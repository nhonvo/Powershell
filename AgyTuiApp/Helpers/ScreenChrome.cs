using System;
using Spectre.Console;

namespace AgyTui;

public static class ScreenChrome
{
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
        try
        {
            Console.CursorVisible = false;
            Console.Write("\x1b[?25l");
        }
        catch {}
    }

    public static void ShowCursor()
    {
        try
        {
            Console.CursorVisible = true;
            Console.Write("\x1b[?25h");
        }
        catch {}
    }

    public static void ClearTrailingLines()
    {
        try
        {
            Console.Write("\x1b[J");
        }
        catch {}
    }

    private static void MarkupLineEl(string markup)
    {
        try
        {
            AnsiConsole.Markup(markup);
            Console.Write("\x1b[K\n");
        }
        catch
        {
            try { Console.WriteLine(); } catch {}
        }
    }

    public static void RenderBanner(string? category = null, string? activeItem = null, bool forceClear = false)
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
        catch {}

        var w = Math.Max(50, winWidth - 2);
        var sep = new string('=', w);

        try
        {
            if (forceClear)
            {
                AnsiConsole.Clear();
            }
            else
            {
                Console.SetCursorPosition(0, 0);
            }
        }
        catch
        {
            try { AnsiConsole.Clear(); } catch {}
        }

        var titleIcon = Icons.IsUtf8Supported ? "🛸" : "[AGY]";

        if (winHeight > 0 && winHeight < 28)
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
        MarkupLineEl("[dim] [[Tab/→]] Navigate Panes | [[←/Esc]] Go Back | [[Enter]] Select & Run[/]");
        MarkupLineEl($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
    }
}
