namespace AgyTui.UI.Screens.Learn.Helpers;


public static class StudyStats
{
    public static void Run()
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        if (log == null || log.Sessions.Length == 0)
        {
            SpectrePanel.Info("No study sessions recorded yet.");
            return;
        }
        ShowWeeklyChart(log.Sessions);
        AnsiConsole.WriteLine();
        ShowRecentTable(log.Sessions, 10);
        AnsiConsole.MarkupLine($"\n[bold]Current streak:[/] [yellow]{GetCurrentStreak(log.Sessions)} days 🔥[/]");
        AnsiConsole.MarkupLine("[dim]Press any key...[/]");
        Console.ReadKey(true);

    }

    public static void ShowWeeklyChart(StudyLogEntry[] logs)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Minutes studied (last 7 days)[/]").RuleStyle("grey"));
        var cutoff = DateTime.Today.AddDays(-6);
        var byTopic = logs.Where(s => DateTime.TryParse(s.Date, out var d) && d >= cutoff).GroupBy(s => s.Topic).ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));
        if (byTopic.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No study data recorded in the last 7 days.[/]");
            return;
        }
        var chart = new BarChart().Width(50).Label("[bold]Minutes[/]").CenterLabel();
        foreach (var (topic, mins) in byTopic.OrderByDescending(x => x.Value)) chart.AddItem(topic.EscapeMarkup(), mins, Color.Cyan1);
        AnsiConsole.Write(chart);

    }

    public static void ShowRecentTable(StudyLogEntry[] logs, int days)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Recent Sessions[/]").RuleStyle("grey"));
        var recent = logs.TakeLast(days).Reverse().ToArray();
        var rows = recent.Select(s => new[]
        {
            s.Date+" "+s.StartTime, s.Topic, s.Activity, s.Score.Total>0?$"{s.Score.Correct}/{s.Score.Total} ({s.Score.Percentage:F0}%)":$"{s.DurationMinutes}min"
        }
        ).ToArray();
        SpectreTable.Render(["Date", "Topic", "Activity", "Score/Duration"], rows);

    }

    public static int GetCurrentStreak(StudyLogEntry[] logs, bool allowGraceDay = true)
    {
        var dates = logs.Select(s =>
        {
            if (DateTime.TryParse(s.Date, out var dt)) return dt.Date;
            return DateTime.MinValue;
        }).Where(d => d != DateTime.MinValue && d <= DateTime.Today).Distinct().OrderByDescending(d => d).ToArray();

        if (dates.Length == 0) return 0;

        int streak = 0;
        var check = DateTime.Today;

        if (dates[0] != check)
        {
            var diff = (check - dates[0]).TotalDays;
            if (diff >= 0 && (diff == 1 || (allowGraceDay && diff <= 2)))
            {
                check = dates[0];
            }
            else
            {
                return 0;
            }
        }

        for (int i = 0; i < dates.Length; i++)
        {
            if (i == 0)
            {
                streak++;
                continue;
            }
            var gap = (dates[i - 1] - dates[i]).TotalDays;
            if (gap == 1 || (allowGraceDay && gap == 2))
            {
                streak++;
            }
            else
            {
                break;
            }
        }
        return streak;
    }

}
public static class DailyGoals
{
    public static void Show()
    {
        var data = LoadToday();
        AnsiConsole.Write(new Rule($"[bold cyan]Daily Goals: {data.Date}[/]").RuleStyle("grey"));
        if (data.Targets.Length == 0)
        {
            AnsiConsole.MarkupLine("[dim] No goals set today. Press n to add.[/]");
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var t in data.Targets)
            {
                bool done = t.Completed >= t.Count;
                int bars = t.Count > 0 ? (int)(t.Completed * 16.0 / t.Count) : 0;
                var bar = new string('█', Math.Min(16, bars)) + new string('░', Math.Max(0, 16 - bars));
                sb.AppendLine($" {(done ? "[green]✓[/]" : "[red]✗[/]")} {t.Topic.EscapeMarkup(),-12} {t.Activity.EscapeMarkup(),-12} [{bar}] {t.Completed}/{t.Count}");
            }
            int complete = data.Targets.Count(t => t.Completed >= t.Count);
            AnsiConsole.Write(new Panel(sb.ToString().TrimEnd())
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Cyan1),
                Padding = new Padding(1, 0)
            }
            );
            AnsiConsole.MarkupLine($"[dim] {complete} / {data.Targets.Length} goals complete[/]");
        }

    }

    public static void SetGoal(string topic, string activity, int count)
    {
        var data = LoadToday();
        var targets = data.Targets.ToList();
        var existing = targets.FindIndex(t => t.Topic == topic && t.Activity == activity);
        if (existing >= 0) targets[existing] = targets[existing] with
        {
            Count = count
        }
        ;

        else targets.Add(new GoalTarget(topic, activity, count, 0));
        SaveToday(data with
        {
            Targets = [.. targets]
        }
        );
        SpectrePanel.Success($"Goal set: {topic}/{activity} = {count}");

    }

    public static void UpdateProgress(string topic, string activity, int completedCount)
    {
        var data = LoadToday();
        var targets = data.Targets.ToList();
        var idx = targets.FindIndex(t => t.Topic == topic && t.Activity == activity);
        if (idx >= 0) targets[idx] = targets[idx] with
        {
            Completed = completedCount
        }
        ;
        SaveToday(data with
        {
            Targets = [.. targets]
        }
        );

    }

    public static bool AllComplete() => LoadToday().Targets.All(t => t.Completed >= t.Count);

    private static DailyGoalData LoadToday()
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var goals = log?.DailyGoals;
        if (goals != null && goals.Date == today) return goals;
        return new DailyGoalData(today, []);

    }

    private static void SaveToday(DailyGoalData data)
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile) ?? new StudyLogFile(null, []);
        LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, log with
        {
            DailyGoals = data
        }
        );

    }

}
public static class StudyStreak
{
    public static StreakData Calculate()
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var sessions = log?.Sessions ?? [];
        var dates = sessions.Select(s => s.Date).Distinct().Where(d => DateTime.TryParse(d, out _)).OrderByDescending(d => d).ToArray();
        int current = StudyStats.GetCurrentStreak(sessions);
        int best = 0, run = 0;
        for (int i = 0; i < dates.Length; i++)
        {
            if (i == 0) run = 1;
            else
            {
                var gap = (DateTime.Parse(dates[i - 1]) - DateTime.Parse(dates[i])).TotalDays;
                if (gap == 1 || gap == 2) run++;
                else run = 1;
            }
            if (run > best) best = run;
        }
        var lastActive = dates.Length > 0 ? dates[0] : "Never";
        var weekAgo = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
        int daysThisWeek = dates.Count(d => string.Compare(d, weekAgo, StringComparison.Ordinal) >= 0);
        return new StreakData(current, best, lastActive, daysThisWeek);

    }

    public static void ShowPanel()
    {
        var s = Calculate();
        AnsiConsole.Write(new Panel($"🔥 Current streak : [bold yellow]{s.Current} days[/]\n" + $"🏆 Best streak : [bold green]{s.Best} days[/]\n" + $"📅 Last active : [cyan]{s.LastActive}[/]\n" + $"📊 This week : [dim]{s.DaysThisWeek} / 7 days active[/]")
        {
            Header = new PanelHeader("[bold cyan]Study Streak[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(1, 1)
        }
        );

    }

    public static bool StudiedToday()
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return log?.Sessions.Any(s => s.Date == today) ?? false;

    }

}
public static class DueReview
{
    public static void Show()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Due for Review[/]").RuleStyle("grey"));
        var groups = GetAllDue().GroupBy(d => d.Topic).ToArray();
        if (groups.Length == 0)
        {
            SpectrePanel.Success("Nothing due for review today!");
            return;
        }
        var rows = groups.Select(g =>
        {
            int due = g.Count(d => !d.Overdue);
            int over = g.Count(d => d.Overdue);
            var next = g.Where(d => !d.Overdue).OrderBy(d => d.NextReview).FirstOrDefault();
            return new[]
            {
                g.Key, due.ToString(), over.ToString(), next?.NextReview.ToString("yyyy-MM-dd")??"today"
            }
            ;
        }
        ).ToArray();
        SpectreTable.Render(["Topic", "Due Today", "Overdue", "Next Due"], rows);
        AnsiConsole.MarkupLine($"\n[dim]Total: {groups.Sum(g => g.Count())} items due · {groups.Sum(g => g.Count(d => d.Overdue))} overdue[/]");

    }

    public static DueItem[] GetAllDue()
    {
        var all = new List<DueItem>();
        ScanDecks(all);
        ScanJlpt(all);
        return [.. all];

    }

    public static DueItem[] GetDueByTopic(string topic) => GetAllDue().Where(d => d.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static void ScanDecks(List<DueItem> all)
    {
        if (!Directory.Exists(LearnDataPaths.DecksDir)) return;
        foreach (var f in Directory.GetFiles(LearnDataPaths.DecksDir, "*.json"))
        {
            var deck = LearnDataPaths.LoadJson<DeckFile>(f);
            if (deck == null) continue;
            foreach (var card in deck.Cards.Where(c => SpacedRepetitionEngine.IsDueToday(c.Sr))) all.Add(new DueItem(deck.Meta.Topic, card.Id, card.Front, card.Sr.NextReview ?? DateTime.Today, card.Sr.NextReview != null && card.Sr.NextReview.Value.Date < DateTime.Today));
        }

    }

    private static void ScanJlpt(List<DueItem> all)
    {
        if (!Directory.Exists(LearnDataPaths.JlptDir)) return;
        foreach (var f in Directory.GetFiles(LearnDataPaths.JlptDir, "*.json"))
        {
            var jlpt = LearnDataPaths.LoadJson<JlptFile>(f);
            if (jlpt == null) continue;
            foreach (var w in jlpt.Words.Where(x => SpacedRepetitionEngine.IsDueToday(x.Sr))) all.Add(new DueItem($"JLPT {jlpt.JlptLevel}", w.Id, w.Word, w.Sr.NextReview ?? DateTime.Today, w.Sr.NextReview != null && w.Sr.NextReview.Value.Date < DateTime.Today));
        }

    }

}
public static class ProgressDashboard
{
    public static void Show()
    {
        AnsiConsole.Clear();
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var sessions = log?.Sessions ?? [];
        StudyStats.ShowWeeklyChart(sessions);
        AnsiConsole.WriteLine();
        ShowMasteryTree(sessions);
        AnsiConsole.WriteLine();
        StudyStats.ShowRecentTable(sessions, 5);
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
        Console.ReadKey(true);

    }

