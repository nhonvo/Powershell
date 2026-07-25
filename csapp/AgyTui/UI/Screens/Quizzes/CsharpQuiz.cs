using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AgyTui.UI.Screens.Quizzes;

public sealed record CodeSnippet(string Id, string Title, string Category, string Code, string Explanation, string UseCase, string[] Tags, int Difficulty);

public sealed record SnippetsFile(string Language, CodeSnippet[] Snippets);

public static class SnippetLibrary
{
    public static void Run()
    {
        var langs = Directory.Exists(LearnDataPaths.SnippetsDir) ? Directory.GetFiles(LearnDataPaths.SnippetsDir, "*.json").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToArray() : new[]
        {
            "csharp","powershell","sql"
        };
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
        };
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
                };

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
        };
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
        }).ToArray();
        SpectreTable.Render(["Topic", "Correct", "Total", "Score%"], rows);
    }
}
