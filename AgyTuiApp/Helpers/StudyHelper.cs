using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class LearnDataPaths
{
    public static string LearnRoot => System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount()), "learn");

    public static string DecksDir => System.IO.Path.Combine(LearnRoot, "decks");

    public static string VocabDir => System.IO.Path.Combine(LearnRoot, "vocab");

    public static string JlptDir => System.IO.Path.Combine(LearnRoot, "jlpt");

    public static string SnippetsDir => System.IO.Path.Combine(LearnRoot, "snippets");

    public static string SheetsDir => System.IO.Path.Combine(LearnRoot, "cheatsheets");

    public static string WordBankFile => System.IO.Path.Combine(LearnRoot, "word_bank.json");

    public static string KanaFile => System.IO.Path.Combine(LearnRoot, "kana.json");

    public static string KanjiFile => System.IO.Path.Combine(LearnRoot, "kanji.json");

    public static string ComplexityFile => System.IO.Path.Combine(LearnRoot, "complexity.json");

    public static string QuizFile => System.IO.Path.Combine(LearnRoot, "csharp_quiz.json");

    public static string InterviewFile => System.IO.Path.Combine(LearnRoot, "interview_questions.json");

    public static string StarFile => System.IO.Path.Combine(LearnRoot, "star_answers.json");

    public static string ProblemsFile => System.IO.Path.Combine(LearnRoot, "problems.json");

    public static string StudyLogFile => System.IO.Path.Combine(LearnRoot, "study_log.json");

    public static string ObsidianCfgFile => System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount()), "obsidian_config.json");

    public static string ResourcesIndex => System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount()), "resources", "index.json");

    public static void EnsureDirectories()
    {
        foreach (var d in new[]
        {
            LearnRoot, DecksDir, VocabDir, JlptDir, SnippetsDir, SheetsDir
        }
        ) Directory.CreateDirectory(d);
        SeedDefaultData();
    }

    public static void SeedDefaultData()
    {
        // 1. C# Quiz questions
        if (!File.Exists(QuizFile))
        {
            var defaultQuestions = new[]
            {
                new QuizQuestion("cs-1", "C# Basics", 1, "What is the size of an int in C#?", new[] { "2 bytes", "4 bytes", "8 bytes", "Depends on platform" }, 1, "In C#, 'int' maps directly to System.Int32, which is always 32-bit (4 bytes) regardless of platform or architecture.", null, new[] { "basics", "types" }),
                new QuizQuestion("cs-2", "C# Basics", 1, "Which keyword is used to declare a constant in C#?", new[] { "const", "readonly", "static", "let" }, 0, "The 'const' keyword declares a compile-time constant, whereas 'readonly' declares a run-time constant.", null, new[] { "basics", "keywords" }),
                new QuizQuestion("cs-3", "OOP", 2, "Which access modifier allows access within the same assembly or subclass?", new[] { "private", "protected", "internal", "protected internal" }, 3, "The 'protected internal' modifier allows access within the defining assembly, or from derived classes in any assembly.", null, new[] { "oop", "access-modifiers" })
            };
            SaveJson(QuizFile, new QuizFile(defaultQuestions));
        }

        // 2. Flashcard deck
        var defaultDeckFile = System.IO.Path.Combine(DecksDir, "general.json");
        if (!File.Exists(defaultDeckFile))
        {
            var defaultMeta = new DeckMeta("deck-1", "General Developer Deck", "English", "General Dev", "Beginner", new[] { "internal" }, DateTime.UtcNow.ToString("o"), 1);
            var defaultCards = new[]
            {
                new FlashCard("card-1", "What is SOLID?", "SOLID is an acronym for five design principles: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion.", "Software design acronym", "S-O-L-I-D principles", "Use SOLID principles to write clean code.", new[] { "oop", "architecture" }, 2, NewCardState()),
                new FlashCard("card-2", "Explain Git merge vs. rebase.", "Merge keeps full commit history with merge commits. Rebase rewrites commit history on top of the target branch for a linear project history.", "Git workflow strategy", "Rebase = rewrite history, Merge = preserve history", "Rebasing keeps the commit tree clean.", new[] { "git" }, 2, NewCardState())
            };
            SaveJson(defaultDeckFile, new DeckFile(defaultMeta, defaultCards));
        }

        // 3. JLPT N5 words
        var defaultJlptFile = System.IO.Path.Combine(JlptDir, "n5.json");
        if (!File.Exists(defaultJlptFile))
        {
            var defaultWords = new[]
            {
                new JlptWord("jlpt-1", "日本語", "にほんご", "nihongo", "Japanese language", "Noun", "N5", "日本語を勉強します。", "I study Japanese.", new[] { "language" }, NewCardState()),
                new JlptWord("jlpt-2", "食べる", "たべる", "taberu", "To eat", "Verb", "N5", "リンゴを食べます。", "I eat an apple.", new[] { "verbs" }, NewCardState())
            };
            SaveJson(defaultJlptFile, new JlptFile("N5", defaultWords));
        }

        // 4. Vocab Words (Intermediate)
        var defaultVocabFile = System.IO.Path.Combine(VocabDir, "intermediate.json");
        if (!File.Exists(defaultVocabFile))
        {
            var defaultWords = new[]
            {
                new VocabWord("vocab-1", "ubiquitous", "yoo-bik-wi-tuhs", "Adjective", "Existing or being everywhere at the same time; constantly encountered.", "Mobile phones are ubiquitous today.", new[] { "omnipresent", "pervasive" }, new[] { "rare", "scarce" }, 3, new[] { "adjectives" }, NewCardState()),
                new VocabWord("vocab-2", "pragmatic", "prag-mat-ik", "Adjective", "Dealing with things sensibly and realistically in a way that is based on practical rather than theoretical considerations.", "We need to take a pragmatic approach to software development.", new[] { "practical", "realistic" }, new[] { "idealistic", "impractical" }, 2, new[] { "adjectives" }, NewCardState())
            };
            SaveJson(defaultVocabFile, new VocabFile("Intermediate", defaultWords));
        }
    }

    private static SrState NewCardState() => new(2.5, 0, 0, null, null, "new");

    private static readonly JsonSerializerOptions _js = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true

    }
    ;

    public static T? LoadJson<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _js);
        }
        catch
        {
            return null;
        }

    }

    public static void SaveJson<T>(string path, T obj)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(obj, _js), Encoding.UTF8);
        }
        catch
        {
        }

    }

}

