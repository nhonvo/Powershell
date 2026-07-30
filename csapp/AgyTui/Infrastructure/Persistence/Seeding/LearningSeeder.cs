using System.Text.Json;
using AgyTui.Domain.LearnContext;
using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Seeding;

public class LearningSeeder : ISeeder
{
    private readonly ISqliteDatabase _db;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public int Order => 3;

    public LearningSeeder(ISqliteDatabase db)
    {
        _db = db;
    }

    public void Seed()
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
