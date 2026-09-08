namespace AgyTui.UI.Screens.Quizzes.Helpers;

public sealed record KanaEntry(string Char, string Romaji, string Row, string Type, SrState Sr);

public sealed record KanaFile(KanaEntry[] Hiragana, KanaEntry[] Katakana);

public static class KanaQuiz
{
    public static void Run(string type = "hiragana")
    {
        LearnDataPaths.EnsureDirectories();
        var kana = LearnDataPaths.LoadJson<KanaFile>(LearnDataPaths.KanaFile);
        if (kana == null)
        {
            SpectrePanel.Warning("kana.json data file not available.");
            return;
        }
        KanaEntry[] pool = type switch
        {
            "katakana" => kana.Katakana,
            "both" => [.. kana.Hiragana, .. kana.Katakana],
            _ => kana.Hiragana
        };
        if (pool == null || pool.Length == 0)
        {
            SpectrePanel.Warning("No Kana entries available in the pool.");
            System.Threading.Thread.Sleep(1500);
            return;
        }
        var due = pool.Where(k => SpacedRepetitionEngine.IsDueToday(k.Sr)).ToArray();
        if (due.Length == 0) due = pool;

        var rowStats = new Dictionary<string, (int c, int t)>(StringComparer.OrdinalIgnoreCase);
        int correct = 0;
        var start = DateTime.Now;
        var weakItems = new List<string>();
        var reviewedList = due.Take(15).ToArray();

        foreach (var entry in reviewedList)
        {
            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(new Rule($"[bold cyan]Kana Quiz — {type}[/]").RuleStyle("grey"));
                AnsiConsole.MarkupLine($"[dim]Score: {correct}/{reviewedList.IndexOf(entry)} · Due: {due.Length}[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new FigletText(entry.Char).Centered().Color(Color.Green));
                AnsiConsole.WriteLine();
            });
            var answer = AnsiConsole.Ask<string>("[cyan]Romaji:[/]").Trim().ToLower();
            bool ok = answer == entry.Romaji.ToLower();
            AnsiConsole.MarkupLine(ok ? $"[green]✓ Correct! {entry.Char} = {entry.Romaji}[/]" : $"[red]✗ Wrong — {entry.Char} = {entry.Romaji} (you typed: {answer.EscapeMarkup()})[/]");

            var srResult = SpacedRepetitionEngine.UpdateCard(entry.Sr, ok ? 4 : 1);

            for (int i = 0; i < kana.Hiragana.Length; i++)
            {
                if (kana.Hiragana[i].Char == entry.Char)
                {
                    kana.Hiragana[i] = kana.Hiragana[i] with { Sr = srResult.Updated };
                    break;
                }
            }
            for (int i = 0; i < kana.Katakana.Length; i++)
            {
                if (kana.Katakana[i].Char == entry.Char)
                {
                    kana.Katakana[i] = kana.Katakana[i] with { Sr = srResult.Updated };
                    break;
                }
            }

            if (ok)
            {
                correct++;
            }
            else
            {
                weakItems.Add(entry.Char);
            }
            rowStats.TryGetValue(entry.Row, out var stat);
            rowStats[entry.Row] = (stat.c + (ok ? 1 : 0), stat.t + 1);
            Thread.Sleep(600);
        }

        LearnDataPaths.SaveJson(LearnDataPaths.KanaFile, kana);
        ShowAccuracyChart(rowStats);

        var duration = (int)(DateTime.Now - start).TotalMinutes;
        StudySession.Record($"Kana {type}", "language", "quiz", new StudyScore(correct, reviewedList.Length, reviewedList.Length > 0 ? (correct * 100.0 / reviewedList.Length) : 100.0), [.. weakItems], 0, duration, $"Reviewed Kana {type}", start);
        try { ObsidianStudySync.OfferSync(new StudySummary($"Kana {type}", correct, reviewedList.Length, [.. weakItems], duration)); } catch { }
    }

    public static void ShowAccuracyChart(Dictionary<string, (int c, int t)> rowStats)
    {
        if (rowStats.Count == 0) return;
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]Row Accuracy[/]").RuleStyle("grey"));
        var chart = new BarChart().Width(50).Label("[bold]Accuracy %[/]").CenterLabel();
        foreach (var (row, (c, t)) in rowStats.OrderBy(x => x.Key))
        {
            double pct = t > 0 ? Math.Round(c * 100.0 / t, 0) : 0;
            chart.AddItem($"{row}-row", pct, pct >= 80 ? Color.Green : pct >= 50 ? Color.Yellow : Color.Red);
        }
        AnsiConsole.Write(chart);

    }

}
static class ListExtensions
{
    public static int IndexOf<T>(this T[] arr, T item) => Array.IndexOf(arr, item);

}