public sealed record SrState(double EaseFactor, int IntervalDays, int Repetitions, DateTime? LastReviewed, DateTime? NextReview, string Status);

public sealed record SrResult(SrState Updated, bool Passed, int NextIntervalDays);

public static class SpacedRepetitionEngine
{
    public static SrState NewCard() => new(2.5, 0, 0, null, null, "new");

    public static bool IsDueToday(SrState sr) => sr.NextReview == null || sr.NextReview.Value.Date <= DateTime.Today;

    public static SrResult UpdateCard(SrState current, int quality)
    {
        bool passed = quality >= 3;
        double ef = Math.Max(1.3, current.EaseFactor + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
        int reps = passed ? current.Repetitions + 1 : 0;
        int interval = reps switch
        {
            0 => 1,
            1 => 1,
            2 => 6,
            _ => (int)Math.Round(current.IntervalDays * ef)
        }
        ;
        if (!passed)
        {
            interval = 1;
        }
        string status = !passed ? "learning" : interval > 21 ? "mastered" : "review";
        var updated = new SrState(ef, interval, reps, DateTime.Now, DateTime.Today.AddDays(interval), status);
        return new SrResult(updated, passed, interval);

    }

    public static int CardsRemaining(SrState[] states) => states.Count(IsDueToday);

}

public sealed record FlashCard(string Id, string Front, string Back, string? Hint, string? Mnemonic, string? ExampleSentence, string[] Tags, int Difficulty, SrState Sr);

public sealed record DeckMeta(string Id, string Title, string Language, string Topic, string Level, string[] SourceNotes, string GeneratedAt, int Version);

public sealed record DeckFile(DeckMeta Meta, FlashCard[] Cards);

public static class FlashcardEngine
{
    public static void Run(string deckPath)
    {
        var deck = LearnDataPaths.LoadJson<DeckFile>(deckPath);
        if (deck == null || deck.Cards.Length == 0)
        {
            SpectrePanel.Warning("Deck not found or empty.");
            return;
        }
        Run(deck.Cards, deck.Meta.Title);

    }

