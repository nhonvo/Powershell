using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public record TopicItem(string Key, string DisplayName);

public static class SubPageTopicNavigator
{
    public static readonly IReadOnlyList<TopicItem> DefaultTopics = new TopicItem[]
    {
        new("jp", "jp (Japanese / Language)"),
        new("en", "en (English Vocabulary)"),
        new("cs", "cs (C# Quiz)"),
        new("dsa", "dsa (Data Structures & Algorithms)"),
        new("interview", "interview (Question Bank & STAR)"),
        new("[Type Custom Topic...]", "[Type Custom Topic...]")
    };

    public static List<TopicItem> GetFilteredTopics(string searchBuffer)
    {
        if (string.IsNullOrWhiteSpace(searchBuffer))
            return DefaultTopics.ToList();

        return DefaultTopics
            .Where(t => t.Key.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase) ||
                        t.DisplayName.Contains(searchBuffer, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static bool HandleSelection(string mode, string searchBuffer, int detailsSel)
    {
        var topics = GetFilteredTopics(searchBuffer);
        if (detailsSel < 0 || detailsSel >= topics.Count) return false;

        var selectedTopic = topics[detailsSel].Key;
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
        var topics = GetFilteredTopics(searchBuffer);
        for (var i = 0; i < topics.Count; i++)
        {
            var isSelected = (i == selIdx);
            var prefix = isSelected ? "[green bold]> [/]" : "  ";
            grid.AddRow(new Markup($"{prefix}{topics[i].DisplayName.EscapeMarkup()}"));
        }
        grid.AddRow(new Markup("\n[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]"));
        return grid;
    }
}
