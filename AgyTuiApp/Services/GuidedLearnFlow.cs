using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class GuidedLearnFlow
{
    public static void Run()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]🧭 Guided Learning Suite[/]").RuleStyle("grey"));

        var decks = FlashcardEngine.GetDecksWithPaths(LearnDataPaths.DecksDir);
        int totalDueDecks = 0;
        int totalDueCards = 0;

        var dueSummary = new List<string>();
        foreach (var (path, deck) in decks)
        {
            var dueCount = deck.Cards.Count(c => SpacedRepetitionEngine.IsDueToday(c.Sr));
            if (dueCount > 0)
            {
                totalDueDecks++;
                totalDueCards += dueCount;
                var masteryIcon = deck.Cards.Length > 0 ? Icons.GetMasteryIcon(deck.Cards[0].Sr) : "🌱";
                dueSummary.Add($" {masteryIcon} [cyan]{deck.Meta.Title.EscapeMarkup()}[/]: {dueCount} cards due");
            }
        }

        if (dueSummary.Count == 0)
        {
            SpectrePanel.Success("🎉 All learning decks are completely up-to-date for today!");
            AnsiConsole.MarkupLine("[dim]Press any key to choose a specific deck...[/]");
            Console.ReadKey(true);
            FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
            return;
        }

        AnsiConsole.Write(new Panel(string.Join("\n", dueSummary))
        {
            Header = new PanelHeader($"[yellow bold]Due Reviews Today ({totalDueCards} cards in {totalDueDecks} decks)[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(1, 1)
        });

        var choices = new[]
        {
            "▶ Start Guided Review (All Due Decks)",
            "🎴 Select Specific Flashcard Deck",
            "🎌 JLPT Vocab Drill",
            "📖 English Vocab Drill",
            "📘 Grammar Quiz",
            "📊 Review Study Stats & Streaks"
        };

        int selected = SpectreMenu.Show("Select Action", choices, 0);
        if (selected == 0)
        {
            foreach (var (path, deck) in decks)
            {
                var dueCount = deck.Cards.Count(c => SpacedRepetitionEngine.IsDueToday(c.Sr));
                if (dueCount > 0)
                {
                    FlashcardEngine.Run(deck.Cards, deck.Meta.Title, path, deck);
                }
            }
        }
        else if (selected == 1)
        {
            FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
        }
        else if (selected == 2)
        {
            JlptVocabDrill.Run("N5");
        }
        else if (selected == 3)
        {
            VocabDrill.Run("Intermediate");
        }
        else if (selected == 4)
        {
            GrammarQuiz.Run("N5");
        }
        else if (selected == 5)
        {
            StudyStats.Run();
        }
    }
}
