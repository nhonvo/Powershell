namespace AgyTui.UI.Screens.Career;

public sealed record InterviewQuestion(string Id, string Type, string Category, string Difficulty, string Question, string Format, string[] Hints, string[] Companies, string[] Tags);

public sealed record InterviewFile(InterviewQuestion[] Questions);

public static class InterviewBank
{
    public static void Run()
    {
        var file = LearnDataPaths.LoadJson<InterviewFile>(LearnDataPaths.InterviewFile);
        if (file == null || file.Questions.Length == 0)
        {
            SpectrePanel.Warning("No interview data. Run: learn interview");
            return;
        }
        var all = file.Questions;
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Interview Bank — {all.Length} questions[/]").RuleStyle("grey"));
            var items = all.Select(q => $"{q.Question,-55} [dim]{q.Type}[/]").ToArray();
            var actions = new[]
            {
                "[r] Random question","[f] Filter by type","← Back"
            };
            var topIdx = SpectreMenu.Show("Options", actions, 0, false);
            if (topIdx == 0)
            {
                ShowQuestion(all[new Random().Next(all.Length)]);
                continue;
            }
            if (topIdx == 1)
            {
                var types = all.Select(q => q.Type).Distinct().ToArray();
                var tIdx = SpectreMenu.Show("Filter by type", types, 0, false);
                if (tIdx >= 0)
                {
                    var filtered = Filter(all, types[tIdx], null, null);
                    var qIdx = SpectreMenu.Show($"Type: {types[tIdx]}", filtered.Select(q => q.Question).ToArray(), 0, true);
                    if (qIdx >= 0) ShowQuestion(filtered[qIdx]);
                }
                continue;
            }
            return;
        }
    }

    public static void RunRandom()
    {
        var file = LearnDataPaths.LoadJson<InterviewFile>(LearnDataPaths.InterviewFile);
        if (file == null || file.Questions.Length == 0)
        {
            SpectrePanel.Warning("No interview data.");
            return;
        }
        ShowQuestion(file.Questions[new Random().Next(file.Questions.Length)]);
    }

    public static void ShowQuestion(InterviewQuestion q)
    {
        var lines = new List<string>
        {
            $"[bold cyan]{q.Type.EscapeMarkup()} · {q.Category.EscapeMarkup()} · {q.Difficulty.EscapeMarkup()}[/]", new string('─', 50),"",$"[bold]{q.Question.EscapeMarkup()}[/]","",$"[dim]Format: {q.Format.EscapeMarkup()}[/]",
        };
        if (q.Hints.Length > 0)
        {
            lines.Add("");
            lines.Add("[cyan]Hints:[/]");
            foreach (var h in q.Hints) lines.Add($" • {h.EscapeMarkup()}");
        }
        if (q.Companies.Length > 0) lines.Add($"\n[dim]Companies: {string.Join(", ", q.Companies).EscapeMarkup()}[/]");
        SpectrePager.Show($"Interview: {q.Type}", [.. lines]);
    }

    public static InterviewQuestion[] Filter(InterviewQuestion[] all, string? type, string? difficulty, string? tag)
    {
        IEnumerable<InterviewQuestion> q = all;
        if (!string.IsNullOrEmpty(type)) q = q.Where(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(difficulty)) q = q.Where(x => x.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(tag)) q = q.Where(x => x.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase)));
        return [.. q];
    }
}

public sealed record StarAnswer(string Id, string QuestionId, string QuestionText, string Situation, string Task, string Action, string Result, string OutcomeMetric, string CreatedAt, string UpdatedAt, string[] Tags, int Rating);

public sealed record StarFile(StarAnswer[] Answers);