public sealed record ExampleWord(string Word, string Reading, string Meaning);

public sealed record KanjiEntry(string Char, string[] Onyomi, string[] Kunyomi, string Meaning, string JlptLevel, int StrokeCount, string[] Radicals, ExampleWord[] ExampleWords, string? Mnemonic, string[] Tags, SrState Sr);

public sealed record KanjiFile(KanjiEntry[] Kanji);

public static class KanjiLookup
{
    public static void Run()
    {
        LearnDataPaths.EnsureDirectories();
        var file = LearnDataPaths.LoadJson<KanjiFile>(LearnDataPaths.KanjiFile);
        if (file == null || file.Kanji.Length == 0)
        {
            SpectrePanel.Warning("Kanji database not found.");
            return;
        }

        var all = file.Kanji;
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold cyan]Kanji Lookup[/]").RuleStyle("grey"));
            var query = AnsiConsole.Ask<string>("[cyan]Search[/] (meaning/kana, Enter=quit):", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(query)) return;
            var results = Search(all, query);
            if (results.Length == 0)
            {
                SpectrePanel.Warning($"No kanji matched '{query}'");
                continue;
            }
            var items = results.Select(k => $"{k.Char} {k.Meaning,-20} {k.JlptLevel,-3} {string.Join("、", k.Kunyomi)}").ToArray();
            var idx = SpectreMenu.Show($"Results for '{query}'", items, 0, false);
            if (idx >= 0) ShowDetail(results[idx]);
        }
    }

    public static KanjiEntry[] Search(KanjiEntry[] all, string query) => all.Where(k => k.Meaning.Contains(query, StringComparison.OrdinalIgnoreCase) || k.Char.Contains(query) || k.Onyomi.Any(o => o.Contains(query, StringComparison.OrdinalIgnoreCase)) || k.Kunyomi.Any(u => u.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();

    public static void ShowDetail(KanjiEntry k)
    {
        var lines = new List<string>
        {
            $"Meaning : {k.Meaning}",$"On-yomi : {string.Join("、", k.Onyomi)}",$"Kun-yomi : {string.Join("、", k.Kunyomi)}",$"JLPT : {k.JlptLevel}",$"Strokes : {k.StrokeCount}",$"Radicals : {string.Join(" ", k.Radicals)}","","Example words", new string('─', 40)
        }
        ;
        foreach (var ex in k.ExampleWords) lines.Add($" {ex.Word} {ex.Reading,-10} {ex.Meaning}");
        if (k.Mnemonic != null)
        {
            lines.Add("");
            lines.Add($"💡 {k.Mnemonic}");
        }
        SpectrePager.Show($"Kanji: {k.Char}", [.. lines]);

    }

}

public sealed record JlptWord(string Id, string Word, string Reading, string Romaji, string Meaning, string PartOfSpeech, string JlptLevel, string ExampleJp, string ExampleEn, string[] Tags, SrState Sr);

public sealed record JlptFile(string JlptLevel, JlptWord[] Words);

public static class JlptVocabDrill
{
    public static void Run(string level = "N5")
    {
        var path = System.IO.Path.Combine(LearnDataPaths.JlptDir, $"{level}.json");
        var data = LearnDataPaths.LoadJson<JlptFile>(path);
        if (data == null || data.Words.Length == 0)
        {
            SpectrePanel.Warning($"No JLPT {level} data found. Run: learn jp");
            return;
        }
        var cards = data.Words.Where(w => SpacedRepetitionEngine.IsDueToday(w.Sr)).Select(w => new FlashCard(w.Id, w.Word, $"{w.Reading} {w.Meaning}", w.Romaji, null, w.ExampleJp + " / " + w.ExampleEn, w.Tags, 3, w.Sr)).ToArray();
        FlashcardEngine.Run(cards, $"JLPT {level}", onSave: (updatedCards) =>
        {
            var dict = updatedCards.ToDictionary(c => c.Id, c => c.Sr);
            for (int i = 0; i < data.Words.Length; i++)
            {
                if (dict.TryGetValue(data.Words[i].Id, out var newSr))
                {
                    data.Words[i] = data.Words[i] with { Sr = newSr };
                }
            }
            LearnDataPaths.SaveJson(path, data);
        });

    }

}
public sealed record GrammarCard(string Id, string Level, string Pattern, string Meaning, string Usage, string ExampleJp, string ExampleEn, string[] Tags, SrState Sr);

public sealed record GrammarFile(string Level, GrammarCard[] Cards);

public static class GrammarQuiz
{
    public static void Run(string level = "N5")
    {
        LearnDataPaths.EnsureDirectories();
        string file = Path.Combine(LearnDataPaths.GrammarDir, $"{level.ToLower()}.json");
        if (!File.Exists(file))
        {
            SpectrePanel.Warning($"No grammar data found for level '{level}' at {file}.");
            return;
        }
        var data = LearnDataPaths.LoadJson<GrammarFile>(file);
        if (data == null || data.Cards.Length == 0)
        {
            SpectrePanel.Warning($"No grammar data found for level '{level}'.");
            return;
        }

        var due = data.Cards.Where(c => SpacedRepetitionEngine.IsDueToday(c.Sr)).ToArray();
        if (due.Length == 0) due = data.Cards;

        int correct = 0;
        int limit = Math.Min(10, due.Length);
        var start = DateTime.Now;
        var weakItems = new List<string>();

        for (int i = 0; i < limit; i++)
        {
            var g = due[i];
            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(new Rule($"[bold cyan]Grammar Drill — Level {g.Level}[/]").RuleStyle("grey"));
                AnsiConsole.MarkupLine($"[dim]Card {i + 1} / {limit}[/]");
                AnsiConsole.WriteLine();

                AnsiConsole.Write(new Panel($"[bold yellow]Pattern:[/] [bold white]{g.Pattern.EscapeMarkup()}[/]\n\n[dim]Usage:[/] {g.Usage.EscapeMarkup()}")
                {
                    Header = new PanelHeader($"[cyan]Grammar Point ({g.Level})[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Cyan1),
                    Padding = new Padding(1, 1)
                });

                AnsiConsole.MarkupLine("[dim]Press Enter to reveal meaning & examples (Esc to quit)...[/]");
            });
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;

            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(new Rule($"[bold cyan]Grammar Detail — {g.Pattern.EscapeMarkup()}[/]").RuleStyle("grey"));

                var detail = $"[bold yellow]Meaning:[/] {g.Meaning.EscapeMarkup()}\n\n" +
                             $"[bold green]Example (JP/EN):[/] {g.ExampleJp.EscapeMarkup()}\n" +
                             $"[bold green]Translation:[/] {g.ExampleEn.EscapeMarkup()}";

                AnsiConsole.Write(new Panel(detail)
                {
                    Header = new PanelHeader("[green]Explanation[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Green),
                    Padding = new Padding(1, 1)
                });
            });

            bool ok = AnsiConsole.Confirm("Did you understand this pattern?", defaultValue: true);
            var srResult = SpacedRepetitionEngine.UpdateCard(g.Sr, ok ? 4 : 1);

            for (int j = 0; j < data.Cards.Length; j++)
            {
                if (data.Cards[j].Id == g.Id)
                {
                    data.Cards[j] = data.Cards[j] with { Sr = srResult.Updated };
                    break;
                }
            }

            if (ok)
            {
                correct++;
            }
            else
            {
                weakItems.Add(g.Pattern);
            }
        }

        LearnDataPaths.SaveJson(file, data);
        SpectrePanel.Success($"Grammar drill complete — {correct}/{limit} understood.");

        var duration = (int)(DateTime.Now - start).TotalMinutes;
        StudySession.Record($"Grammar {level}", "language", "grammar", new StudyScore(correct, limit, limit > 0 ? (correct * 100.0 / limit) : 100.0), [.. weakItems], 0, duration, $"Reviewed Grammar {level}", start);
        try { ObsidianStudySync.OfferSync(new StudySummary($"Grammar {level}", correct, limit, [.. weakItems], duration)); } catch { }
    }
}
