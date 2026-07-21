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
        var due = pool.Where(k => SpacedRepetitionEngine.IsDueToday(k.Sr)).ToArray();
        if (due.Length == 0) due = pool;

        var rowStats = new Dictionary<string, (int c, int t)>(StringComparer.OrdinalIgnoreCase);
        int correct = 0;
        var start = DateTime.Now;
        var weakItems = new List<string>();
        var reviewedList = due.Take(15).ToArray();

        foreach (var entry in reviewedList)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Kana Quiz — {type}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Score: {correct}/{reviewedList.IndexOf(entry)} · Due: {due.Length}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new FigletText(entry.Char).Centered().Color(Color.Green));
            AnsiConsole.WriteLine();
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
        StudySession.Record($"Kana {type}", "language", "quiz", new StudyScore(correct, reviewedList.Length, reviewedList.Length > 0 ? (correct * 100.0 / reviewedList.Length) : 100.0), [.. weakItems], 0, duration, $"Reviewed Kana {type}");
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
        FlashcardEngine.Run(cards, $"JLPT {level}");

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
            AnsiConsole.Clear();
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
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;

            AnsiConsole.Clear();
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
        StudySession.Record($"Grammar {level}", "language", "grammar", new StudyScore(correct, limit, limit > 0 ? (correct * 100.0 / limit) : 100.0), [.. weakItems], 0, duration, $"Reviewed Grammar {level}");
    }
}

public static class AlgoVisualizer
{
    public static void PickAndRun()
    {
        var algos = new[]
        {
            "Bubble Sort",
            "Binary Search",
            "Merge Sort",
            "Quick Sort",
            "BFS Graph Traversal",
            "Dynamic Programming (Fibonacci Table)"
        };
        var idx = SpectreMenu.Show("Algorithm Visualizer", algos, 0, false);
        var arr = GenerateArray(8);
        switch (idx)
        {
            case 0:
                RunBubbleSort([.. arr]);
                break;
            case 1:
                RunBinarySearch([.. arr.OrderBy(x => x)], arr[0]);
                break;
            case 2:
                RunMergeSort([.. arr]);
                break;
            case 3:
                RunQuickSort([.. arr]);
                break;
            case 4:
                RunBfsTraversal();
                break;
            case 5:
                RunDpFibonacci(7);
                break;
        }
    }

