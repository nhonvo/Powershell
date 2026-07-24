using System;
using System.Linq;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui;

public static class SubPageTopicNavigator
{
    public static bool HandleSelection(string mode, string searchBuffer, int detailsSel)
    {
        var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            topics = topics.Where(t => t.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (detailsSel < 0 || detailsSel >= topics.Length) return false;

        var selectedTopic = topics[detailsSel];
        if (selectedTopic == "[Type Custom Topic...]")
        {
            Console.CursorVisible = true;
            selectedTopic = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
            Console.CursorVisible = false;
        }
        if (!string.IsNullOrEmpty(selectedTopic))
        {
            Console.CursorVisible = true;
            if (mode == "learn") LearnRouter.StartLearning(selectedTopic);
            else if (mode == "session") StudySession.Run(selectedTopic);
            else if (mode == "weak") WeakItemsQueue.ShowPreSessionReview(selectedTopic);
            Console.CursorVisible = false;
        }
        return true;
    }

    public static IRenderable Render(Grid grid, string mode, string searchBuffer, int selIdx)
    {
        grid.AddRow(new Markup($"[cyan bold]Select Topic for {mode.ToUpperInvariant()}:[/]\n"));
        if (!string.IsNullOrEmpty(searchBuffer))
        {
            grid.AddRow(new Markup($"[yellow]Search:[/] [white]{searchBuffer.EscapeMarkup()}[/]_\n"));
        }
        var allTopics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
        var topics = string.IsNullOrEmpty(searchBuffer)
            ? allTopics
            : allTopics.Where(t => t.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
        for (var i = 0; i < topics.Length; i++)
        {
            var isSelected = (i == selIdx);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            grid.AddRow(new Markup($"{prefix}{topics[i].EscapeMarkup()}"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
        return grid;
    }
}
