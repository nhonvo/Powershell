using System.Text.Json;
using AgyTui.Domain.LearnContext;
using AgyTui.Domain.WorkspaceContext;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.DbContext;

public interface ILearningDataSeeder
{
    void SeedFromFiles();
}

public class LearningDataSeeder : ILearningDataSeeder
{
    private readonly ISqliteDatabase _db;
    private readonly IWorkspaceRepository _workspaceRepo;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public LearningDataSeeder(ISqliteDatabase db, IWorkspaceRepository workspaceRepo)
    {
        _db = db;
        _workspaceRepo = workspaceRepo;
    }

    public void SeedFromFiles()
    {
        SeedWorkspaces();
        SeedFlashcardDecks();
    }

    private void SeedWorkspaces()
    {
        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "priority_workspaces.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "csapp", "AgyTui", "priority_workspaces.json");
            }
            if (!File.Exists(jsonPath)) return;

            var json = File.ReadAllText(jsonPath);
            var entries = JsonSerializer.Deserialize<WorkspaceEntry[]>(json, JsonOpts);
            if (entries == null) return;

            foreach (var entry in entries)
            {
                var existing = _workspaceRepo.GetWorkspace(entry.Name);
                if (existing == null)
                {
                    var agg = WorkspaceAggregate.FromEntry(entry);
                    _workspaceRepo.SaveWorkspace(agg);
                }
            }
        }
        catch { }
    }

    private void SeedFlashcardDecks()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM flashcard_decks;";
            var countObj = cmd.ExecuteScalar();
            long count = countObj != null && countObj != DBNull.Value ? Convert.ToInt64(countObj) : 0;
            if (count > 0) return;

            var decksDir = LearnDataPaths.DecksDir;
            if (!Directory.Exists(decksDir)) return;

            var files = Directory.GetFiles(decksDir, "*.json");
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var deckFile = JsonSerializer.Deserialize<DeckFile>(text, JsonOpts);
                if (deckFile == null || deckFile.Meta == null) continue;

                var topic = deckFile.Meta.Topic;
                var now = DateTime.UtcNow.ToString("o");

                using var insertDeckCmd = conn.CreateCommand();
                insertDeckCmd.CommandText = """
                    INSERT INTO flashcard_decks (topic, cards_count, average_ease_factor, last_reviewed_utc)
                    VALUES (@topic, @count, @ease, @now)
                    ON CONFLICT(topic) DO UPDATE SET cards_count = @count, last_reviewed_utc = @now;
                    """;
                insertDeckCmd.Parameters.AddWithValue("@topic", topic);
                insertDeckCmd.Parameters.AddWithValue("@count", deckFile.Cards?.Length ?? 0);
                insertDeckCmd.Parameters.AddWithValue("@ease", 2.5);
                insertDeckCmd.Parameters.AddWithValue("@now", now);
                insertDeckCmd.ExecuteNonQuery();

                if (deckFile.Cards != null)
                {
                    foreach (var card in deckFile.Cards)
                    {
                        using var insertCardCmd = conn.CreateCommand();
                        insertCardCmd.CommandText = """
                            INSERT INTO flashcards (id, topic, front, back, ease_factor, interval_days, repetitions, next_review, status)
                            VALUES (@id, @topic, @front, @back, @ease, @interval, @reps, @next, @status)
                            ON CONFLICT(id) DO UPDATE SET front = @front, back = @back;
                            """;
                        insertCardCmd.Parameters.AddWithValue("@id", card.Id);
                        insertCardCmd.Parameters.AddWithValue("@topic", topic);
                        insertCardCmd.Parameters.AddWithValue("@front", card.Front);
                        insertCardCmd.Parameters.AddWithValue("@back", card.Back);
                        insertCardCmd.Parameters.AddWithValue("@ease", card.Sr?.EaseFactor ?? 2.5);
                        insertCardCmd.Parameters.AddWithValue("@interval", card.Sr?.IntervalDays ?? 0);
                        insertCardCmd.Parameters.AddWithValue("@reps", card.Sr?.Repetitions ?? 0);
                        insertCardCmd.Parameters.AddWithValue("@next", (object?)card.Sr?.NextReview?.ToString("o") ?? DBNull.Value);
                        insertCardCmd.Parameters.AddWithValue("@status", card.Sr?.Status ?? "new");
                        insertCardCmd.ExecuteNonQuery();
                    }
                }
            }
        }
        catch { }
    }
}
