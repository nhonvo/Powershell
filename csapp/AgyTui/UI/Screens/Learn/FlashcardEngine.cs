namespace AgyTui.UI.Screens.Learn;

public sealed record FlashCard(string Id, string Front, string Back, string? Hint, string? Mnemonic, string? ExampleSentence, string[] Tags, int Difficulty, SrState Sr);

public sealed record DeckMeta(string Id, string Title, string Language, string Topic, string Level, string[] SourceNotes, string GeneratedAt, int Version);

public sealed record DeckFile(DeckMeta Meta, FlashCard[] Cards);

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
        Run(deck.Cards, deck.Meta.Title, deckPath, deck);
    }

    public static void Run(FlashCard[] cards, string deckName, string? deckPath = null, DeckFile? deck = null, Action<FlashCard[]>? onSave = null)
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
        var start = DateTime.Now;
        var weakItems = new List<string>();

        foreach (var card in queue)
        {
            ScreenChrome.RenderFrame(() =>
            {
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
            });
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape) break;

            ScreenChrome.RenderFrame(() =>
            {
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
            });

            bool knewIt = AnsiConsole.Confirm("[bold]Did you know it?[/]", defaultValue: false);
            int quality = knewIt ? 4 : 1;
            var srResult = SpacedRepetitionEngine.UpdateCard(card.Sr, quality);

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].Id == card.Id)
                {
                    cards[i] = cards[i] with { Sr = srResult.Updated };
                    break;
                }
            }

            if (knewIt)
            {
                known++;
            }
            else
            {
                again++;
                weakItems.Add(card.Front);
            }
        }

        if (deckPath != null && deck != null)
        {
            var updatedDeck = deck with { Cards = cards };
            LearnDataPaths.SaveJson(deckPath, updatedDeck);
        }
        else if (onSave != null)
        {
            onSave(cards);
        }

        AnsiConsole.Clear();
        SpectrePanel.Success($"Session complete — ✓ {known} known ✗ {again} missed ({queue.Count} cards reviewed)");

        var duration = (int)(DateTime.Now - start).TotalMinutes;
        StudySession.Record(deckName, "cards", "review", new StudyScore(known, known + again, known + again > 0 ? (known * 100.0 / (known + again)) : 100.0), [.. weakItems], 0, duration, $"Reviewed {deckName} deck", start);

        try
        {
            ObsidianStudySync.OfferSync(new StudySummary(deckName, known, known + again, [.. weakItems], duration));
        }
        catch { }
    }

    public static (string FilePath, DeckFile Deck)[] GetDecksWithPaths(string decksDir)
    {
        if (!Directory.Exists(decksDir)) return [];
        return Directory.GetFiles(decksDir, "*.json")
            .Select(f => (FilePath: f, Deck: LearnDataPaths.LoadJson<DeckFile>(f)))
            .Where(pair => pair.Deck != null)
            .Select(pair => (pair.FilePath, pair.Deck!))
            .ToArray();
    }

    public static void PickAndRun(string decksDir)
    {
        var decks = GetDecksWithPaths(decksDir);
        if (decks.Length == 0)
        {
            SpectrePanel.Warning($"No decks found in {decksDir}");
            return;
        }
        var names = decks.Select(d => d.Deck.Meta.Title).ToArray();
        var idx = SpectreMenu.Show("Select Flashcard Deck", names, 0, true);
        if (idx >= 0)
        {
            Run(decks[idx].Deck.Cards, decks[idx].Deck.Meta.Title, decks[idx].FilePath, decks[idx].Deck);
        }
    }
}
