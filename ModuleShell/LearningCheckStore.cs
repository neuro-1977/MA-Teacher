using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class LearningCheckStore
{
    private const int SchemaVersion = 3;
    private const int MaximumAttachmentBytes = 10 * 1024 * 1024;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> Outcomes = new(StringComparer.Ordinal) { "met", "partially-met", "not-yet", "invalid" };
    private readonly string _connectionString;
    private readonly LessonReviewStore _lessonReviews;

    public LearningCheckStore(string moduleRoot, LessonReviewStore lessonReviews)
    {
        _lessonReviews = lessonReviews;
        var dataRoot = Path.Combine(moduleRoot, "data"); Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public LearningCheckOverview GetOverview()
    {
        var currentFingerprints = _lessonReviews.GetOverview().Lessons.ToDictionary(value => value.Id, value => value.CurrentFingerprint, StringComparer.Ordinal);
        using var connection = OpenConnection();
        var checks = new List<LearningCheckRecord>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT c.id, c.lesson_id, l.title, p.learner_id, learner.display_name, c.prompt, c.response_mode,
                       c.success_criteria, c.evidence_state, c.status, c.created_utc, c.lesson_fingerprint,
                       (SELECT COUNT(*) FROM learning_check_evidence e WHERE e.check_id=c.id)
                FROM learning_checks c
                JOIN lesson_records l ON l.id=c.lesson_id
                JOIN study_plans p ON p.id=l.study_plan_id
                JOIN learner_profiles learner ON learner.id=p.learner_id
                ORDER BY c.created_utc, c.id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var lessonId = reader.GetString(1); var storedFingerprint = reader.IsDBNull(11) ? null : reader.GetString(11);
                var current = storedFingerprint is not null && currentFingerprints.TryGetValue(lessonId, out var observed) && observed == storedFingerprint;
                var currency = storedFingerprint is null ? "legacy-currency-unknown" : current ? "current-fingerprint" : "stale-fingerprint";
                checks.Add(new(reader.GetString(0), lessonId, reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9),
                    reader.GetString(10), storedFingerprint, current, currency, reader.GetInt32(12)));
            }
        }
        var attempts = new List<LearningCheckAttemptRecord>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT a.id, a.check_id, c.lesson_id, a.learner_id, learner.display_name, a.response_text,
                       a.submitted_utc, a.review_state, a.outcome, a.feedback, a.reviewed_utc,
                       attachment.original_filename, attachment.media_type, attachment.byte_length, attachment.sha256
                FROM learning_check_attempts a
                JOIN learning_checks c ON c.id=a.check_id
                JOIN learner_profiles learner ON learner.id=a.learner_id
                LEFT JOIN learning_check_attempt_attachments attachment ON attachment.attempt_id=a.id
                ORDER BY a.submitted_utc, a.id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read()) attempts.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12), reader.IsDBNull(13) ? null : reader.GetInt64(13),
                reader.IsDBNull(14) ? null : reader.GetString(14)));
        }
        return new LearningCheckOverview(true, "install-root-sqlite", SchemaVersion, checks, attempts,
            new[]
            {
                "Checks and criteria are operator-authored and remain unverified teaching content.",
                "New checks bind to the exact approved lesson fingerprint; legacy rows remain currency-unknown and changed lessons make prior checks stale.",
                "Only a current-fingerprint check can accept a new learner attempt.",
                "Typed responses and one optional bounded work attachment are stored inside the canonical SQLite database; no loose upload folder is created.",
                "Learner responses and attachments are never automatically scored.",
                "A human review records a bounded outcome and feedback; it does not prove broad mastery.",
                "Free-text responses may contain personal information and remain local to the install root.",
                "No model, remote service or browser agent participates in this workflow.",
            });
    }

    public LearningCheckMutation CreateCheck(LearningCheckInput input)
    {
        try
        {
            var id = RequireId(input.Id, "check id"); var lessonId = RequireId(input.LessonId, "lesson id");
            var prompt = RequireText(input.Prompt, "prompt", 5, 2000); var criteria = RequireText(input.SuccessCriteria, "success criteria", 5, 3000);
            var evidenceIds = (input.CurriculumCandidateIds ?? Array.Empty<string>()).Select(value => RequireId(value, "candidate id"))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (evidenceIds.Length is < 1 or > 10) throw new ArgumentException("A learning check requires 1-10 lesson-linked curriculum candidates.");
            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction(); string learnerId;
            var approvedLesson = _lessonReviews.GetCurrentApprovedLesson(connection, transaction, lessonId);
            if (approvedLesson is null) return Rollback(transaction, "invalid", id, "Lesson requires a current approved-for-use review before practice can be authored.");
            object? learnerValue;
            using (var lesson = connection.CreateCommand())
            {
                lesson.Transaction = transaction; lesson.CommandText = "SELECT p.learner_id FROM lesson_records l JOIN study_plans p ON p.id=l.study_plan_id WHERE l.id=$lessonId;";
                lesson.Parameters.AddWithValue("$lessonId", lessonId); learnerValue = lesson.ExecuteScalar();
            }
            if (learnerValue is not string found) return Rollback(transaction, "invalid", id, "Lesson does not exist."); learnerId = found;
            foreach (var candidateId in evidenceIds)
            {
                long acceptedCount;
                using (var evidence = connection.CreateCommand())
                {
                    evidence.Transaction = transaction;
                    evidence.CommandText = """
                        SELECT COUNT(*) FROM lesson_evidence le JOIN curriculum_statements c ON c.id=le.curriculum_statement_id
                        WHERE le.lesson_id=$lessonId AND le.curriculum_statement_id=$candidateId AND c.review_state='accepted';
                        """;
                    evidence.Parameters.AddWithValue("$lessonId", lessonId); evidence.Parameters.AddWithValue("$candidateId", candidateId);
                    acceptedCount = Convert.ToInt64(evidence.ExecuteScalar());
                }
                if (acceptedCount != 1) return Rollback(transaction, "invalid", id, $"Candidate {candidateId} is not accepted evidence for this lesson.");
            }
            var exists = false; var sameContent = false;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction; existing.CommandText = "SELECT lesson_id, prompt, success_criteria, response_mode, status, lesson_fingerprint FROM learning_checks WHERE id=$id;";
                existing.Parameters.AddWithValue("$id", id); using var reader = existing.ExecuteReader();
                if (reader.Read()) { exists = true; sameContent = reader.GetString(0) == lessonId && reader.GetString(1) == prompt
                    && reader.GetString(2) == criteria && reader.GetString(3) == "free-text" && reader.GetString(4) == "operator-authorized-practice"
                    && !reader.IsDBNull(5) && reader.GetString(5) == approvedLesson.Fingerprint; }
            }
            if (exists)
            {
                var linked = new List<string>();
                using (var links = connection.CreateCommand())
                {
                    links.Transaction = transaction;
                    links.CommandText = "SELECT curriculum_statement_id FROM learning_check_evidence WHERE check_id=$id ORDER BY curriculum_statement_id;";
                    links.Parameters.AddWithValue("$id", id); using var reader = links.ExecuteReader(); while (reader.Read()) linked.Add(reader.GetString(0));
                }
                var same = sameContent && linked.SequenceEqual(evidenceIds, StringComparer.Ordinal); transaction.Rollback();
                return same ? new(true, "already-present", id, null) : new(false, "conflict", id, "Check id already exists with different content or evidence.");
            }
            var now = DateTimeOffset.UtcNow.ToString("O");
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction; insert.CommandText = """
                    INSERT INTO learning_checks(id, lesson_id, lesson_fingerprint, prompt, response_mode, success_criteria, evidence_state, status, created_utc)
                    VALUES ($id, $lessonId, $fingerprint, $prompt, 'free-text', $criteria, 'operator-authored-criteria-unverified', 'operator-authorized-practice', $now);
                    """;
                insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$lessonId", lessonId); insert.Parameters.AddWithValue("$prompt", prompt);
                insert.Parameters.AddWithValue("$fingerprint", approvedLesson.Fingerprint);
                insert.Parameters.AddWithValue("$criteria", criteria); insert.Parameters.AddWithValue("$now", now); insert.ExecuteNonQuery();
            }
            foreach (var candidateId in evidenceIds)
            {
                using var link = connection.CreateCommand(); link.Transaction = transaction;
                link.CommandText = "INSERT INTO learning_check_evidence(check_id, curriculum_statement_id) VALUES ($checkId, $candidateId);";
                link.Parameters.AddWithValue("$checkId", id); link.Parameters.AddWithValue("$candidateId", candidateId); link.ExecuteNonQuery();
            }
            InsertEvent(connection, transaction, "learning-check", id, "created", now, learnerId); transaction.Commit();
            return new(true, "created-operator-authored-unverified", id, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    public LearningCheckMutation SubmitAttempt(LearningCheckAttemptInput input)
    {
        try
        {
            var id = RequireId(input.Id, "attempt id"); var checkId = RequireId(input.CheckId, "check id");
            var learnerId = RequireId(input.LearnerId, "learner id");
            var response = (input.ResponseText ?? string.Empty).Trim();
            if (response.Length > 10000) throw new ArgumentException("response must be no more than 10000 characters.");
            var attachment = ParseAttachment(input.AttachmentName, input.AttachmentMediaType, input.AttachmentBase64);
            if (response.Length == 0 && attachment is null) throw new ArgumentException("A typed response or work attachment is required.");
            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            string? expectedLearnerId = null; string? authorityLessonId = null; string? fingerprint = null;
            using (var authority = connection.CreateCommand())
            {
                authority.Transaction = transaction; authority.CommandText = """
                    SELECT p.learner_id, c.lesson_id, c.lesson_fingerprint FROM learning_checks c JOIN lesson_records l ON l.id=c.lesson_id
                    JOIN study_plans p ON p.id=l.study_plan_id WHERE c.id=$checkId AND c.status='operator-authorized-practice';
                    """; authority.Parameters.AddWithValue("$checkId", checkId); using var reader = authority.ExecuteReader();
                if (reader.Read())
                {
                    expectedLearnerId = reader.GetString(0); authorityLessonId = reader.GetString(1);
                    fingerprint = reader.IsDBNull(2) ? null : reader.GetString(2);
                }
            }
            if (expectedLearnerId is null || authorityLessonId is null) return Rollback(transaction, "invalid", id, "Learning check is unavailable.");
            if (expectedLearnerId != learnerId) return Rollback(transaction, "invalid", id, "Attempt learner does not own this lesson plan.");
            var approved = _lessonReviews.GetCurrentApprovedLesson(connection, transaction, authorityLessonId);
            if (fingerprint is null || approved is null || approved.Fingerprint != fingerprint)
                return Rollback(transaction, "invalid", id, "Learning check is legacy or stale; author a new check against the current approved lesson before accepting another attempt.");
            LearningCheckMutation? duplicateResult = null;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction; existing.CommandText = """
                    SELECT attempt.check_id, attempt.learner_id, attempt.response_text,
                           attachment.original_filename, attachment.media_type, attachment.byte_length, attachment.sha256
                    FROM learning_check_attempts attempt
                    LEFT JOIN learning_check_attempt_attachments attachment ON attachment.attempt_id=attempt.id
                    WHERE attempt.id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id); using var reader = existing.ExecuteReader(); if (reader.Read())
                {
                    var same = reader.GetString(0) == checkId && reader.GetString(1) == learnerId && reader.GetString(2) == response;
                    var storedHasAttachment = !reader.IsDBNull(3);
                    same = same && (storedHasAttachment
                        ? attachment is not null && reader.GetString(3) == attachment.FileName && reader.GetString(4) == attachment.MediaType
                            && reader.GetInt64(5) == attachment.Body.LongLength && reader.GetString(6) == attachment.Sha256
                        : attachment is null);
                    duplicateResult = same ? new(true, "already-present", id, null) : new(false, "conflict", id, "Attempt id already exists with different content.");
                }
            }
            if (duplicateResult is not null) { transaction.Rollback(); return duplicateResult; }
            var now = DateTimeOffset.UtcNow.ToString("O"); using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction; insert.CommandText = """
                    INSERT INTO learning_check_attempts(id, check_id, learner_id, response_text, submitted_utc, review_state)
                    VALUES ($id, $checkId, $learnerId, $response, $now, 'unreviewed');
                    """; insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$checkId", checkId);
                insert.Parameters.AddWithValue("$learnerId", learnerId); insert.Parameters.AddWithValue("$response", response);
                insert.Parameters.AddWithValue("$now", now); insert.ExecuteNonQuery();
            }
            if (attachment is not null)
            {
                using var insertAttachment = connection.CreateCommand(); insertAttachment.Transaction = transaction;
                insertAttachment.CommandText = """
                    INSERT INTO learning_check_attempt_attachments(attempt_id, original_filename, media_type, byte_length, sha256, content_blob)
                    VALUES ($attemptId, $fileName, $mediaType, $bytes, $sha256, $body);
                    """;
                insertAttachment.Parameters.AddWithValue("$attemptId", id); insertAttachment.Parameters.AddWithValue("$fileName", attachment.FileName);
                insertAttachment.Parameters.AddWithValue("$mediaType", attachment.MediaType); insertAttachment.Parameters.AddWithValue("$bytes", attachment.Body.LongLength);
                insertAttachment.Parameters.AddWithValue("$sha256", attachment.Sha256); insertAttachment.Parameters.Add("$body", SqliteType.Blob).Value = attachment.Body;
                insertAttachment.ExecuteNonQuery();
            }
            InsertEvent(connection, transaction, "learning-check-attempt", id, attachment is null ? "submitted-unreviewed" : "submitted-unreviewed:attachment", now, learnerId); transaction.Commit();
            return new(true, "submitted-unreviewed", id, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    public LearningCheckAttemptAttachment? GetAttemptAttachment(string attemptId)
    {
        try
        {
            var id = RequireId(attemptId, "attempt id"); using var connection = OpenConnection(); using var command = connection.CreateCommand();
            command.CommandText = "SELECT original_filename, media_type, byte_length, sha256, content_blob FROM learning_check_attempt_attachments WHERE attempt_id=$id;";
            command.Parameters.AddWithValue("$id", id); using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            var body = (byte[])reader[4];
            if (body.LongLength != reader.GetInt64(2) || body.LongLength is < 1 or > MaximumAttachmentBytes) return null;
            var sha256 = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
            if (!string.Equals(sha256, reader.GetString(3), StringComparison.Ordinal)) return null;
            return new(reader.GetString(0), reader.GetString(1), body.LongLength, sha256, body);
        }
        catch (ArgumentException) { return null; }
    }

    public LearningCheckMutation ReviewAttempt(LearningCheckReviewInput input)
    {
        try
        {
            var id = RequireId(input.AttemptId, "attempt id"); var outcome = RequireText(input.Outcome, "outcome", 3, 32).ToLowerInvariant();
            if (!Outcomes.Contains(outcome)) throw new ArgumentException("Outcome must be met, partially-met, not-yet or invalid.");
            var feedback = RequireText(input.Feedback, "feedback", 1, 4000);
            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction(); string? learnerId = null;
            LearningCheckMutation? duplicateResult = null;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction; existing.CommandText = "SELECT learner_id, review_state, outcome, feedback FROM learning_check_attempts WHERE id=$id;";
                existing.Parameters.AddWithValue("$id", id); using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    learnerId = reader.GetString(0);
                    if (reader.GetString(1) == "human-reviewed")
                    {
                        var same = !reader.IsDBNull(2) && reader.GetString(2) == outcome && !reader.IsDBNull(3) && reader.GetString(3) == feedback;
                        duplicateResult = same ? new(true, "already-present", id, null) : new(false, "conflict", id, "Attempt already has a different human review.");
                    }
                }
            }
            if (learnerId is null) return Rollback(transaction, "invalid", id, "Attempt does not exist.");
            if (duplicateResult is not null) { transaction.Rollback(); return duplicateResult; }
            int updated;
            var now = DateTimeOffset.UtcNow.ToString("O"); using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction; update.CommandText = """
                    UPDATE learning_check_attempts SET review_state='human-reviewed', outcome=$outcome, feedback=$feedback, reviewed_utc=$now
                    WHERE id=$id AND review_state='unreviewed';
                    """; update.Parameters.AddWithValue("$outcome", outcome); update.Parameters.AddWithValue("$feedback", feedback);
                update.Parameters.AddWithValue("$now", now); update.Parameters.AddWithValue("$id", id);
                updated = update.ExecuteNonQuery();
            }
            if (updated != 1) return Rollback(transaction, "conflict", id, "Attempt review state changed concurrently.");
            InsertEvent(connection, transaction, "learning-check-attempt", id, $"human-reviewed:{outcome}", now, learnerId); transaction.Commit();
            return new(true, "human-reviewed", id, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    private void Initialize()
    {
        using var connection = OpenConnection(); using var command = connection.CreateCommand(); command.CommandText = """
            CREATE TABLE IF NOT EXISTS learning_check_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS learning_checks (
                id TEXT PRIMARY KEY, lesson_id TEXT NOT NULL REFERENCES lesson_records(id), lesson_fingerprint TEXT NULL, prompt TEXT NOT NULL,
                response_mode TEXT NOT NULL, success_criteria TEXT NOT NULL, evidence_state TEXT NOT NULL,
                status TEXT NOT NULL, created_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS learning_check_evidence (
                check_id TEXT NOT NULL REFERENCES learning_checks(id) ON DELETE CASCADE,
                curriculum_statement_id TEXT NOT NULL REFERENCES curriculum_statements(id), PRIMARY KEY(check_id, curriculum_statement_id)
            );
            CREATE TABLE IF NOT EXISTS learning_check_attempts (
                id TEXT PRIMARY KEY, check_id TEXT NOT NULL REFERENCES learning_checks(id), learner_id TEXT NOT NULL REFERENCES learner_profiles(id),
                response_text TEXT NOT NULL, submitted_utc TEXT NOT NULL, review_state TEXT NOT NULL,
                outcome TEXT NULL, feedback TEXT NULL, reviewed_utc TEXT NULL
            );
            CREATE TABLE IF NOT EXISTS learning_check_attempt_attachments (
                attempt_id TEXT PRIMARY KEY REFERENCES learning_check_attempts(id) ON DELETE CASCADE,
                original_filename TEXT NOT NULL, media_type TEXT NOT NULL, byte_length INTEGER NOT NULL,
                sha256 TEXT NOT NULL, content_blob BLOB NOT NULL
            );
            CREATE TABLE IF NOT EXISTS learning_check_events (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT, occurred_utc TEXT NOT NULL, entity_kind TEXT NOT NULL,
                entity_id TEXT NOT NULL, action TEXT NOT NULL, learner_id TEXT NOT NULL
            );
            INSERT INTO learning_check_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            CREATE INDEX IF NOT EXISTS idx_learning_checks_lesson ON learning_checks(lesson_id);
            CREATE INDEX IF NOT EXISTS idx_learning_attempts_check ON learning_check_attempts(check_id, submitted_utc);
            """; command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString()); command.ExecuteNonQuery();
        if (!ColumnExists(connection, "learning_checks", "lesson_fingerprint"))
        { using var alter = connection.CreateCommand(); alter.CommandText = "ALTER TABLE learning_checks ADD COLUMN lesson_fingerprint TEXT NULL;"; alter.ExecuteNonQuery(); }
    }

    private SqliteConnection OpenConnection()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }
    private static string RequireId(string? value, string field) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-64 lowercase letters, numbers, hyphens or underscores."); return normalized; }
    private static bool ColumnExists(SqliteConnection connection, string table, string column)
    { using var command = connection.CreateCommand(); command.CommandText = $"PRAGMA table_info({table});"; using var reader = command.ExecuteReader(); while (reader.Read()) if (reader.GetString(1) == column) return true; return false; }
    private static string RequireText(string? value, string field, int min, int max) { var normalized = (value ?? "").Trim(); if (normalized.Length < min || normalized.Length > max) throw new ArgumentException($"{field} must be {min}-{max} characters."); return normalized; }
    private static AttemptAttachmentDraft? ParseAttachment(string? suppliedName, string? suppliedMediaType, string? suppliedBase64)
    {
        var any = !string.IsNullOrWhiteSpace(suppliedName) || !string.IsNullOrWhiteSpace(suppliedMediaType) || !string.IsNullOrWhiteSpace(suppliedBase64);
        if (!any) return null;
        if (string.IsNullOrWhiteSpace(suppliedName) || string.IsNullOrWhiteSpace(suppliedMediaType) || string.IsNullOrWhiteSpace(suppliedBase64)) throw new ArgumentException("Attachment name, media type and content are required together.");
        var fileName = suppliedName.Trim();
        if (fileName.Length is < 1 or > 180 || fileName.IndexOfAny(['/', '\\', '\r', '\n']) >= 0) throw new ArgumentException("Attachment filename must be 1-180 characters without path or control separators.");
        var extension = Path.GetExtension(fileName).ToLowerInvariant(); var mediaType = suppliedMediaType.Split(';', 2)[0].Trim().ToLowerInvariant();
        var allowed = extension switch
        {
            ".pdf" => mediaType == "application/pdf", ".txt" => mediaType == "text/plain", ".rtf" => mediaType is "application/rtf" or "text/rtf",
            ".doc" => mediaType == "application/msword", ".docx" => mediaType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".odt" => mediaType == "application/vnd.oasis.opendocument.text", ".png" => mediaType == "image/png",
            ".jpg" or ".jpeg" => mediaType == "image/jpeg", ".webp" => mediaType == "image/webp", _ => false,
        };
        if (!allowed) throw new ArgumentException("Attachment must be a matching PDF, TXT, RTF, DOC, DOCX, ODT, PNG, JPEG or WEBP file.");
        byte[] body; try { body = Convert.FromBase64String(suppliedBase64); } catch (FormatException) { throw new ArgumentException("Attachment content is not valid base64."); }
        if (body.LongLength is < 1 or > MaximumAttachmentBytes) throw new ArgumentException("Attachment must contain 1 byte to 10 MB.");
        return new(fileName, mediaType, Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant(), body);
    }
    private static LearningCheckMutation Rollback(SqliteTransaction transaction, string state, string id, string error) { transaction.Rollback(); return new(false, state, id, error); }
    private static void InsertEvent(SqliteConnection connection, SqliteTransaction transaction, string kind, string id, string action, string now, string learnerId)
    { using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "INSERT INTO learning_check_events(occurred_utc, entity_kind, entity_id, action, learner_id) VALUES ($now, $kind, $id, $action, $learnerId);"; command.Parameters.AddWithValue("$now", now); command.Parameters.AddWithValue("$kind", kind); command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$action", action); command.Parameters.AddWithValue("$learnerId", learnerId); command.ExecuteNonQuery(); }
}

internal sealed record LearningCheckOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, IReadOnlyList<LearningCheckRecord> Checks, IReadOnlyList<LearningCheckAttemptRecord> Attempts, IReadOnlyList<string> Boundaries);
internal sealed record LearningCheckRecord(string Id, string LessonId, string LessonTitle, string LearnerId, string LearnerDisplayName, string Prompt, string ResponseMode, string SuccessCriteria, string EvidenceState, string Status, string CreatedUtc, string? LessonFingerprint, bool FingerprintCurrent, string CurrencyState, int EvidenceCount);
internal sealed record LearningCheckAttemptRecord(string Id, string CheckId, string LessonId, string LearnerId, string LearnerDisplayName, string ResponseText, string SubmittedUtc, string ReviewState, string? Outcome, string? Feedback, string? ReviewedUtc, string? AttachmentName, string? AttachmentMediaType, long? AttachmentBytes, string? AttachmentSha256);
internal sealed record LearningCheckInput(string Id, string LessonId, string Prompt, string SuccessCriteria, IReadOnlyList<string> CurriculumCandidateIds);
internal sealed record LearningCheckAttemptInput(string Id, string CheckId, string LearnerId, string ResponseText, string? AttachmentName, string? AttachmentMediaType, string? AttachmentBase64);
internal sealed record LearningCheckReviewInput(string AttemptId, string Outcome, string Feedback);
internal sealed record LearningCheckMutation(bool Ok, string State, string? Id, string? Error);
internal sealed record LearningCheckAttemptAttachment(string FileName, string MediaType, long ByteLength, string Sha256, byte[] Body);
internal sealed record AttemptAttachmentDraft(string FileName, string MediaType, string Sha256, byte[] Body);
