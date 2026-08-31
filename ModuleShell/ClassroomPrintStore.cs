using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class ClassroomPrintStore
{
    private readonly string _connectionString;

    internal ClassroomPrintStore(string moduleRoot)
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

    internal ClassroomPrintMutation Request(string learnerId, string lessonId, string kind)
    {
        kind = NormalizeKind(kind);
        if (kind.Length == 0) return new(false, "invalid", null, "Choose lesson or feedback.");
        using var connection = OpenConnection();
        using (var existing = connection.CreateCommand())
        {
            existing.CommandText = "SELECT id FROM classroom_print_requests WHERE learner_id=$learner AND lesson_id=$lesson AND document_kind=$kind AND state='pending' ORDER BY requested_utc DESC LIMIT 1;";
            existing.Parameters.AddWithValue("$learner", Bound(learnerId, 160));
            existing.Parameters.AddWithValue("$lesson", Bound(lessonId, 160));
            existing.Parameters.AddWithValue("$kind", kind);
            if (existing.ExecuteScalar() is string existingId) return new(true, "already-pending", existingId, null);
        }
        var id = $"print-{Guid.NewGuid():N}";
        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO classroom_print_requests(id,requested_utc,learner_id,lesson_id,document_kind,state) VALUES($id,$now,$learner,$lesson,$kind,'pending');";
        insert.Parameters.AddWithValue("$id", id);
        insert.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        insert.Parameters.AddWithValue("$learner", Bound(learnerId, 160));
        insert.Parameters.AddWithValue("$lesson", Bound(lessonId, 160));
        insert.Parameters.AddWithValue("$kind", kind);
        insert.ExecuteNonQuery();
        return new(true, "pending-teacher-approval", id, null);
    }

    internal ClassroomPrintOverview GetOverview(int limit = 200)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,requested_utc,learner_id,lesson_id,document_kind,state,reviewed_utc,printer_name,error
            FROM classroom_print_requests ORDER BY requested_utc DESC,id LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 500));
        using var reader = command.ExecuteReader();
        var requests = new List<ClassroomPrintRequest>();
        while (reader.Read()) requests.Add(Read(reader));
        return new(true, requests);
    }

    internal ClassroomPrintRequest? Get(string id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,requested_utc,learner_id,lesson_id,document_kind,state,reviewed_utc,printer_name,error FROM classroom_print_requests WHERE id=$id;";
        command.Parameters.AddWithValue("$id", Bound(id, 160));
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    internal ClassroomPrintMutation Complete(string id, string expectedState, string state, string? printerName, string? error)
    {
        using var connection = OpenConnection();
        using var update = connection.CreateCommand();
        update.CommandText = """
            UPDATE classroom_print_requests SET state=$state,reviewed_utc=$now,printer_name=$printer,error=$error
            WHERE id=$id AND state=$expected;
            """;
        update.Parameters.AddWithValue("$id", Bound(id, 160));
        update.Parameters.AddWithValue("$expected", expectedState);
        update.Parameters.AddWithValue("$state", Bound(state, 40));
        update.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        update.Parameters.AddWithValue("$printer", (object?)printerName ?? DBNull.Value);
        update.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value);
        return update.ExecuteNonQuery() == 1
            ? new(true, state, id, error)
            : new(false, "conflict", id, "The print request is no longer pending.");
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS classroom_print_requests(
                id TEXT PRIMARY KEY,
                requested_utc TEXT NOT NULL,
                learner_id TEXT NOT NULL,
                lesson_id TEXT NOT NULL,
                document_kind TEXT NOT NULL CHECK(document_kind IN ('lesson','feedback')),
                state TEXT NOT NULL CHECK(state IN ('pending','printed','declined','failed')),
                reviewed_utc TEXT NULL,
                printer_name TEXT NULL,
                error TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_classroom_print_state ON classroom_print_requests(state,requested_utc DESC);
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

    private static ClassroomPrintRequest Read(SqliteDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8));
    private static string NormalizeKind(string? value) => value?.Trim().ToLowerInvariant() is "lesson" or "feedback" ? value.Trim().ToLowerInvariant() : string.Empty;
    private static string Bound(string value, int maximum) => value.Trim().Length <= maximum ? value.Trim() : value.Trim()[..maximum];
}

internal sealed record ClassroomPrintOverview(bool Ok, IReadOnlyList<ClassroomPrintRequest> Requests);
internal sealed record ClassroomPrintRequest(string Id, string RequestedUtc, string LearnerId, string LessonId, string DocumentKind, string State, string? ReviewedUtc, string? PrinterName, string? Error);
internal sealed record ClassroomPrintMutation(bool Ok, string State, string? Id, string? Error);
internal sealed record ClassroomPrintRequestInput(string Kind);