    public static void Run(FlashCard[] cards, string deckName)
    {
        if (cards.Length == 0)
        {
            SpectrePanel.Info("No cards in deck.");
            return;
        }
        var queue = cards.Where(c => SpacedRepetitionEngine.IsDueToday(c.Sr)).ToList();
        if (queue.Count == 0)
        {
            SpectrePanel.Success($"All {cards.Length} cards in '{deckName}' are up to date!");
            return;
        }
        int known = 0, again = 0;
        foreach (var card in queue)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Flashcard: {deckName.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Card {known + again + 1} / {queue.Count} · ✓ {known} known · ✗ {again} again[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[bold]{card.Front.EscapeMarkup()}[/]" + (card.Hint != null ? $"\n[dim]{card.Hint.EscapeMarkup()}[/]" : ""))
            {
                Header = new PanelHeader("[dim]Front[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Cyan1),
                Padding = new Padding(1, 1)
            }
            );
            AnsiConsole.MarkupLine("[dim] Press Enter to reveal · Esc to exit[/]");
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape) break;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Flashcard: {deckName.EscapeMarkup()}[/]").RuleStyle("grey"));
            var backContent = card.Back.EscapeMarkup() + (card.ExampleSentence != null ? $"\n\n[dim]\"{card.ExampleSentence.EscapeMarkup()}\"[/]" : "") + (card.Mnemonic != null ? $"\n[yellow]💡 {card.Mnemonic.EscapeMarkup()}[/]" : "");
            AnsiConsole.Write(new Panel(backContent)
            {
                Header = new PanelHeader("[green bold]✓ Back[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(1, 1)
            }
            );
            if (AnsiConsole.Confirm("[bold]Did you know it?[/]", defaultValue: false)) known++;

            else again++;
        }
        AnsiConsole.Clear();
        SpectrePanel.Success($"Session complete — ✓ {known} known ✗ {again} missed ({queue.Count} cards reviewed)");

    }

    public static DeckFile[] GetDecks(string decksDir)
    {
        if (!Directory.Exists(decksDir)) return [];
        return Directory.GetFiles(decksDir, "*.json").Select(f => LearnDataPaths.LoadJson<DeckFile>(f)).OfType<DeckFile>().ToArray();

    }

    public static void PickAndRun(string decksDir)
    {
        var decks = GetDecks(decksDir);
        if (decks.Length == 0)
        {
            SpectrePanel.Warning($"No decks found in {decksDir}");
            return;
        }
        var names = decks.Select(d => d.Meta.Title).ToArray();
        var idx = SpectreMenu.Show("Select Flashcard Deck", names, 0, true);
        if (idx >= 0) Run(decks[idx].Cards, decks[idx].Meta.Title);

    }

}

public sealed record StudyScore(int Correct, int Total, double Percentage);

public sealed record StudyLogEntry(string Id, string Date, string StartTime, string EndTime, int DurationMinutes, string Topic, string SubTopic, string Activity, StudyScore Score, string[] WeakItems, int PomodoroCount, string Notes, string[] Tags);

public sealed record GoalTarget(string Topic, string Activity, int Count, int Completed);

public sealed record DailyGoalData(string Date, GoalTarget[] Targets);

public sealed record StreakData(int Current, int Best, string LastActive, int DaysThisWeek);

public sealed record DueItem(string Topic, string ItemId, string Front, DateTime NextReview, bool Overdue);

public sealed record MasteryData(string Topic, int Total, int Mastered, int Learning, int NewItems);

public sealed record WeakItem(string Topic, string ItemId, string FrontText, int FailCount);

public sealed record StudySummary(string Topic, int Score, int Total, string[] WeakItems, int DurationMinutes);

public sealed record StudyLogFile(DailyGoalData? DailyGoals, StudyLogEntry[] Sessions);

public static class StudySession
{
    public static void Run(string topic, int workMin = 25, int breakMin = 5)
    {
        LearnDataPaths.EnsureDirectories();
        int cycle = 0;
        var start = DateTime.Now;
        var allWeak = new List<string>();
        while (true)
        {
            cycle++;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Study Session — {topic.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Mode: Work · Cycle {cycle}[/]");
            RunTimer($"Work: {topic}", workMin * 60, Color.Green);
            AnsiConsole.Write(new Panel("[green]Work block complete! Take a break.[/]")
            {
                Header = new PanelHeader("[green bold]✓[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green)
            }
            );
            Thread.Sleep(500);
            if (!AnsiConsole.Confirm($"Continue to break ({breakMin} min)?", defaultValue: true)) break;
            RunTimer("Break", breakMin * 60, Color.Yellow);
            if (!AnsiConsole.Confirm("Continue next cycle?", defaultValue: true)) break;
        }
        var duration = (int)(DateTime.Now - start).TotalMinutes;
        var notes = AnsiConsole.Ask<string>("[dim]Session notes[/] (optional):", "").Trim();
        Record(topic, "general", "pomodoro", new StudyScore(0, 0, 0), [.. allWeak], cycle, duration, notes);
        SpectrePanel.Success($"Session complete — {cycle} cycles · {duration} min");

    }

    private static void RunTimer(string label, int totalSecs, Color barColor)
    {
        var start = DateTime.Now;
        while (true)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
            var elapsed = (int)(DateTime.Now - start).TotalSeconds;
            if (elapsed >= totalSecs) break;
            var pct = elapsed * 100.0 / totalSecs;
            var remain = TimeSpan.FromSeconds(totalSecs - elapsed);
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold]{label.EscapeMarkup()}[/]").RuleStyle("grey"));
            int bars = (int)(pct / 100.0 * 40);
            AnsiConsole.MarkupLine($"[{(barColor == Color.Green ? "green" : "yellow")}]{'█'.ToString().PadRight(bars, '█').PadRight(40, '░')}[/] {pct:F0}%");
            AnsiConsole.MarkupLine($"[dim]{elapsed / 60:00}:{elapsed % 60:00} elapsed · {remain:mm\\:ss} remaining[/]");
            AnsiConsole.MarkupLine("[dim]Esc to end early[/]");
            Thread.Sleep(1000);
        }

    }

