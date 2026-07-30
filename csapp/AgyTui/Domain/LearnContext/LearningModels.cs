namespace AgyTui.Domain.LearnContext;

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

public sealed record VocabWord(string Id, string Word, string Pronunciation, string PartOfSpeech, string Definition, string ExampleSentence, string[] Synonyms, string[] Antonyms, int Difficulty, string[] Tags, SrState Sr);

public sealed record VocabFile(string Level, VocabWord[] Words);