public static class StarBuilder
{
    public static void Run()
    {
        AnsiConsole.Write(new Rule("[bold cyan]STAR Answer Builder[/]").RuleStyle("grey"));
        var question = AnsiConsole.Ask<string>("[bold]Interview question:[/]").Trim();
        if (string.IsNullOrWhiteSpace(question)) return;
        AnsiConsole.MarkupLine("[dim]Answer each section. Press Enter when done.[/]\n");
        var situation = AnsiConsole.Ask<string>("[cyan]Situation[/] (set the context):").Trim();
        var task = AnsiConsole.Ask<string>("[cyan]Task[/] (your responsibility):").Trim();
        var action = AnsiConsole.Ask<string>("[cyan]Action[/] (what you did):").Trim();
        var result = AnsiConsole.Ask<string>("[cyan]Result[/] (outcome):").Trim();
        var metric = AnsiConsole.Ask<string>("[dim]Outcome metric[/] (optional):", "").Trim();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel($"[bold]S:[/] {situation.EscapeMarkup()}\n" + $"[bold]T:[/] {task.EscapeMarkup()}\n" + $"[bold]A:[/] {action.EscapeMarkup()}\n" + $"[bold]R:[/] {result.EscapeMarkup()}" + (metric.Length > 0 ? $"\n[dim]{metric.EscapeMarkup()}[/]" : ""))
        {
            Header = new PanelHeader("[bold]✓ STAR Answer[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(1, 1)
        }
        );
        if (!AnsiConsole.Confirm("Save this answer?", defaultValue: false)) return;
        var file = LearnDataPaths.LoadJson<StarFile>(LearnDataPaths.StarFile) ?? new StarFile([]);
        var answers = file.Answers.ToList();
        var now = DateTimeOffset.Now.ToString("o");
        answers.Add(new StarAnswer($"star_{answers.Count + 1:000}", "", question, situation, task, action, result, metric, now, now, [], 3));
        LearnDataPaths.SaveJson(LearnDataPaths.StarFile, new StarFile([.. answers]));
        SpectrePanel.Success("STAR answer saved.");
    }

    public static void Review()
    {
        var file = LearnDataPaths.LoadJson<StarFile>(LearnDataPaths.StarFile);
        if (file == null || file.Answers.Length == 0)
        {
            SpectrePanel.Info("No saved STAR answers.");
            return;
        }
        var items = file.Answers.Select(a => a.QuestionText).ToArray();
        var idx = SpectreMenu.Show("Saved STAR Answers", items, 0, true);
        if (idx < 0) return;
        var a = file.Answers[idx];
        SpectrePager.Show($"STAR: {a.QuestionText[..ParagraphLength(a.QuestionText)]}", [$"[bold]Question:[/] {a.QuestionText.EscapeMarkup()}", "", $"[bold]S:[/] {a.Situation.EscapeMarkup()}", "", $"[bold]T:[/] {a.Task.EscapeMarkup()}", "", $"[bold]A:[/] {a.Action.EscapeMarkup()}", "", $"[bold]R:[/] {a.Result.EscapeMarkup()}", a.OutcomeMetric.Length > 0 ? $"\n[dim]{a.OutcomeMetric.EscapeMarkup()}[/]" : "", $"\n[dim]Created: {a.CreatedAt}[/]"]);
    }

    private static int ParagraphLength(string val) => Math.Min(40, val.Length);
}

public static class MockInterviewTimer
{
    public static void Run(int timeLimitSeconds = 300)
    {
        var file = LearnDataPaths.LoadJson<InterviewFile>(LearnDataPaths.InterviewFile);
        InterviewQuestion[] questions = file?.Questions ?? [];
        if (questions.Length == 0)
        {
            SpectrePanel.Warning("No interview data.");
            return;
        }
        RunSession(questions.OrderBy(_ => Guid.NewGuid()).Take(3).ToArray(), timeLimitSeconds);
    }

    public static void RunSession(InterviewQuestion[] questions, int timeLimitSeconds)
    {
        foreach (var q in questions)
        {
            var start = DateTime.Now;
            AnsiConsole.Live(new Table
            {
                Border = TableBorder.None
            }
            ).Start(ctx =>
            {
                while ((DateTime.Now - start).TotalSeconds < timeLimitSeconds && !Console.KeyAvailable)
                {
                    var elapsed = DateTime.Now - start;
                    var pct = Math.Min(100.0, elapsed.TotalSeconds / timeLimitSeconds * 100.0);
                    AnsiConsole.Clear();
                    AnsiConsole.Write(new Rule($"[bold cyan]Mock Interview[/] [dim]{elapsed:mm\\:ss} / {TimeSpan.FromSeconds(timeLimitSeconds):mm\\:ss}[/]").RuleStyle("grey"));
                    AnsiConsole.Write(new Panel($"[bold]{q.Type.EscapeMarkup()}[/]\n\n[bold white]{q.Question.EscapeMarkup()}[/]" + (q.Hints.Length > 0 ? $"\n\n[dim]Hint: {q.Hints[0].EscapeMarkup()}[/]" : ""))
                    {
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Cyan1),
                        Padding = new Padding(1, 1)
                    }
                    );
                    int bars = (int)(pct / 100.0 * 40);
                    AnsiConsole.MarkupLine($"[cyan]{'█'.ToString().PadRight(bars, '█').PadRight(40, '░')}[/] {pct:F0}%");
                    AnsiConsole.MarkupLine("[dim] Esc stop early · Enter mark done & next[/]");
                    Thread.Sleep(500);
                }
                if (Console.KeyAvailable) Console.ReadKey(true);
            });
            if (!AnsiConsole.Confirm("Continue to next question?", defaultValue: true)) break;
        }
        SpectrePanel.Success("Mock interview session complete.");
    }
}