    public static void RunBubbleSort(int[] input)
    {
        var a = (int[])input.Clone();
        int step = 0, comps = 0, swaps = 0;
        for (int i = 0;
        i < a.Length - 1;
        i++) for (int j = 0;
        j < a.Length - i - 1;
        j++)
        {
            RenderArray(a, j, j + 1, ++step, comps, swaps, "Bubble Sort");
            comps++;
            if (a[j] > a[j + 1])
            {
                (a[j], a[j + 1]) = (a[j + 1], a[j]);
                swaps++;
            }
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) return;
        }
        RenderArray(a, -1, -1, step, comps, swaps, "Bubble Sort — Done");
        Console.ReadKey(true);

    }

    public static void RunBinarySearch(int[] sorted, int target)
    {
        int lo = 0, hi = sorted.Length - 1, step = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            RenderArray(sorted, lo, hi, ++step, 0, 0, $"Binary Search: target={target} mid={sorted[mid]}");
            if (sorted[mid] == target)
            {
                SpectrePanel.Success($"Found {target} at index {mid}!");
                return;
            }
            if (sorted[mid] < target) lo = mid + 1;

            else hi = mid - 1;
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) return;
        }
        SpectrePanel.Warning($"{target} not found.");

    }

    public static void RunMergeSort(int[] input)
    {
        var a = (int[])input.Clone();
        int step = 0;
        MergeSortHelper(a, 0, a.Length - 1, ref step);
        RenderArray(a, -1, -1, step, 0, 0, "Merge Sort — Done");
        Console.ReadKey(true);

    }

    private static void MergeSortHelper(int[] a, int lo, int hi, ref int step)
    {
        if (lo >= hi) return;
        int mid = (lo + hi) / 2;
        MergeSortHelper(a, lo, mid, ref step);
        MergeSortHelper(a, mid + 1, hi, ref step);
        int[] merged = new int[hi - lo + 1];
        int l = lo, r = mid + 1, k = 0;
        while (l <= mid && r <= hi) merged[k++] = a[l] <= a[r] ? a[l++] : a[r++];
        while (l <= mid) merged[k++] = a[l++];
        while (r <= hi) merged[k++] = a[r++];
        for (int i = 0;
        i < merged.Length;
        i++) a[lo + i] = merged[i];
        RenderArray(a, lo, hi, ++step, 0, 0, $"Merge Sort — merged [{lo}..{hi}]");
        Console.ReadKey(true);
    }

    public static void RunQuickSort(int[] input)
    {
        var a = (int[])input.Clone();
        int step = 0;
        QuickSortHelper(a, 0, a.Length - 1, ref step);
        RenderArray(a, -1, -1, step, 0, 0, "Quick Sort — Done");
        Console.ReadKey(true);
    }

    private static void QuickSortHelper(int[] a, int low, int high, ref int step)
    {
        if (low < high)
        {
            int pi = Partition(a, low, high, ref step);
            QuickSortHelper(a, low, pi - 1, ref step);
            QuickSortHelper(a, pi + 1, high, ref step);
        }
    }

    private static int Partition(int[] a, int low, int high, ref int step)
    {
        int pivot = a[high];
        int i = (low - 1);
        for (int j = low; j < high; j++)
        {
            if (a[j] < pivot)
            {
                i++;
                (a[i], a[j]) = (a[j], a[i]);
            }
            RenderArray(a, low, high, ++step, 0, 0, $"Quick Sort — pivot={pivot}");
        }
        (a[i + 1], a[high]) = (a[high], a[i + 1]);
        return i + 1;
    }

    public static void RunBfsTraversal()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]BFS Graph Traversal Visualizer[/]").RuleStyle("grey"));

        var graph = new Dictionary<string, string[]>
        {
            ["A"] = new[] { "B", "C" },
            ["B"] = new[] { "D", "E" },
            ["C"] = new[] { "F" },
            ["D"] = Array.Empty<string>(),
            ["E"] = new[] { "F" },
            ["F"] = Array.Empty<string>()
        };

        var queue = new Queue<string>();
        var visited = new HashSet<string>();
        queue.Enqueue("A");
        visited.Add("A");

        int step = 0;
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            step++;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]BFS Step {step}: Visiting Node [{node}][/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[bold yellow]Queue:[/] {string.Join(" -> ", queue)}");
            AnsiConsole.MarkupLine($"[bold green]Visited:[/] {string.Join(", ", visited)}");
            AnsiConsole.WriteLine();

            foreach (var neighbor in graph[node])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            AnsiConsole.MarkupLine("[dim]Press Enter for next BFS step...[/]");
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;
        }
        SpectrePanel.Success("BFS Graph Traversal Complete!");
    }

    public static void RunDpFibonacci(int n)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold cyan]Dynamic Programming — Fibonacci(N={n})[/]").RuleStyle("grey"));

        long[] dp = new long[n + 1];
        dp[0] = 0;
        if (n >= 1) dp[1] = 1;

        for (int i = 2; i <= n; i++)
        {
            dp[i] = dp[i - 1] + dp[i - 2];
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]DP Fibonacci Step {i}[/]").RuleStyle("grey"));
            
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Index (N)");
            table.AddColumn("DP Value");
            table.AddColumn("Formula");

            for (int k = 0; k <= i; k++)
            {
                table.AddRow(k.ToString(), $"[bold green]{dp[k]}[/]", k >= 2 ? $"F({k-1}) + F({k-2}) = {dp[k-1]} + {dp[k-2]}" : "Base case");
            }
            AnsiConsole.Write(table);

            AnsiConsole.MarkupLine("[dim]Press Enter for next DP step...[/]");
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;
        }
        SpectrePanel.Success($"Fibonacci({n}) = {dp[n]} computed via Dynamic Programming!");
    }

    private static void RenderArray(int[] a, int lo, int hi, int step, int comps, int swaps, string label)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold cyan]AGY — Algo: {label.EscapeMarkup()}[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[dim]Step {step} · Comparisons: {comps} · Swaps: {swaps}[/]");
        AnsiConsole.WriteLine();
        var t = new Table
        {
            Border = TableBorder.Rounded
        }
        ;
        for (int i = 0;
        i < a.Length;
        i++) t.AddColumn(new TableColumn("").Centered());
        t.AddRow(a.Select((v, i) =>
        {
            bool hl = i >= lo && i <= hi;
            return hl ? $"[green bold]{v}[/]" : v.ToString();
        }
        ).ToArray());
        AnsiConsole.Write(t);
        if (lo >= 0 && hi >= 0 && lo < a.Length) AnsiConsole.MarkupLine($"[dim] comparing indices {lo}–{hi}[/]");
        AnsiConsole.MarkupLine("[dim] Enter next step · Esc exit[/]");

    }

    private static int[] GenerateArray(int size)
    {
        var rng = new Random();
        return Enumerable.Range(0, size).Select(_ => rng.Next(1, 20)).ToArray();

    }

}

