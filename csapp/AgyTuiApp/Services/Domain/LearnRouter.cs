using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class LearnRouter
{
    public static void StartLearning(string topic)
    {
        LearnDataPaths.EnsureDirectories();
        RefreshData(topic);
        if (string.IsNullOrWhiteSpace(topic) || topic.Equals("all", StringComparison.OrdinalIgnoreCase) || topic.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            LaunchMasterHub();
        }
        else
        {
            LaunchTool(topic, "auto");
        }
    }

    public static void LaunchMasterHub()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold cyan]🎓 Antigravity Master Learning Suite[/]").RuleStyle("grey"));

            var options = new[]
            {
                "🎌 Japanese Language Suite (Kana, Kanji, JLPT)",
                "📖 English & Vocabulary (Vocab Drill, Word of Day, Flashcards)",
                "💻 C# & .NET Masterclass (Quiz, Snippets, Cheat Sheets)",
                "🧩 DSA & System Architecture (Algo Visualizer, Big-O, Tracker)",
                "💼 Career & Technical Interview (Questions, STAR Builder, Mock)",
                "📊 Progress & Spaced Repetition Queue",
                "← Exit Learning Suite"
            };

            var idx = SpectreMenu.Show("Select Learning Domain", options, 0);
            if (idx == 0) LaunchTool("jp", "auto");
            else if (idx == 1) LaunchTool("en", "auto");
            else if (idx == 2) LaunchTool("cs", "auto");
            else if (idx == 3) LaunchTool("dsa", "auto");
            else if (idx == 4) LaunchTool("interview", "auto");
            else if (idx == 5) ProgressDashboard.Show();
            else break;
        }
    }

    public static void RefreshData(string topic)
    {
        var cfg = ObsidianBridge.LoadConfig();
        if (cfg == null || !Directory.Exists(cfg.VaultPath))
        {
            return;
        }
        var tagMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["jp"] = ["japanese", "jp", "jlpt"],
            ["japanese"] = ["japanese", "jp", "jlpt"],
            ["en"] = ["english", "vocab", "idiom", "grammar"],
            ["english"] = ["english", "vocab", "idiom"],
            ["cs"] = ["csharp", "dotnet", "cs", "snippet"],
            ["csharp"] = ["csharp", "dotnet", "cs"],
            ["dsa"] = ["dsa", "algorithm", "leetcode", "problem"],
            ["interview"] = ["interview", "behavioral", "system-design"],
        };
        var tags = tagMap.GetValueOrDefault(topic, [topic]);
        var notes = ResourceScanner.FindNotesByTag(cfg.VaultPath, tags);
        if (notes.Length == 0)
        {
            return;
        }
        var items = new List<ExtractedItem>();
        SpectreProgress.BulkProgress($"Extracting {notes.Length} notes", notes, (_, note) =>
        {
            var fakeEntry = new ResourceEntry("tmp", note, "md", Path.GetFileNameWithoutExtension(note), tags, [topic], "auto", "obsidian_note", null, 0, DateTimeOffset.Now.ToString("o"), null, "pending", null, 0, [], true, true);
            items.AddRange(MdExtractor.Extract(note, fakeEntry));
        });
        TemplateGenerator.RouteItemsToFiles([.. items]);
        SpectrePanel.Success($"Generated {items.Count} items from {notes.Length} notes → learn/");
    }

    public static void LaunchTool(string topic, string level)
    {
        LearnDataPaths.EnsureDirectories();
        switch (topic.ToLower())
        {
            case "jp" or "japanese":
                var jpTools = new[]
                {
                    "🎌 JLPT Vocabulary Drill (N5)",
                    "🌸 Hiragana & Katakana Kana Quiz",
                    "⛩️ Kanji Radical & Stroke Lookup",
                    "📘 Japanese Grammar Drill (N5–N2)"
                };
                var jpChoice = SpectreMenu.Show("Japanese Learning Suite", jpTools, 0);
                if (jpChoice == 0) JlptVocabDrill.Run("N5");
                else if (jpChoice == 1) KanaQuiz.Run("hiragana");
                else if (jpChoice == 2) KanjiLookup.Run();
                else if (jpChoice == 3)
                {
                    var lvl = SpectreMenu.Show("Grammar Level", new[] { "N5", "N4", "N3" }, 0);
                    if (lvl == 0) GrammarQuiz.Run("N5");
                    else if (lvl == 1) GrammarQuiz.Run("N4");
                    else if (lvl == 2) GrammarQuiz.Run("N3");
                }
                break;
            case "en" or "english":
                var enTools = new[]
                {
                    "📖 English Vocab Drill",
                    "📘 English Grammar Drill",
                    "🌟 Word of the Day",
                    "🎴 Flashcard Decks"
                };
                var enChoice = SpectreMenu.Show("English & Vocabulary Suite", enTools, 0);
                if (enChoice == 0) VocabDrill.Run("Intermediate");
                else if (enChoice == 1) GrammarQuiz.Run("English");
                else if (enChoice == 2)
                {
                    var word = WordOfDay.Pick();
                    if (word != null) WordOfDay.Render(word);
                    else SpectrePanel.Warning("No word of the day available.");
                }
                else if (enChoice == 3) FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                break;
            case "cs" or "csharp":
                var csTools = new[]
                {
                    "💻 C# & .NET Interactive Quiz",
                    "⚡ Code Snippet Library",
                    "📄 Developer Cheat Sheets"
                };
                var csChoice = SpectreMenu.Show("C# & Dev Masterclass Suite", csTools, 0);
                if (csChoice == 0) CsharpQuiz.Run();
                else if (csChoice == 1) SnippetLibrary.Run();
                else if (csChoice == 2) CheatSheetBrowser.Run();
                break;
            case "dsa":
                var dsaTools = new[]
                {
                    "🧩 Algorithm Step Visualizer",
                    "📊 Big-O Complexity Sheet",
                    "🎯 Coding Problem Tracker"
                };
                var dsaChoice = SpectreMenu.Show("DSA & System Architecture Suite", dsaTools, 0);
                if (dsaChoice == 0) AlgoVisualizer.PickAndRun();
                else if (dsaChoice == 1) ComplexitySheet.Run();
                else if (dsaChoice == 2) ProblemTracker.Run();
                break;
            case "interview":
                var intTools = new[]
                {
                    "💼 Technical & Behavioral Question Bank",
                    "⭐ STAR Answer Builder",
                    "⏱️ Mock Interview Session Timer"
                };
                var intChoice = SpectreMenu.Show("Career & Interview Suite", intTools, 0);
                if (intChoice == 0) InterviewBank.Run();
                else if (intChoice == 1) StarBuilder.Run();
                else if (intChoice == 2) MockInterviewTimer.Run(300);
                break;
            default:
                FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                break;
        }
    }
}
