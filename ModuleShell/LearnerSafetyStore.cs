using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class LearnerSafetyStore
{
    private readonly string _connectionString;

    internal LearnerSafetyStore(string moduleRoot)
    {
        var dataRoot = Path.Combine(moduleRoot, "data");
        Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        }.ToString();
        Initialize();
    }

    internal LearnerSafetyIncident Record(string learnerId, string lessonId, string surface, string rawInput, LearnerSafetyEvaluation evaluation)
    {
        if (evaluation.Allowed || evaluation.Categories.Count == 0) throw new ArgumentException("An allowed input is not a safety incident.", nameof(evaluation));
        using var connection = OpenConnection();
        var salt = GetOrCreateSalt(connection);
        var categoryText = string.Join(',', evaluation.Categories);
        var fingerprint = Convert.ToHexString(HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(rawInput.Normalize(NormalizationForm.FormKC)))).ToLowerInvariant();
        var idMaterial = $"{learnerId}\n{lessonId}\n{surface}\n{categoryText}\n{fingerprint}";
        var id = "safety-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idMaterial))).ToLowerInvariant()[..24];
        var now = DateTimeOffset.UtcNow.ToString("O");
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO learner_safety_incidents(id, first_seen_utc, last_seen_utc, learner_id, lesson_id, surface, categories, action, input_length, content_hmac, occurrence_count)
            VALUES ($id, $now, $now, $learner, $lesson, $surface, $categories, 'blocked-and-reported', $length, $hmac, 1)
            ON CONFLICT(id) DO UPDATE SET last_seen_utc=excluded.last_seen_utc, occurrence_count=learner_safety_incidents.occurrence_count+1;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$now", now);
        command.Parameters.AddWithValue("$learner", Bound(learnerId, 160));
        command.Parameters.AddWithValue("$lesson", Bound(lessonId, 160));
        command.Parameters.AddWithValue("$surface", Bound(surface, 80));
        command.Parameters.AddWithValue("$categories", categoryText);
        command.Parameters.AddWithValue("$length", evaluation.InputLength);
        command.Parameters.AddWithValue("$hmac", fingerprint);
        command.ExecuteNonQuery();
        return Get(connection, id)!;
    }

    internal LearnerSafetyOverview GetOverview(int limit = 200)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, first_seen_utc, last_seen_utc, learner_id, lesson_id, surface, categories, action, input_length, occurrence_count
            FROM learner_safety_incidents ORDER BY last_seen_utc DESC, id LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 500));
        using var reader = command.ExecuteReader();
        var incidents = new List<LearnerSafetyIncident>();
        while (reader.Read()) incidents.Add(Read(reader));
        return new(true, incidents, new[]
        {
            "Reports contain categories and an installation-salted fingerprint, not the rejected text.",
            "Repeated identical attempts increment one incident instead of flooding the database.",
            "A report is evidence for teacher follow-up, not an automatic punishment or diagnosis."
        });
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS learner_safety_metadata(key TEXT PRIMARY KEY, value BLOB NOT NULL);
            CREATE TABLE IF NOT EXISTS learner_safety_incidents(
                id TEXT PRIMARY KEY,
                first_seen_utc TEXT NOT NULL,
                last_seen_utc TEXT NOT NULL,
                learner_id TEXT NOT NULL,
                lesson_id TEXT NOT NULL,
                surface TEXT NOT NULL,
                categories TEXT NOT NULL,
                action TEXT NOT NULL,
                input_length INTEGER NOT NULL,
                content_hmac TEXT NOT NULL,
                occurrence_count INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_learner_safety_last_seen ON learner_safety_incidents(last_seen_utc DESC);
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static byte[] GetOrCreateSalt(SqliteConnection connection)
    {
        using (var select = connection.CreateCommand())
        {
            select.CommandText = "SELECT value FROM learner_safety_metadata WHERE key='content-hmac-salt';";
            if (select.ExecuteScalar() is byte[] existing && existing.Length == 32) return existing;
        }
        var salt = RandomNumberGenerator.GetBytes(32);
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT OR IGNORE INTO learner_safety_metadata(key,value) VALUES ('content-hmac-salt',$value);";
        insert.Parameters.AddWithValue("$value", salt);
        insert.ExecuteNonQuery();
        using var reload = connection.CreateCommand();
        reload.CommandText = "SELECT value FROM learner_safety_metadata WHERE key='content-hmac-salt';";
        return (byte[])reload.ExecuteScalar()!;
    }

    private static LearnerSafetyIncident? Get(SqliteConnection connection, string id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, first_seen_utc, last_seen_utc, learner_id, lesson_id, surface, categories, action, input_length, occurrence_count FROM learner_safety_incidents WHERE id=$id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    private static LearnerSafetyIncident Read(SqliteDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5),
        reader.GetString(6).Split(',', StringSplitOptions.RemoveEmptyEntries), reader.GetString(7), reader.GetInt32(8), reader.GetInt32(9));
    private static string Bound(string value, int maximum) => value.Trim().Length <= maximum ? value.Trim() : value.Trim()[..maximum];
}

internal sealed record LearnerSafetyOverview(bool Ok, IReadOnlyList<LearnerSafetyIncident> Incidents, IReadOnlyList<string> Boundaries);
internal sealed record LearnerSafetyIncident(string Id, string FirstSeenUtc, string LastSeenUtc, string LearnerId, string LessonId, string Surface, IReadOnlyList<string> Categories, string Action, int InputLength, int OccurrenceCount);