public sealed record ComplexityEntry(string Name, string Access, string Search, string Insert, string Delete, string Space, string Notes, string[] Tags);

public sealed record AlgoEntry(string Name, string Best, string Average, string Worst, string Space, string Category, string Notes, string[] Tags);

public sealed record ComplexityFile(ComplexityEntry[] DataStructures, AlgoEntry[] Algorithms);

public static class ComplexitySheet
{
    public static void Run()
    {
        var categories = new[]
        {
            "Data Structures","Sorting Algorithms","Search Algorithms"
        }
        ;
        while (true)
        {
            var idx = SpectreMenu.Show("Big-O Complexity Sheet", [.. categories, "← Back"], 0, false);
            if (idx < 0 || idx >= categories.Length) return;
            ShowCategory(categories[idx]);
        }

    }

    public static void ShowCategory(string category)
    {
        var data = LearnDataPaths.LoadJson<ComplexityFile>(LearnDataPaths.ComplexityFile);
        if (category == "Data Structures")
        {
            var rows = (data?.DataStructures ?? GetDefaultStructures()).Select(e => new[]
            {
                e.Name, e.Access, e.Search, e.Insert, e.Delete, e.Space, e.Notes
            }
            ).ToArray();
            SpectreTable.Render(["Structure", "Access", "Search", "Insert", "Delete", "Space", "Notes"], rows);
        }
        else
        {
            var rows = (data?.Algorithms ?? GetDefaultAlgorithms()).Where(a => category == "Sorting Algorithms" ? a.Category == "sort" : a.Category == "search").Select(a => new[]
            {
                a.Name, a.Best, a.Average, a.Worst, a.Space, a.Notes
            }
            ).ToArray();
            SpectreTable.Render(["Algorithm", "Best", "Average", "Worst", "Space", "Notes"], rows);
        }
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
        Console.ReadKey(true);

    }

    private static ComplexityEntry[] GetDefaultStructures() => [new("Array", "O(1)", "O(n)", "O(n)", "O(n)", "O(n)", "random access O(1)", []), new("Linked List", "O(n)", "O(n)", "O(1)", "O(1)", "O(n)", "prepend O(1)", []), new("Hash Table", "N/A", "O(1)", "O(1)", "O(1)", "O(n)", "worst O(n) collision", []), new("BST", "O(log n)", "O(log n)", "O(log n)", "O(log n)", "O(n)", "balanced only", []), new("Heap", "O(1)*", "O(n)", "O(log n)", "O(log n)", "O(n)", "*min/max only", []), new("Stack/Queue", "O(n)", "O(n)", "O(1)", "O(1)", "O(n)", "push/pop O(1)", []),];

    private static AlgoEntry[] GetDefaultAlgorithms() => [new("Merge Sort", "O(n log n)", "O(n log n)", "O(n log n)", "O(n)", "sort", "stable", []), new("Quick Sort", "O(n log n)", "O(n log n)", "O(n²)", "O(log n)", "sort", "in-place", []), new("Heap Sort", "O(n log n)", "O(n log n)", "O(n log n)", "O(1)", "sort", "in-place", []), new("Bubble Sort", "O(n)", "O(n²)", "O(n²)", "O(1)", "sort", "simple", []), new("Binary Search", "O(1)", "O(log n)", "O(log n)", "O(1)", "search", "sorted array", []), new("BFS/DFS", "O(V+E)", "O(V+E)", "O(V+E)", "O(V)", "search", "graph traversal", []),];

}

public sealed record Problem(string Id, string Title, string Source, string Url, string Difficulty, string[] Topics, string Status, string TimeComplexity, string SpaceComplexity, string ApproachNotes, int Attempts, string? FirstSolvedAt, string? LastReviewedAt, string[] Tags);

public sealed record ProblemsFile(Problem[] Problems);

