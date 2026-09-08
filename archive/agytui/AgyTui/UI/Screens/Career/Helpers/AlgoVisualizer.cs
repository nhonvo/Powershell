namespace AgyTui.UI.Screens.Career.Helpers;

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
        for (int i = 0; i < a.Length - 1; i++)
            for (int j = 0; j < a.Length - i - 1; j++)
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
        for (int i = 0; i < merged.Length; i++) a[lo + i] = merged[i];
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
            ["A"] = ["B", "C"],
            ["B"] = ["D", "E"],
            ["C"] = ["F"],
            ["D"] = Array.Empty<string>(),
            ["E"] = ["F"],
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
            ScreenChrome.RenderFrame(() =>
            {
                AnsiConsole.Write(new Rule($"[bold cyan]BFS Step {step}: Visiting Node [{node}][/]").RuleStyle("grey"));
                AnsiConsole.MarkupLine($"[bold yellow]Queue:[/] {string.Join(" -> ", queue)}");
                AnsiConsole.MarkupLine($"[bold green]Visited:[/] {string.Join(", ", visited)}");
                AnsiConsole.WriteLine();
            });

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
                table.AddRow(k.ToString(), $"[bold green]{dp[k]}[/]", k >= 2 ? $"F({k - 1}) + F({k - 2}) = {dp[k - 1]} + {dp[k - 2]}" : "Base case");
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
        };
        for (int i = 0; i < a.Length; i++) t.AddColumn(new TableColumn("").Centered());
        t.AddRow(a.Select((v, i) =>
        {
            bool hl = i >= lo && i <= hi;
            return hl ? $"[green bold]{v}[/]" : v.ToString();
        }).ToArray());
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
        };
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
            }).ToArray();
            SpectreTable.Render(["Structure", "Access", "Search", "Insert", "Delete", "Space", "Notes"], rows);
        }
        else
        {
            var rows = (data?.Algorithms ?? GetDefaultAlgorithms()).Where(a => category == "Sorting Algorithms" ? a.Category == "sort" : a.Category == "search").Select(a => new[]
            {
                a.Name, a.Best, a.Average, a.Worst, a.Space, a.Notes
            }).ToArray();
            SpectreTable.Render(["Algorithm", "Best", "Average", "Worst", "Space", "Notes"], rows);
        }
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
        Console.ReadKey(true);
    }

    private static ComplexityEntry[] GetDefaultStructures() => [
        new("Array", "O(1)", "O(n)", "O(n)", "O(n)", "O(n)", "random access O(1)", []),
        new("Linked List", "O(n)", "O(n)", "O(1)", "O(1)", "O(n)", "prepend O(1)", []),
        new("Hash Table", "N/A", "O(1)", "O(1)", "O(1)", "O(n)", "worst O(n) collision", []),
        new("BST", "O(log n)", "O(log n)", "O(log n)", "O(log n)", "O(n)", "balanced only", []),
        new("Heap", "O(1)*", "O(n)", "O(log n)", "O(log n)", "O(n)", "*min/max only", []),
        new("Stack/Queue", "O(n)", "O(n)", "O(1)", "O(1)", "O(n)", "push/pop O(1)", []),
    ];

    private static AlgoEntry[] GetDefaultAlgorithms() => [
        new("Merge Sort", "O(n log n)", "O(n log n)", "O(n log n)", "O(n)", "sort", "stable", []),
        new("Quick Sort", "O(n log n)", "O(n log n)", "O(n²)", "O(log n)", "sort", "in-place", []),
        new("Heap Sort", "O(n log n)", "O(n log n)", "O(n log n)", "O(1)", "sort", "in-place", []),
        new("Bubble Sort", "O(n)", "O(n²)", "O(n²)", "O(1)", "sort", "simple", []),
        new("Binary Search", "O(1)", "O(log n)", "O(log n)", "O(1)", "search", "sorted array", []),
        new("BFS/DFS", "O(V+E)", "O(V+E)", "O(V+E)", "O(V)", "search", "graph traversal", []),
    ];
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
            }).ToArray();
            SpectreTable.Render(["Title", "Diff", "Topics", "Status"], rows, markup: true);
            var actions = new[]
            {
                "[n] Add problem","[f] Filter by topic","← Back"
            };
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
