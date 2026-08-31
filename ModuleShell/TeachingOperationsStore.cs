using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class TeachingOperationsStore
{
    private readonly string _connectionString;
    private readonly LessonReviewStore _lessonReviews;
    private readonly TeachingSessionStore _teachingSessions;

    public TeachingOperationsStore(string moduleRoot, LessonReviewStore lessonReviews, TeachingSessionStore teachingSessions)
    {
        _lessonReviews = lessonReviews; _teachingSessions = teachingSessions;
        var dataRoot = Path.Combine(moduleRoot, "data"); Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
    }

    public TeachingOperationsOverview GetOverview()
    {
        var lessonOverview = _lessonReviews.GetOverview(); var sessionOverview = _teachingSessions.GetOverview();
        var practice = ReadPracticeEvidence();
        var rows = lessonOverview.Lessons.Select(lesson =>
        {
            var currentApproved = lesson.LatestDecision == "approved-for-use" && lesson.LatestReviewCurrent;
            var sessionReceipts = sessionOverview.Receipts.Where(value => value.LessonId == lesson.Id).ToArray();
            var currentSessions = sessionReceipts.Count(value => value.LessonFingerprint == lesson.CurrentFingerprint);
            var lessonPractice = practice.Where(value => value.LessonId == lesson.Id).ToArray();
            var currentPractice = PracticeEvidence.Combine(lessonPractice.Where(value => value.LessonFingerprint == lesson.CurrentFingerprint));
            var historicalPractice = PracticeEvidence.Combine(lessonPractice.Where(value => value.LessonFingerprint != lesson.CurrentFingerprint));
            var allPractice = PracticeEvidence.Combine(lessonPractice); var state = DeriveState(currentApproved, currentSessions, currentPractice, historicalPractice);
            return new TeachingOperationLesson(lesson.Id, lesson.Title, lesson.Subject, lesson.LearningStage, lesson.Status,
                lesson.CurrentFingerprint, lesson.LatestDecision, lesson.LatestReviewCurrent, lesson.ReviewCount,
                sessionReceipts.Length, currentSessions, allPractice.CheckCount, currentPractice.CheckCount, historicalPractice.CheckCount,
                allPractice.AttemptCount, currentPractice.AttemptCount, historicalPractice.AttemptCount,
                currentPractice.PendingAttemptCount, currentPractice.HumanReviewedAttemptCount, state.Id, state.Label,
                state.NextAction, state.TargetSurfaceId, state.EvidenceBoundary);
        }).ToArray();
        return new(true, "install-root-sqlite-read-only", rows,
        [
            "Pipeline state is a deterministic presentation of existing records, not a workflow engine, score, recommendation or mastery model.",
            "Current approval and current delivery are bound to the exact deterministic lesson fingerprint.",
            "Schema-v2 practice checks bind to the exact approved lesson fingerprint; legacy-null and nonmatching rows remain historical evidence.",
            "A next action identifies a missing or inspectable record. It does not authorize publication, teaching, assessment or learner progression.",
            "This route performs no database write, model invocation, remote request, notification or automatic transition.",
        ]);
    }

    private IReadOnlyList<PracticeEvidence> ReadPracticeEvidence()
    {
        var values = new List<PracticeEvidence>();
        using var connection = OpenConnection(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.lesson_id, c.lesson_fingerprint, COUNT(DISTINCT c.id), COUNT(a.id),
                   SUM(CASE WHEN a.review_state='human-reviewed' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN a.id IS NOT NULL AND a.review_state<>'human-reviewed' THEN 1 ELSE 0 END)
            FROM learning_checks c LEFT JOIN learning_check_attempts a ON a.check_id=c.id
            GROUP BY c.lesson_id, c.lesson_fingerprint;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read()) values.Add(new(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3),
            reader.IsDBNull(4) ? 0 : reader.GetInt32(4), reader.IsDBNull(5) ? 0 : reader.GetInt32(5)));
        return values;
    }

    private static PipelineState DeriveState(bool currentApproved, int currentSessions, PracticeEvidence current, PracticeEvidence historical)
    {
        if (!currentApproved) return new("approval-required", "EXACT REVIEW REQUIRED", "Review the exact saved lesson before use.",
            "workspace-lesson-review-records", "No current approved-for-use review matches the saved lesson fingerprint.");
        if (currentSessions == 0) return new("delivery-unrecorded", "DELIVERY NOT RECORDED", "Record delivery only after the lesson is actually used.",
            "workspace-teaching-sessions", "Approval exists, but no teaching-session receipt matches the current lesson fingerprint.");
        if (current.CheckCount == 0) return new("practice-not-authored", "CURRENT PRACTICE NOT AUTHORED", "Create a bounded manual practice check if evidence is needed.",
            "workspace-learning-checks", historical.CheckCount == 0 ? "Current approval and delivery evidence exist; no manual practice check is recorded." : "Only legacy or stale practice exists; author a new check against the current approved fingerprint.");
        if (current.PendingAttemptCount > 0) return new("human-review-pending", "HUMAN REVIEW PENDING", "Review each pending attempt against its stated success criteria.",
            "workspace-learning-checks", "Historical practice evidence contains one or more attempts without a bounded human review.");
        if (current.AttemptCount == 0) return new("attempt-not-recorded", "CURRENT ATTEMPT NOT RECORDED", "Record an attempt only when one actually occurs.",
            "workspace-learning-checks", "Practice checks exist, but no learner-owned response is recorded.");
        return new("evidence-cycle-present", "EVIDENCE CYCLE PRESENT", "Inspect currency and decide the next evidence manually.",
            "workspace-progress", "Approval and current delivery exist with historical practice and human-review records; no mastery or progression is inferred.");
    }

    private SqliteConnection OpenConnection()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }
    private sealed record PracticeEvidence(string LessonId, string? LessonFingerprint, int CheckCount, int AttemptCount, int HumanReviewedAttemptCount, int PendingAttemptCount)
    {
        public static PracticeEvidence Combine(IEnumerable<PracticeEvidence> values)
        { var rows = values.ToArray(); return new("", null, rows.Sum(value => value.CheckCount), rows.Sum(value => value.AttemptCount), rows.Sum(value => value.HumanReviewedAttemptCount), rows.Sum(value => value.PendingAttemptCount)); }
    }
    private sealed record PipelineState(string Id, string Label, string NextAction, string TargetSurfaceId, string EvidenceBoundary);
}

internal sealed record TeachingOperationsOverview(bool Ok, string DatabaseAuthority, IReadOnlyList<TeachingOperationLesson> Lessons, IReadOnlyList<string> Boundaries);
internal sealed record TeachingOperationLesson(string Id, string Title, string Subject, string LearningStage, string Status,
    string CurrentFingerprint, string? LatestDecision, bool LatestReviewCurrent, int ReviewCount,
    int SessionReceiptCount, int CurrentSessionReceiptCount, int CheckCount, int CurrentCheckCount, int HistoricalCheckCount,
    int AttemptCount, int CurrentAttemptCount, int HistoricalAttemptCount,
    int PendingAttemptCount, int HumanReviewedAttemptCount, string PipelineState, string PipelineLabel,
    string NextAction, string TargetSurfaceId, string EvidenceBoundary);
