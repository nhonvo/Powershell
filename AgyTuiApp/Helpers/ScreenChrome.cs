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

    public static void RenderBanner(string? category = null, string? activeItem = null)
    {
        var acc = AgyAccountCore.GetActiveAccount();
        var displayAcc = acc;
        if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) displayAcc = $"default ({email})";
        }
        var now = DateTime.Now;
        var w = Math.Min(65, Math.Max(50, Console.WindowWidth - 2));
        var sep = new string('=', w);
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[cyan] ▄████▄  ▄████▄ [/] [bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]");
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