public static class ProblemTracker
{
    public static void Run()
    {
        while (true)
        {
            var data = Load();
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold cyan]Problem Tracker[/]").RuleStyle("grey"));
            var rows = data.Select(p => new[]
            {
                p.Title, p.Difficulty, string.Join(", ", p.Topics), p.Status=="solved"?"[green]✓ Solved[/]":p.Status=="review"?"[yellow]↺ Review[/]":"[dim]○ Todo[/]"
            }
            ).ToArray();
            SpectreTable.Render(["Title", "Diff", "Topics", "Status"], rows, markup: true);
            var actions = new[]
            {
                "[n] Add problem","[f] Filter by topic","← Back"
            }
            ;
            var idx = SpectreMenu.Show("Problem Tracker", actions, 0, false);
            if (idx == 0) Add();

            else return;
        }

    }

    public static void Add()
    {
        var title = AnsiConsole.Ask<string>("[cyan]Title:[/]").Trim();
        var source = AnsiConsole.Ask<string>("[dim]Source[/] (e.g. LeetCode #1):", "").Trim();
        var diff = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Difficulty").AddChoices("easy", "medium", "hard"));
        var topics = AnsiConsole.Ask<string>("[dim]Topics[/] (comma-separated):", "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
        var data = Load().ToList();
        var id = $"p_{(data.Count + 1):000}";
        data.Add(new Problem(id, title, source, "", diff, topics, "todo", "?", "?", "", 0, null, null, []));
        Save([.. data]);
        SpectrePanel.Success($"Problem '{title}' added.");

    }

    public static Problem[] Filter(Problem[] all, string? topic, string? status)
    {
        IEnumerable<Problem> q = all;
        if (!string.IsNullOrEmpty(topic)) q = q.Where(p => p.Topics.Any(t => t.Contains(topic, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);
        return [.. q];

    }

    private static Problem[] Load()
    {
        var f = LearnDataPaths.LoadJson<ProblemsFile>(LearnDataPaths.ProblemsFile);
        return f?.Problems ?? [];

    }

    private static void Save(Problem[] problems) => LearnDataPaths.SaveJson(LearnDataPaths.ProblemsFile, new ProblemsFile(problems));

}

public sealed record CodeSnippet(string Id, string Title, string Category, string Code, string Explanation, string UseCase, string[] Tags, int Difficulty);

public sealed record SnippetsFile(string Language, CodeSnippet[] Snippets);

public static class SnippetLibrary
{
    public static void Run()
    {
        var langs = Directory.Exists(LearnDataPaths.SnippetsDir) ? Directory.GetFiles(LearnDataPaths.SnippetsDir, "*.json").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToArray() : new[]
        {
            "csharp","powershell","sql"
        }
        ;
        if (langs.Length == 0)
        {
            SpectrePanel.Warning("No snippet files found.");
            return;
        }
        var langIdx = SpectreMenu.Show("Snippet Library", [.. langs, "← Back"], 0, false);
        if (langIdx < 0 || langIdx >= langs.Length) return;
        var lang = langs[langIdx];
        var path = System.IO.Path.Combine(LearnDataPaths.SnippetsDir, $"{lang}.json");
        var file = LearnDataPaths.LoadJson<SnippetsFile>(path);
        if (file == null || file.Snippets.Length == 0)
        {
            SpectrePanel.Warning($"No {lang} snippets found.");
            return;
        }
        var titles = file.Snippets.Select(s => s.Title).ToArray();
        var idx = SpectreMenu.Show($"{lang} Snippets", [.. titles, "← Back"], 0, true);
        if (idx < 0 || idx >= file.Snippets.Length) return;
        var snip = file.Snippets[idx];
        var lines = new List<string>
        {
            $"[bold]{snip.Title.EscapeMarkup()}[/]",$"[dim]{snip.Category.EscapeMarkup()} · Difficulty {snip.Difficulty}[/]","", snip.Code.EscapeMarkup(),"",$"[cyan]{snip.Explanation.EscapeMarkup()}[/]","",$"[dim]Use case: {snip.UseCase.EscapeMarkup()}[/]"
        }
        ;
        SpectrePager.Show($"{lang}: {snip.Title}", [.. lines]);
        if (AnsiConsole.Confirm("Copy to clipboard?", defaultValue: false)) CopyToClipboard(snip.Code);

    }

    public static void CopyToClipboard(string text)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var psi = new ProcessStartInfo("clip")
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true
                }
                ;

                using var p = Process.Start(psi)!;
                p.StandardInput.Write(text);
                p.StandardInput.Close();
                p.WaitForExit();
                SpectrePanel.Success("Copied to clipboard.");
            }
            else SpectrePanel.Warning("Clipboard only supported on Windows.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Clipboard error: {ex.Message}");
        }

    }

}
public static class CheatSheetBrowser
{
    public static void Run()
    {
        var sheets = Directory.Exists(LearnDataPaths.SheetsDir) ? Directory.GetFiles(LearnDataPaths.SheetsDir, "*.txt").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToArray() : new[]
        {
            "csharp","powershell","sql","bash","regex","git","docker"
        }
        ;
        var idx = SpectreMenu.Show("Cheat Sheets", [.. sheets, "← Back"], 0, false);
        if (idx < 0 || idx >= sheets.Length) return;
        Show(System.IO.Path.Combine(LearnDataPaths.SheetsDir, $"{sheets[idx]}.txt"), sheets[idx]);

    }

