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

    public static void ClearTrailingLines()
    {
        try
        {
            Console.Write("\x1b[J");
        }
        catch {}
    }

    public static void RenderBanner(string? category = null, string? activeItem = null, bool forceClear = false)
    {
        var acc = AgyAccountCore.GetActiveAccount() ?? "default";
        var displayAcc = acc;
        if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) displayAcc = $"default ({email})";
        }
        var now = DateTime.Now;
        var winWidth = 80;
        try { winWidth = Console.WindowWidth; } catch {}
        var w = Math.Min(65, Math.Max(50, winWidth - 2));
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

        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        var titleIcon = Icons.IsUtf8Supported ? "🛸" : "[AGY]";
        AnsiConsole.MarkupLine($"[cyan] ▄████▄  ▄████▄ [/] [bold green]{titleIcon} Powershell Profile Control Center v3.0 {titleIcon}[/]");
        AnsiConsole.MarkupLine("[cyan] █▀  ▀   █▀  ▀  [/] [dim]System dashboard and control suite.[/]");
        AnsiConsole.MarkupLine("[cyan] █       █      [/]");
        AnsiConsole.MarkupLine($"[cyan] █▄  ▄   █▄  ▄  [/] [dim]Active Account:[/] [green bold]{displayAcc.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"[cyan] ▀████▀  ▀████▀ [/] [dim]Time:[/] [yellow]{now:yyyy-MM-dd HH:mm}[/]");

        if (!string.IsNullOrEmpty(category))
        {
            var breadcrumb = $" [bold cyan]Home[/] [dim]>[/] [bold green]{category.EscapeMarkup()}[/]";
            if (!string.IsNullOrEmpty(activeItem))
            {
                breadcrumb += $" [dim]>[/] [yellow]{activeItem.EscapeMarkup()}[/]";
            }
            AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine(breadcrumb);
        }

        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[dim] [[Tab/→]] Navigate Panes | [[←/Esc]] Go Back | [[Enter]] Select & Run[/]");
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
    }
}