public sealed record VocabWord(string Id, string Word, string Pronunciation, string PartOfSpeech, string Definition, string ExampleSentence, string[] Synonyms, string[] Antonyms, int Difficulty, string[] Tags, SrState Sr);

public sealed record VocabFile(string Level, VocabWord[] Words);

public static class VocabDrill
{
    public static void Run(string difficulty = "Intermediate")
    {
        var file = System.IO.Path.Combine(LearnDataPaths.VocabDir, $"{difficulty.ToLower()}.json");
        var vocab = LearnDataPaths.LoadJson<VocabFile>(file);
        if (vocab == null || vocab.Words.Length == 0)
        {
            SpectrePanel.Warning($"No vocabulary data for level '{difficulty}'. Run refresh-data first.");
            return;
        }
        var due = vocab.Words.Where(w => SpacedRepetitionEngine.IsDueToday(w.Sr)).ToArray();
        if (due.Length == 0)
        {
            SpectrePanel.Success($"All {difficulty} vocabulary is up to date!");
            return;
        }
        int correct = 0, total = 0;
        var start = DateTime.Now;
        var weakItems = new List<string>();

        foreach (var word in due)
        {
            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(new Rule($"[bold cyan]{difficulty} Vocab[/]").RuleStyle("grey"));
                AnsiConsole.MarkupLine($"[dim]Word {total + 1} / {due.Length} · Weak queue: {due.Length - total}[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel($"[bold]{word.Word.EscapeMarkup()}[/]\n[dim]{word.Pronunciation.EscapeMarkup()}[/]")
                {
                    Header = new PanelHeader("[cyan]ℹ[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Cyan1),
                    Padding = new Padding(1, 1)
                }
                );
                AnsiConsole.MarkupLine("[dim] Press Enter to reveal definition[/]");
            });
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;

            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(new Rule($"[bold cyan]{difficulty} Vocab[/]").RuleStyle("grey"));
                var detail = $"[bold]{word.Word.EscapeMarkup()}[/] [dim]{word.PartOfSpeech.EscapeMarkup()}[/]\n\n" + $"{word.Definition.EscapeMarkup()}\n\n" + $"[italic dim]\"{word.ExampleSentence.EscapeMarkup()}\"[/]";
                if (word.Synonyms.Length > 0) detail += $"\n[dim]Synonyms: {string.Join(", ", word.Synonyms).EscapeMarkup()}[/]";
                AnsiConsole.Write(new Panel(detail)
                {
                    Header = new PanelHeader("[green]Definition[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Green),
                    Padding = new Padding(1, 1)
                }
                );
            });
            bool knewIt = AnsiConsole.Confirm("Did you know it?", defaultValue: false);
            int quality = knewIt ? 4 : 1;
            var srResult = SpacedRepetitionEngine.UpdateCard(word.Sr, quality);

            for (int i = 0; i < vocab.Words.Length; i++)
            {
                if (vocab.Words[i].Id == word.Id)
                {
                    vocab.Words[i] = vocab.Words[i] with { Sr = srResult.Updated };
                    break;
                }
            }

            if (knewIt)
            {
                correct++;
            }
            else
            {
                weakItems.Add(word.Word);
            }
            total++;
        }

        LearnDataPaths.SaveJson(file, vocab);
        SpectrePanel.Success($"Vocab drill done — {correct}/{total} correct");

        var duration = (int)(DateTime.Now - start).TotalMinutes;
        StudySession.Record($"Vocab {difficulty}", "vocabulary", "drill", new StudyScore(correct, total, total > 0 ? (correct * 100.0 / total) : 100.0), [.. weakItems], 0, duration, $"Reviewed Vocab {difficulty}", start);
        try { ObsidianStudySync.OfferSync(new StudySummary($"Vocab {difficulty}", correct, total, [.. weakItems], duration)); } catch { }
    }
}
