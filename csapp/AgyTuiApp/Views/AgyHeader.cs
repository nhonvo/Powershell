using System;
using System.IO;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class AgyHeader
{
    public static void ShowSplash()
    {
        AnsiConsole.Clear();
        var splashW = Math.Min(65, Math.Max(50, Console.WindowWidth - 2));
        var sep = new string('=', splashW);
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.Write(new FigletText("AGY TUI").Centered().Color(Color.Green));
        AnsiConsole.Write(new Rule("[bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        var active = AgyAccountCore.GetActiveAccount();
        var stats = AgyAccountCore.GetAccountStats(active);
        var quota = AgyAccountCore.CalculateRollingQuotas(active);
        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadLeft(4));
        grid.AddRow($"[cyan]Active account[/] : [green bold]{active.EscapeMarkup()}[/]");
        grid.AddRow($"[cyan]Login status[/] : {(stats.TokenStatus == "Logged In" ? "[green]● Logged In[/]" : "[red]○ Not Logged In[/]")}");
        grid.AddRow($"[cyan]Weekly quota[/] : {AgyAccountCore.GetProgressBar(quota.RemainingWeekly).EscapeMarkup()}");
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        try
        {
            var w = WordOfDay.Pick();
            if (w != null) WordOfDay.Render(w);
        }
        catch
        {
        }
        try
        {
            StudyStreak.ShowPanel();
        }
        catch
        {
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim] Press Enter to continue[/]");
        Console.ReadKey(true);
        AnsiConsole.Clear();
    }
}
