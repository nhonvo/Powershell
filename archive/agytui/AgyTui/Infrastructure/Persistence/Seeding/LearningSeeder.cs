using System.Text.Json;

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
        SeedFlashcardDecks();
        SeedInterviewQuestions();
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

            var learnDir = AppPaths.LearnDir;
            if (!Directory.Exists(learnDir))
            {
                learnDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "learn");
            }
            if (!Directory.Exists(learnDir)) return;

            var files = Directory.GetFiles(learnDir, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (Path.GetFileName(file).Equals("interview_questions.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
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
                catch { }
            }
        }
        catch { }
    }

    private void SeedInterviewQuestions()
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM quiz_questions;";
            var countObj = cmd.ExecuteScalar();
            long count = countObj != null && countObj != DBNull.Value ? Convert.ToInt64(countObj) : 0;
            if (count > 0) return;

            var questionsFile = Path.Combine(AppPaths.LearnDir, "interview_questions.json");
            if (!File.Exists(questionsFile))
            {
                questionsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "learn", "interview_questions.json");
            }
            if (!File.Exists(questionsFile)) return;

            var text = File.ReadAllText(questionsFile);
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("Questions", out var questionsElem) || questionsElem.ValueKind != JsonValueKind.Array) return;

            var now = DateTime.UtcNow.ToString("o");
            foreach (var q in questionsElem.EnumerateArray())
            {
                var id = q.TryGetProperty("Id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                var category = q.TryGetProperty("Category", out var cProp) ? cProp.GetString() ?? "general" : "general";
                var type = q.TryGetProperty("Type", out var tProp) ? tProp.GetString() : null;
                var diff = q.TryGetProperty("Difficulty", out var dProp) ? dProp.GetString() : null;
                var questionText = q.TryGetProperty("Question", out var qProp) ? qProp.GetString() ?? "" : "";
                var format = q.TryGetProperty("Format", out var fProp) ? fProp.GetString() : null;
                var hints = q.TryGetProperty("Hints", out var hProp) ? hProp.GetRawText() : null;
                var companies = q.TryGetProperty("Companies", out var compProp) ? compProp.GetRawText() : null;
                var tags = q.TryGetProperty("Tags", out var tagProp) ? tagProp.GetRawText() : null;

                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO quiz_questions (id, category, type, difficulty, question, format, hints_json, companies_json, tags_json, updated_at)
                    VALUES (@id, @cat, @type, @diff, @qText, @format, @hints, @comp, @tags, @now)
                    ON CONFLICT(id) DO UPDATE SET question = @qText, updated_at = @now;
                    """;
                insertCmd.Parameters.AddWithValue("@id", id);
                insertCmd.Parameters.AddWithValue("@cat", category);
                insertCmd.Parameters.AddWithValue("@type", (object?)type ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@diff", (object?)diff ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@qText", questionText);
                insertCmd.Parameters.AddWithValue("@format", (object?)format ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@hints", (object?)hints ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@comp", (object?)companies ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@tags", (object?)tags ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@now", now);
                insertCmd.ExecuteNonQuery();
            }
        }
        catch { }
    }
}