    public static void Show(string filePath, string title)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Warning($"Cheat sheet not found: {filePath}");
            return;
        }
        var lines = File.ReadAllLines(filePath);
        SpectrePager.Show($"Cheat Sheet: {title}", lines);

    }

}

public sealed record QuizQuestion(string Id, string Topic, int Difficulty, string Question, string[] Options, int CorrectAnswer, string Explanation, string? CodeSnippet, string[] Tags);

public sealed record QuizFile(QuizQuestion[] Questions);

public static class CsharpQuiz
{
    public static void Run(string? topic = null)
    {
        var file = LearnDataPaths.LoadJson<QuizFile>(LearnDataPaths.QuizFile);
        if (file == null || file.Questions.Length == 0)
        {
            SpectrePanel.Warning("No quiz data. Run: learn cs");
            return;
        }
        var questions = topic != null ? file.Questions.Where(q => q.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)).ToArray() : file.Questions;
        if (questions.Length == 0)
        {
            SpectrePanel.Warning($"No questions for topic '{topic}'");
            return;
        }
        var scores = new Dictionary<string, (int c, int t)>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in questions.OrderBy(_ => Guid.NewGuid()).Take(10))
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]C# Quiz — {q.Topic.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]{q.Question.EscapeMarkup()}[/]");
            if (q.CodeSnippet != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]{q.CodeSnippet.EscapeMarkup()}[/]");
            }
            AnsiConsole.WriteLine();
            var chosen = SpectreMenu.Show("Select answer", q.Options, 0, false);
            bool correct = chosen == q.CorrectAnswer;
            scores.TryGetValue(q.Topic, out var s);
            scores[q.Topic] = (s.c + (correct ? 1 : 0), s.t + 1);
            AnsiConsole.Write(new Panel((correct ? "[green]✓ Correct![/]" : $"[red]✗ Wrong — answer: {q.Options[q.CorrectAnswer].EscapeMarkup()}[/]") + $"\n\n{q.Explanation.EscapeMarkup()}")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(correct ? Color.Green : Color.Red),
                Padding = new Padding(1, 0)
            }
            );
            Console.ReadKey(true);
        }
        ShowResults(scores);

    }

    public static void ShowResults(Dictionary<string, (int c, int t)> scores)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Quiz Results[/]").RuleStyle("grey"));
        var rows = scores.Select(kv => new[]
        {
            kv.Key, kv.Value.c.ToString(), kv.Value.t.ToString(),$"{kv.Value.c * 100 / Math.Max(1, kv.Value.t)}%"
        }
        ).ToArray();
        SpectreTable.Render(["Topic", "Correct", "Total", "Score%"], rows);

    }

}

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
            }
            ;
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
        }
        ;
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
        SpectrePager.Show($"STAR: {a.QuestionText[..Math.Min(40, a.QuestionText.Length)]}", [$"[bold]Question:[/] {a.QuestionText.EscapeMarkup()}", "", $"[bold]S:[/] {a.Situation.EscapeMarkup()}", "", $"[bold]T:[/] {a.Task.EscapeMarkup()}", "", $"[bold]A:[/] {a.Action.EscapeMarkup()}", "", $"[bold]R:[/] {a.Result.EscapeMarkup()}", a.OutcomeMetric.Length > 0 ? $"\n[dim]{a.OutcomeMetric.EscapeMarkup()}[/]" : "", $"\n[dim]Created: {a.CreatedAt}[/]"]);

    }

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
            }
            );
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
            AnsiConsole.Clear();
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
            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;
            AnsiConsole.Clear();
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
        StudySession.Record($"Vocab {difficulty}", "vocabulary", "drill", new StudyScore(correct, total, total > 0 ? (correct * 100.0 / total) : 100.0), [.. weakItems], 0, duration, $"Reviewed Vocab {difficulty}");
    }
}

