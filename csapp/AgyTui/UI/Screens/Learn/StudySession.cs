namespace AgyTui.UI.Screens.Learn;

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

    public static void Record(string topic, string subTopic, string activity, StudyScore score, string[] weakItems, int pomodoros, int durationMin, string notes, DateTime? startTime = null)
    {
        var log = LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile) ?? new StudyLogFile(null, []);
        var sessions = log.Sessions.ToList();
        var now = DateTime.Now;
        var start = startTime ?? now.AddMinutes(-durationMin);
        var id = $"s_{sessions.Count + 1:000}";
        sessions.Add(new StudyLogEntry(id, start.ToString("yyyy-MM-dd"), start.ToString("HH:mm"), now.ToString("HH:mm"), durationMin, topic, subTopic, activity, score, weakItems, pomodoros, notes, []));
        LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, new StudyLogFile(log.DailyGoals, [.. sessions]));
    }
}
