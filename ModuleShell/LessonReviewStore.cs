using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class LessonReviewStore
{
    private const int SchemaVersion = 1;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly string[] CriterionIds =
    [
        "evidence-linked", "source-context", "coverage-honest", "goal-specific", "prerequisites-explicit", "activity-aligned",
        "content-accurate", "disciplinary-action", "vocabulary-meaningful", "model-visible", "practice-progresses", "misconceptions-bounded",
        "age-respectful", "demand-separated", "support-removable", "prompt-aligned", "criteria-observable", "feedback-bounded",
        "activity-safe", "data-minimal", "disclosure-route", "reader-complete", "interaction-usable", "derivative-reviewed",
    ];
    private static readonly HashSet<string> CriterionIdSet = new(CriterionIds, StringComparer.Ordinal);
    private static readonly HashSet<string> CriterionOutcomes = new(StringComparer.Ordinal) { "met", "failed", "not-applicable" };
    private static readonly HashSet<string> Decisions = new(StringComparer.Ordinal) { "approved-for-use", "changes-required", "deferred" };
    private readonly string _connectionString;

    public LessonReviewStore(string moduleRoot)
    {
        var dataRoot = Path.GetFullPath(moduleRoot);
        Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public LessonReviewOverview GetOverview()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var lessonIds = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT id FROM lesson_records ORDER BY created_utc, id;";
            using var reader = command.ExecuteReader();
            while (reader.Read()) lessonIds.Add(reader.GetString(0));
        }
        var snapshots = lessonIds.Select(id => ReadSnapshot(connection, transaction, id)).Where(value => value is not null)
            .Cast<LessonSnapshot>().ToDictionary(value => value.Id, StringComparer.Ordinal);
        var reviews = new List<LessonReviewRecord>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                SELECT r.id, r.lesson_id, r.lesson_fingerprint, r.reviewer_identity, r.decision,
                       r.runtime_environment, r.derivative_evidence, r.unresolved_failures, r.reviewed_utc,
                       (SELECT COUNT(*) FROM lesson_review_criteria c WHERE c.review_id=r.id)
                FROM lesson_reviews r ORDER BY r.sequence, r.id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var lessonId = reader.GetString(1); var fingerprint = reader.GetString(2);
                reviews.Add(new LessonReviewRecord(reader.GetString(0), lessonId, fingerprint, reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetInt32(9),
                    snapshots.TryGetValue(lessonId, out var snapshot) && snapshot.Fingerprint == fingerprint));
            }
        }
        var lessons = snapshots.Values.OrderBy(value => value.CreatedUtc, StringComparer.Ordinal).ThenBy(value => value.Id, StringComparer.Ordinal)
            .Select(snapshot =>
            {
                var lessonReviews = reviews.Where(value => value.LessonId == snapshot.Id).ToArray();
                var latest = lessonReviews.LastOrDefault();
                return new LessonReviewLessonRecord(snapshot.Id, snapshot.Title, snapshot.Subject, snapshot.LearningStage, snapshot.Status,
                    snapshot.Fingerprint, latest?.Decision, latest?.FingerprintCurrent ?? false, lessonReviews.Length);
            }).ToArray();
        transaction.Commit();
        return new LessonReviewOverview(true, "install-root-sqlite", SchemaVersion, CriterionIds, lessons, reviews,
        [
            "A review binds to the exact saved lesson fingerprint and becomes stale when that fingerprint changes.",
            "Every named criterion requires an explicit outcome and evidence note.",
            "Approved-for-use changes no lesson content or status; it only permits the separately guarded practice workflow.",
            "Reviewer identity is a recorded claim, not authentication or proof of professional qualification.",
            "No model, score, percentage, learner classification or automatic publication exists in this workflow.",
        ]);
    }

    public LessonReviewMutation ReviewLesson(LessonReviewInput input)
    {
        try
        {
            var reviewId = RequireId(input.ReviewId, "review id"); var lessonId = RequireId(input.LessonId, "lesson id");
            var reviewer = RequireText(input.ReviewerIdentity, "reviewer identity", 2, 120);
            var decision = RequireChoice(input.Decision, "decision", Decisions);
            var runtime = RequireText(input.RuntimeEnvironment, "runtime environment", 5, 2000);
            var derivative = RequireText(input.DerivativeEvidence, "derivative evidence", 4, 2000);
            var unresolved = RequireText(input.UnresolvedFailures, "unresolved failures", 4, 4000);
            var criteria = NormalizeCriteria(input.Criteria);
            if (decision == "approved-for-use" && criteria.Any(value => value.Outcome == "failed"))
                throw new ArgumentException("Approved-for-use is unavailable while any criterion is failed.");
            if (decision == "approved-for-use" && !unresolved.Equals("none", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Approved-for-use requires unresolved failures to be exactly 'none'.");

            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            var snapshot = ReadSnapshot(connection, transaction, lessonId);
            if (snapshot is null) return Rollback(transaction, "invalid", reviewId, "Lesson does not exist.");
            var existingFound = false; var sameExistingReview = false;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT lesson_id, lesson_fingerprint, reviewer_identity, decision, runtime_environment,
                           derivative_evidence, unresolved_failures FROM lesson_reviews WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", reviewId); using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    existingFound = true;
                    sameExistingReview = reader.GetString(0) == lessonId && reader.GetString(1) == snapshot.Fingerprint
                        && reader.GetString(2) == reviewer && reader.GetString(3) == decision && reader.GetString(4) == runtime
                        && reader.GetString(5) == derivative && reader.GetString(6) == unresolved;
                }
            }
            if (existingFound)
            {
                var existingCriteria = ReadCriteria(connection, transaction, reviewId);
                sameExistingReview = sameExistingReview && existingCriteria.SequenceEqual(criteria);
                transaction.Rollback();
                return sameExistingReview ? new(true, "already-present", reviewId, snapshot.Fingerprint, null)
                    : new(false, "conflict", reviewId, snapshot.Fingerprint, "Review id already exists with different lesson evidence or content.");
            }
            var now = DateTimeOffset.UtcNow.ToString("O");
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO lesson_reviews(id, lesson_id, lesson_fingerprint, reviewer_identity, decision,
                        runtime_environment, derivative_evidence, unresolved_failures, reviewed_utc)
                    VALUES ($id, $lesson, $fingerprint, $reviewer, $decision, $runtime, $derivative, $unresolved, $now);
                    """;
                insert.Parameters.AddWithValue("$id", reviewId); insert.Parameters.AddWithValue("$lesson", lessonId);
                insert.Parameters.AddWithValue("$fingerprint", snapshot.Fingerprint); insert.Parameters.AddWithValue("$reviewer", reviewer);
                insert.Parameters.AddWithValue("$decision", decision); insert.Parameters.AddWithValue("$runtime", runtime);
                insert.Parameters.AddWithValue("$derivative", derivative); insert.Parameters.AddWithValue("$unresolved", unresolved);
                insert.Parameters.AddWithValue("$now", now); insert.ExecuteNonQuery();
            }
            foreach (var criterion in criteria)
            {
                using var insert = connection.CreateCommand(); insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO lesson_review_criteria(review_id, criterion_id, outcome, evidence) VALUES ($review, $criterion, $outcome, $evidence);";
                insert.Parameters.AddWithValue("$review", reviewId); insert.Parameters.AddWithValue("$criterion", criterion.CriterionId);
                insert.Parameters.AddWithValue("$outcome", criterion.Outcome); insert.Parameters.AddWithValue("$evidence", criterion.Evidence);
                insert.ExecuteNonQuery();
            }
            InsertEvent(connection, transaction, lessonId, reviewId, decision, now, reviewer); transaction.Commit();
            return new(true, "review-recorded", reviewId, snapshot.Fingerprint, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, null, exception.Message); }
    }

    public bool IsCurrentApproved(string lessonId)
    {
        try
        {
            var id = RequireId(lessonId, "lesson id"); using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            var snapshot = ReadSnapshot(connection, transaction, id); if (snapshot is null) { transaction.Rollback(); return false; }
            bool approved;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT decision, lesson_fingerprint FROM lesson_reviews WHERE lesson_id=$id ORDER BY sequence DESC LIMIT 1;";
                command.Parameters.AddWithValue("$id", id);
                using var reader = command.ExecuteReader();
                approved = reader.Read() && reader.GetString(0) == "approved-for-use" && reader.GetString(1) == snapshot.Fingerprint;
            }
            transaction.Commit(); return approved;
        }
        catch (ArgumentException) { return false; }
    }

    public ApprovedLessonEvidence? GetCurrentApprovedLesson(string lessonId)
    {
        try
        {
            var id = RequireId(lessonId, "lesson id"); using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            var result = ReadCurrentApprovedLesson(connection, transaction, id);
            if (!result.LessonExists) { transaction.Rollback(); return null; }
            transaction.Commit(); return result.Evidence;
        }
        catch (ArgumentException) { return null; }
    }

    internal ApprovedLessonEvidence? GetCurrentApprovedLesson(SqliteConnection connection, SqliteTransaction transaction, string lessonId)
    {
        var id = RequireId(lessonId, "lesson id");
        return ReadCurrentApprovedLesson(connection, transaction, id).Evidence;
    }

    private (bool LessonExists, ApprovedLessonEvidence? Evidence) ReadCurrentApprovedLesson(SqliteConnection connection, SqliteTransaction transaction, string id)
    {
            var snapshot = ReadSnapshot(connection, transaction, id); if (snapshot is null) return (false, null);
            string? reviewId = null; string? decision = null; string? fingerprint = null;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT id, decision, lesson_fingerprint FROM lesson_reviews WHERE lesson_id=$id ORDER BY sequence DESC LIMIT 1;";
                command.Parameters.AddWithValue("$id", id); using var reader = command.ExecuteReader();
                if (reader.Read()) { reviewId = reader.GetString(0); decision = reader.GetString(1); fingerprint = reader.GetString(2); }
            }
            var evidence = decision == "approved-for-use" && fingerprint == snapshot.Fingerprint && reviewId is not null
                ? new ApprovedLessonEvidence(snapshot.Id, snapshot.Title, snapshot.Subject, snapshot.LearningStage, snapshot.Fingerprint, reviewId)
                : null;
            return (true, evidence);
    }

    private void Initialize()
    {
        using var connection = OpenConnection(); using var command = connection.CreateCommand(); command.CommandText = """
            CREATE TABLE IF NOT EXISTS lesson_review_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS lesson_reviews (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE,
                lesson_id TEXT NOT NULL REFERENCES lesson_records(id), lesson_fingerprint TEXT NOT NULL,
                reviewer_identity TEXT NOT NULL, decision TEXT NOT NULL, runtime_environment TEXT NOT NULL,
                derivative_evidence TEXT NOT NULL, unresolved_failures TEXT NOT NULL, reviewed_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS lesson_review_criteria (
                review_id TEXT NOT NULL REFERENCES lesson_reviews(id) ON DELETE CASCADE,
                criterion_id TEXT NOT NULL, outcome TEXT NOT NULL, evidence TEXT NOT NULL,
                PRIMARY KEY(review_id, criterion_id)
            );
            CREATE TABLE IF NOT EXISTS lesson_review_events (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT, occurred_utc TEXT NOT NULL,
                lesson_id TEXT NOT NULL, review_id TEXT NOT NULL, action TEXT NOT NULL, actor TEXT NOT NULL
            );
            INSERT INTO lesson_review_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            CREATE INDEX IF NOT EXISTS idx_lesson_reviews_lesson ON lesson_reviews(lesson_id, sequence);
            """; command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString()); command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }

    private static LessonSnapshot? ReadSnapshot(SqliteConnection connection, SqliteTransaction transaction, string lessonId)
    {
        string id; string plan; string title; string objective; string teachingContent; string evidenceState; string status; string created; string updated; string subject; string stage;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction; command.CommandText = """
                SELECT l.id, l.study_plan_id, l.title, l.learning_objective, l.teaching_content, l.evidence_state,
                       l.status, l.created_utc, l.updated_utc, p.subject, p.learning_stage
                FROM lesson_records l JOIN study_plans p ON p.id=l.study_plan_id WHERE l.id=$id;
                """; command.Parameters.AddWithValue("$id", lessonId); using var reader = command.ExecuteReader(); if (!reader.Read()) return null;
            id = reader.GetString(0); plan = reader.GetString(1); title = reader.GetString(2); objective = reader.GetString(3);
            teachingContent = reader.GetString(4); evidenceState = reader.GetString(5); status = reader.GetString(6);
            created = reader.GetString(7); updated = reader.GetString(8); subject = reader.GetString(9); stage = reader.GetString(10);
        }
        var canonical = new StringBuilder();
        AppendPart(canonical, id); AppendPart(canonical, plan); AppendPart(canonical, title); AppendPart(canonical, objective);
        AppendPart(canonical, teachingContent); AppendPart(canonical, evidenceState); AppendPart(canonical, status);
        AppendPart(canonical, created); AppendPart(canonical, updated); AppendPart(canonical, subject); AppendPart(canonical, stage);
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction; command.CommandText = "SELECT sequence, section_kind, content FROM lesson_sections WHERE lesson_id=$id ORDER BY sequence;";
            command.Parameters.AddWithValue("$id", id); using var reader = command.ExecuteReader(); while (reader.Read())
            { AppendPart(canonical, reader.GetInt32(0).ToString(CultureInfo.InvariantCulture)); AppendPart(canonical, reader.GetString(1)); AppendPart(canonical, reader.GetString(2)); }
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction; command.CommandText = """
                SELECT c.id, c.source_revision_id, c.statement_sha256, c.source_locator, c.review_state, e.evidence_role
                FROM lesson_evidence e JOIN curriculum_statements c ON c.id=e.curriculum_statement_id
                WHERE e.lesson_id=$id ORDER BY c.id, e.evidence_role;
                """; command.Parameters.AddWithValue("$id", id); using var reader = command.ExecuteReader(); while (reader.Read())
            { AppendPart(canonical, reader.GetString(0)); AppendPart(canonical, reader.GetInt64(1).ToString(CultureInfo.InvariantCulture)); AppendPart(canonical, reader.GetString(2)); AppendPart(canonical, reader.GetString(3)); AppendPart(canonical, reader.GetString(4)); AppendPart(canonical, reader.GetString(5)); }
        }
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
        return new LessonSnapshot(id, title, subject, stage, status, created, fingerprint);
    }

    private static void AppendPart(StringBuilder target, string value) => target.Append(value.Length).Append(':').Append(value).Append(';');
    private static IReadOnlyList<LessonCriterionResult> NormalizeCriteria(IReadOnlyList<LessonCriterionInput>? input)
    {
        if (input is null || input.Count != CriterionIds.Length) throw new ArgumentException($"Exactly {CriterionIds.Length} criterion results are required.");
        var values = new Dictionary<string, LessonCriterionResult>(StringComparer.Ordinal);
        foreach (var item in input)
        {
            var id = (item.CriterionId ?? "").Trim().ToLowerInvariant(); if (!CriterionIdSet.Contains(id)) throw new ArgumentException($"Unknown criterion {id}.");
            var outcome = RequireChoice(item.Outcome, $"outcome for {id}", CriterionOutcomes); var evidence = RequireText(item.Evidence, $"evidence for {id}", 5, 4000);
            if (!values.TryAdd(id, new(id, outcome, evidence))) throw new ArgumentException($"Criterion {id} appears more than once.");
        }
        if (values.Count != CriterionIds.Length) throw new ArgumentException("Every lesson review criterion is required.");
        return CriterionIds.Select(id => values[id]).ToArray();
    }
    private static IReadOnlyList<LessonCriterionResult> ReadCriteria(SqliteConnection connection, SqliteTransaction transaction, string reviewId)
    {
        var values = new Dictionary<string, LessonCriterionResult>(StringComparer.Ordinal); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT criterion_id, outcome, evidence FROM lesson_review_criteria WHERE review_id=$id;"; command.Parameters.AddWithValue("$id", reviewId);
        using var reader = command.ExecuteReader(); while (reader.Read()) values[reader.GetString(0)] = new(reader.GetString(0), reader.GetString(1), reader.GetString(2));
        return CriterionIds.Where(values.ContainsKey).Select(id => values[id]).ToArray();
    }
    private static string RequireId(string? value, string field) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-64 lowercase letters, numbers, hyphens or underscores."); return normalized; }
    private static string RequireText(string? value, string field, int minimum, int maximum) { var normalized = (value ?? "").Trim(); if (normalized.Length < minimum || normalized.Length > maximum) throw new ArgumentException($"{field} must be {minimum}-{maximum} characters."); return normalized; }
    private static string RequireChoice(string? value, string field, HashSet<string> choices) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!choices.Contains(normalized)) throw new ArgumentException($"{field} is not supported."); return normalized; }
    private static LessonReviewMutation Rollback(SqliteTransaction transaction, string state, string id, string error) { transaction.Rollback(); return new(false, state, id, null, error); }
    private static void InsertEvent(SqliteConnection connection, SqliteTransaction transaction, string lessonId, string reviewId, string action, string now, string actor)
    { using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "INSERT INTO lesson_review_events(occurred_utc, lesson_id, review_id, action, actor) VALUES ($now, $lesson, $review, $action, $actor);"; command.Parameters.AddWithValue("$now", now); command.Parameters.AddWithValue("$lesson", lessonId); command.Parameters.AddWithValue("$review", reviewId); command.Parameters.AddWithValue("$action", action); command.Parameters.AddWithValue("$actor", actor); command.ExecuteNonQuery(); }
}

internal sealed record LessonReviewOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, IReadOnlyList<string> CriterionIds,
    IReadOnlyList<LessonReviewLessonRecord> Lessons, IReadOnlyList<LessonReviewRecord> Reviews, IReadOnlyList<string> Boundaries);
internal sealed record LessonReviewLessonRecord(string Id, string Title, string Subject, string LearningStage, string Status,
    string CurrentFingerprint, string? LatestDecision, bool LatestReviewCurrent, int ReviewCount);
internal sealed record LessonReviewRecord(string Id, string LessonId, string LessonFingerprint, string ReviewerIdentity, string Decision,
    string RuntimeEnvironment, string DerivativeEvidence, string UnresolvedFailures, string ReviewedUtc, int CriterionCount, bool FingerprintCurrent);
internal sealed record LessonReviewInput(string ReviewId, string LessonId, string ReviewerIdentity, string Decision,
    string RuntimeEnvironment, string DerivativeEvidence, string UnresolvedFailures, IReadOnlyList<LessonCriterionInput>? Criteria);
internal sealed record LessonCriterionInput(string CriterionId, string Outcome, string Evidence);
internal sealed record LessonCriterionResult(string CriterionId, string Outcome, string Evidence);
internal sealed record LessonReviewMutation(bool Ok, string State, string? Id, string? LessonFingerprint, string? Error);
internal sealed record LessonSnapshot(string Id, string Title, string Subject, string LearningStage, string Status, string CreatedUtc, string Fingerprint);
internal sealed record ApprovedLessonEvidence(string Id, string Title, string Subject, string LearningStage, string Fingerprint, string ReviewId);