    public static void Record(string topic, string subTopic, string activity, StudyScore score, string[] weakItems, int pomodoros, int durationMin, string notes)
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile) ?? new StudyLogFile(null, []);
        var sessions = log.Sessions.ToList();
        var now = DateTime.Now;
        var id = $"s_{sessions.Count + 1:000}";
        sessions.Add(new StudyLogEntry(id, now.ToString("yyyy-MM-dd"), now.ToString("HH:mm"), now.ToString("HH:mm"), durationMin, topic, subTopic, activity, score, weakItems, pomodoros, notes, []));
        LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, new StudyLogFile(log.DailyGoals, [.. sessions]));

    }

}
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
        var cutoff = DateTime.Today.AddDays(-7);
        var byTopic = logs.Where(s => DateTime.TryParse(s.Date, out var d) && d >= cutoff).GroupBy(s => s.Topic).ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));
        if (byTopic.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No study data recorded in the last 7 days.[/]");
            return;
        }
        var chart = new BarChart().Width(50).Label("[bold]Minutes[/]").CenterLabel();
        foreach (var (topic, mins) in byTopic.OrderByDescending(x => x.Value)) chart.AddItem(topic, mins, Color.Cyan1);
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
        }).Where(d => d != DateTime.MinValue).Distinct().OrderByDescending(d => d).ToArray();

        if (dates.Length == 0) return 0;

        int streak = 0;
        var check = DateTime.Today;

        if (dates[0] != check)
        {
            var diff = (check - dates[0]).TotalDays;
            if (diff == 1 || (allowGraceDay && diff <= 2))
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
                sb.AppendLine($" {(done ? "[green]✓[/]" : "[red]✗[/]")} {t.Topic,-12} {t.Activity,-12} [{bar}] {t.Completed}/{t.Count}");
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
        var weekAgo = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
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
    }

    public static void ClearWeakItems(string topic)
    {
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
