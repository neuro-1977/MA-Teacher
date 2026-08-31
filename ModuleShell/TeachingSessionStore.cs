using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class TeachingSessionStore
{
    private const int SchemaVersion = 1;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> DeliveryModes = new(StringComparer.Ordinal) { "in-person", "remote", "self-directed", "other" };
    private static readonly HashSet<string> DeliveryStates = new(StringComparer.Ordinal) { "delivery-finished", "delivery-paused", "delivery-stopped", "not-started" };
    private readonly string _connectionString;
    private readonly LessonReviewStore _lessonReviews;

    public TeachingSessionStore(string moduleRoot, LessonReviewStore lessonReviews)
    {
        _lessonReviews = lessonReviews;
        var dataRoot = Path.Combine(moduleRoot, "data");
        Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public TeachingSessionOverview GetOverview()
    {
        var approvedLessons = _lessonReviews.GetOverview().Lessons
            .Where(value => value.LatestDecision == "approved-for-use" && value.LatestReviewCurrent)
            .Select(value => new TeachingSessionLesson(value.Id, value.Title, value.Subject, value.LearningStage, value.CurrentFingerprint))
            .ToArray();
        var receipts = new List<TeachingSessionReceipt>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, lesson_id, lesson_fingerprint, lesson_review_id, facilitator_identity, delivered_utc,
                   delivery_mode, delivery_state, delivery_context, covered_content, adaptations,
                   interruptions, continuation_note, recorded_utc
            FROM teaching_session_receipts ORDER BY sequence, id;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read()) receipts.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
            reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetString(12), reader.GetString(13)));
        return new(true, "install-root-sqlite", SchemaVersion, approvedLessons, receipts,
        [
            "A receipt records a claimed delivery event against one exact approved lesson fingerprint; it does not prove teaching quality or learner attendance.",
            "Delivery state describes the activity only. It is not a score, grade, mastery decision, diagnosis or learner classification.",
            "Facilitator identity is a recorded claim, not authentication or proof of qualification.",
            "Receipts are immutable and local. Corrections require a new receipt that names the earlier record in its continuation note.",
            "No model invocation, remote transmission, attendance inference or progress mutation exists in this workflow.",
        ]);
    }

    public TeachingSessionMutation Record(TeachingSessionInput input)
    {
        try
        {
            var id = RequireId(input.SessionId, "session id");
            var lessonId = RequireId(input.LessonId, "lesson id");
            var facilitator = RequireText(input.FacilitatorIdentity, "facilitator identity", 2, 120);
            var deliveredUtc = RequireUtc(input.DeliveredUtc);
            var mode = RequireChoice(input.DeliveryMode, "delivery mode", DeliveryModes);
            var state = RequireChoice(input.DeliveryState, "delivery state", DeliveryStates);
            var context = RequireText(input.DeliveryContext, "delivery context", 4, 1000);
            var covered = RequireText(input.CoveredContent, "covered content", 4, 4000);
            var adaptations = RequireText(input.Adaptations, "adaptations", 4, 4000);
            var interruptions = RequireText(input.Interruptions, "interruptions", 4, 4000);
            var continuation = RequireText(input.ContinuationNote, "continuation note", 4, 4000);
            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            var lesson = _lessonReviews.GetCurrentApprovedLesson(connection, transaction, lessonId);
            if (lesson is null)
            {
                transaction.Rollback();
                return new(false, "approval-required", id, null, "A current approved-for-use review is required for the exact saved lesson fingerprint.");
            }
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT lesson_id, lesson_fingerprint, lesson_review_id, facilitator_identity, delivered_utc,
                           delivery_mode, delivery_state, delivery_context, covered_content, adaptations, interruptions, continuation_note
                    FROM teaching_session_receipts WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id);
                var same = false; var found = false;
                using (var reader = existing.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        found = true;
                        same = reader.GetString(0) == lessonId && reader.GetString(1) == lesson.Fingerprint
                            && reader.GetString(2) == lesson.ReviewId && reader.GetString(3) == facilitator
                            && reader.GetString(4) == deliveredUtc && reader.GetString(5) == mode && reader.GetString(6) == state
                            && reader.GetString(7) == context && reader.GetString(8) == covered && reader.GetString(9) == adaptations
                            && reader.GetString(10) == interruptions && reader.GetString(11) == continuation;
                    }
                }
                if (found)
                {
                    transaction.Rollback();
                    return same ? new(true, "already-present", id, lesson.Fingerprint, null)
                        : new(false, "conflict", id, lesson.Fingerprint, "Session id already exists with different delivery evidence.");
                }
            }
            var now = DateTimeOffset.UtcNow.ToString("O");
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO teaching_session_receipts(id, lesson_id, lesson_fingerprint, lesson_review_id,
                        facilitator_identity, delivered_utc, delivery_mode, delivery_state, delivery_context,
                        covered_content, adaptations, interruptions, continuation_note, recorded_utc)
                    VALUES ($id, $lesson, $fingerprint, $review, $facilitator, $delivered, $mode, $state,
                        $context, $covered, $adaptations, $interruptions, $continuation, $recorded);
                    """;
                insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$lesson", lessonId);
                insert.Parameters.AddWithValue("$fingerprint", lesson.Fingerprint); insert.Parameters.AddWithValue("$review", lesson.ReviewId);
                insert.Parameters.AddWithValue("$facilitator", facilitator); insert.Parameters.AddWithValue("$delivered", deliveredUtc);
                insert.Parameters.AddWithValue("$mode", mode); insert.Parameters.AddWithValue("$state", state);
                insert.Parameters.AddWithValue("$context", context); insert.Parameters.AddWithValue("$covered", covered);
                insert.Parameters.AddWithValue("$adaptations", adaptations); insert.Parameters.AddWithValue("$interruptions", interruptions);
                insert.Parameters.AddWithValue("$continuation", continuation); insert.Parameters.AddWithValue("$recorded", now);
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
            return new(true, "session-recorded", id, lesson.Fingerprint, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, null, exception.Message); }
    }

    private void Initialize()
    {
        using var connection = OpenConnection(); using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS teaching_session_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS teaching_session_receipts (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE,
                lesson_id TEXT NOT NULL REFERENCES lesson_records(id), lesson_fingerprint TEXT NOT NULL,
                lesson_review_id TEXT NOT NULL REFERENCES lesson_reviews(id), facilitator_identity TEXT NOT NULL,
                delivered_utc TEXT NOT NULL, delivery_mode TEXT NOT NULL, delivery_state TEXT NOT NULL,
                delivery_context TEXT NOT NULL, covered_content TEXT NOT NULL, adaptations TEXT NOT NULL,
                interruptions TEXT NOT NULL, continuation_note TEXT NOT NULL, recorded_utc TEXT NOT NULL
            );
            INSERT INTO teaching_session_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            CREATE INDEX IF NOT EXISTS idx_teaching_sessions_lesson ON teaching_session_receipts(lesson_id, sequence);
            """;
        command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString()); command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }
    private static string RequireId(string? value, string field) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-64 lowercase letters, numbers, hyphens or underscores."); return normalized; }
    private static string RequireText(string? value, string field, int minimum, int maximum) { var normalized = (value ?? "").Trim(); if (normalized.Length < minimum || normalized.Length > maximum) throw new ArgumentException($"{field} must be {minimum}-{maximum} characters."); return normalized; }
    private static string RequireChoice(string? value, string field, HashSet<string> choices) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!choices.Contains(normalized)) throw new ArgumentException($"{field} is not supported."); return normalized; }
    private static string RequireUtc(string? value) { if (!DateTimeOffset.TryParse(value, out var parsed)) throw new ArgumentException("delivered UTC must be a valid timestamp."); return parsed.ToUniversalTime().ToString("O"); }
}

internal sealed record TeachingSessionOverview(bool Ok, string DatabaseAuthority, int SchemaVersion,
    IReadOnlyList<TeachingSessionLesson> Lessons, IReadOnlyList<TeachingSessionReceipt> Receipts, IReadOnlyList<string> Boundaries);
internal sealed record TeachingSessionLesson(string Id, string Title, string Subject, string LearningStage, string CurrentFingerprint);
internal sealed record TeachingSessionReceipt(string Id, string LessonId, string LessonFingerprint, string LessonReviewId,
    string FacilitatorIdentity, string DeliveredUtc, string DeliveryMode, string DeliveryState, string DeliveryContext,
    string CoveredContent, string Adaptations, string Interruptions, string ContinuationNote, string RecordedUtc);
internal sealed record TeachingSessionInput(string SessionId, string LessonId, string FacilitatorIdentity, string DeliveredUtc,
    string DeliveryMode, string DeliveryState, string DeliveryContext, string CoveredContent, string Adaptations,
    string Interruptions, string ContinuationNote);
internal sealed record TeachingSessionMutation(bool Ok, string State, string? Id, string? LessonFingerprint, string? Error);