    public static void ShowMasteryTree(StudyLogEntry[] sessions)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Mastery Tree[/]").RuleStyle("grey"));
        var tree = new Tree("[bold cyan]Topics[/]");
        foreach (var topic in sessions.Select(s => s.Topic).Distinct())
        {
            var m = GetMastery(topic);
            var node = tree.AddNode($"[bold]{topic.EscapeMarkup()}[/]");
            node.AddNode($"[dim]{m.Total} total · [green]{m.Mastered} mastered[/] · [yellow]{m.Learning} learning[/] · [dim]{m.NewItems} new[/]");
        }
        AnsiConsole.Write(tree);

    }

    private static MasteryData GetMastery(string topic)
    {
        int total = 0, mastered = 0, learning = 0, newItems = 0;
        if (!Directory.Exists(LearnDataPaths.DecksDir)) return new(topic, 0, 0, 0, 0);
        foreach (var f in Directory.GetFiles(LearnDataPaths.DecksDir, "*.json"))
        {
            var deck = LearnDataPaths.LoadJson<DeckFile>(f);
            if (deck == null || !deck.Meta.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var c in deck.Cards)
            {
                total++;
                if (c.Sr.Status == "mastered") mastered++;

                else if (c.Sr.Status == "learning" || c.Sr.Status == "review") learning++;

                else newItems++;
            }
        }
        return new(topic, total, mastered, learning, newItems);

    }

}
public static class WeakItemsQueue
{
    public static void ShowPreSessionReview(string topic)
    {
        var items = GetWeakItems(topic);
        if (items.Length == 0) return;
        AnsiConsole.Write(new Panel($"You have [yellow]{items.Length}[/] weak items from your last session:\n\n" + string.Join("\n", items.Take(5).Select((w, i) => $" {i + 1}. {w.FrontText.EscapeMarkup()} [dim]({w.Topic} — failed {w.FailCount}x)[/]")) + (items.Length > 5 ? $"\n [dim]... and {items.Length - 5} more[/]" : "") + "\n\n[dim]These will be shown first in your session.[/]")
        {
            Header = new PanelHeader("[yellow]⚠ Review Needed[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(1, 1)
        }
        );
        if (!AnsiConsole.Confirm("Start session with weak items first?", defaultValue: true)) ClearWeakItems(topic);

    }

    public static WeakItem[] GetWeakItems(string topic)
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        return log?.Sessions.Where(s => s.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase) && s.WeakItems.Length > 0).SelectMany(s => s.WeakItems).GroupBy(w => w).Select(g => new WeakItem(topic, g.Key, g.Key, g.Count())).OrderByDescending(w => w.FailCount).ToArray() ?? [];

    }

    public static void AddWeakItem(string topic, string itemId)
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        if (log == null || log.Sessions.Length == 0) return;
        var lastSession = log.Sessions.LastOrDefault(s => s.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase));
        if (lastSession != null)
        {
            if (!lastSession.WeakItems.Contains(itemId))
            {
                var newWeak = lastSession.WeakItems.Append(itemId).ToArray();
                int idx = Array.IndexOf(log.Sessions, lastSession);
                log.Sessions[idx] = lastSession with { WeakItems = newWeak };
                LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, log);
            }
        }
    }

    public static void ClearWeakItems(string topic)
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        if (log == null) return;
        bool changed = false;
        for (int i = 0; i < log.Sessions.Length; i++)
        {
            if (log.Sessions[i].Topic.Equals(topic, StringComparison.OrdinalIgnoreCase) && log.Sessions[i].WeakItems.Length > 0)
            {
                log.Sessions[i] = log.Sessions[i] with { WeakItems = Array.Empty<string>() };
                changed = true;
            }
        }
        if (changed)
        {
            LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, log);
        }
    }

}

public sealed record WordEntry(string Date, string Word, string Pronunciation, string PartOfSpeech, string Definition, string Example, string[] Tags);

public sealed record WordBankFile(WordEntry[] Words);

public static class WordOfDay
{
    public static WordEntry? Pick()
    {
        var bank = LearnDataPaths.LoadJson<WordBankFile>(LearnDataPaths.WordBankFile);
        if (bank == null || bank.Words.Length == 0) return null;
        var idx = DateTime.Today.DayOfYear % bank.Words.Length;
        return bank.Words[idx];

    }

    public static void Render(WordEntry word)
    {
        AnsiConsole.Write(new Rule("[bold cyan]📖 Word of the Day[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"\n [bold green]{word.Word.EscapeMarkup()}[/] [dim]{word.Pronunciation.EscapeMarkup()}[/] [yellow]{word.PartOfSpeech.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($" {word.Definition.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" [italic dim]\"{word.Example.EscapeMarkup()}\"[/]");
        AnsiConsole.WriteLine();

    }

}
