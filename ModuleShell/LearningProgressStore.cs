using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class LearningProgressStore
{
    private const int SchemaVersion = 1;
    private readonly string _connectionString;

    public LearningProgressStore(string moduleRoot)
    {
        var dataRoot = Path.Combine(moduleRoot, "data"); Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
    }

    public LearningProgressOverview GetOverview()
    {
        using var connection = OpenConnection();
        var summaries = new List<LearningProgressSummary>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT learner.id, learner.display_name, p.subject, p.learning_stage,
                       COUNT(a.id),
                       SUM(CASE WHEN a.review_state='unreviewed' THEN 1 ELSE 0 END),
                       SUM(CASE WHEN a.outcome='met' THEN 1 ELSE 0 END),
                       SUM(CASE WHEN a.outcome='partially-met' THEN 1 ELSE 0 END),
                       SUM(CASE WHEN a.outcome='not-yet' THEN 1 ELSE 0 END),
                       SUM(CASE WHEN a.outcome='invalid' THEN 1 ELSE 0 END),
                       MAX(a.submitted_utc)
                FROM learner_profiles learner
                JOIN study_plans p ON p.learner_id=learner.id
                LEFT JOIN lesson_records l ON l.study_plan_id=p.id
                LEFT JOIN learning_checks c ON c.lesson_id=l.id
                LEFT JOIN learning_check_attempts a ON a.check_id=c.id AND a.learner_id=learner.id
                GROUP BY learner.id, learner.display_name, p.subject, p.learning_stage
                ORDER BY learner.display_name, p.subject, p.learning_stage;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read()) summaries.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)));
        }

        var entries = new List<LearningProgressEntry>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT a.id, a.check_id, c.lesson_id, l.title, p.subject, p.learning_stage,
                       a.learner_id, learner.display_name, c.prompt, c.success_criteria, a.response_text,
                       a.submitted_utc, a.review_state, a.outcome, a.feedback, a.reviewed_utc,
                       (SELECT COUNT(*) FROM learning_check_evidence e WHERE e.check_id=c.id)
                FROM learning_check_attempts a
                JOIN learning_checks c ON c.id=a.check_id
                JOIN lesson_records l ON l.id=c.lesson_id
                JOIN study_plans p ON p.id=l.study_plan_id
                JOIN learner_profiles learner ON learner.id=a.learner_id
                ORDER BY a.submitted_utc DESC, a.id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var reviewState = reader.GetString(12); var outcome = reader.IsDBNull(13) ? null : reader.GetString(13);
                entries.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9),
                    reader.GetString(10), reader.GetString(11), reviewState, outcome,
                    reader.IsDBNull(14) ? null : reader.GetString(14), reader.IsDBNull(15) ? null : reader.GetString(15),
                    reader.GetInt32(16), DescribeEvidenceNeed(reviewState, outcome)));
            }
        }
        return new LearningProgressOverview(true, "install-root-sqlite", SchemaVersion, "evidence-ledger-no-score-no-mastery", summaries, entries,
            new[]
            {
                "Counts describe recorded attempts; they are not grades, percentages or mastery estimates.",
                "An outcome belongs to one human-reviewed attempt and must not be generalized automatically.",
                "Unreviewed responses remain evidence awaiting human judgement.",
                "The ledger does not rank learners, compare cohorts or recommend progression.",
                "The operator must inspect prompt quality, criteria, response, feedback and curriculum links together.",
            });
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection;
    }

    private static string DescribeEvidenceNeed(string reviewState, string? outcome) => reviewState == "unreviewed"
        ? "Human review is required before any outcome exists."
        : outcome switch
        {
            "met" => "This single response met the operator criteria; broader retention or mastery is unknown.",
            "partially-met" => "Inspect feedback and decide what explanation or practice is appropriate.",
            "not-yet" => "Inspect prompt, criteria, prior teaching and response before choosing a next action.",
            "invalid" => "The attempt cannot support a learning inference.",
            _ => "Outcome evidence is incomplete or unknown."
        };
}

internal sealed record LearningProgressOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, string InterpretationState,
    IReadOnlyList<LearningProgressSummary> Summaries, IReadOnlyList<LearningProgressEntry> Entries, IReadOnlyList<string> Boundaries);
internal sealed record LearningProgressSummary(string LearnerId, string LearnerDisplayName, string Subject, string LearningStage,
    int Attempts, int Unreviewed, int Met, int PartiallyMet, int NotYet, int Invalid, string? LastSubmittedUtc);
internal sealed record LearningProgressEntry(string AttemptId, string CheckId, string LessonId, string LessonTitle, string Subject,
    string LearningStage, string LearnerId, string LearnerDisplayName, string Prompt, string SuccessCriteria, string ResponseText,
    string SubmittedUtc, string ReviewState, string? Outcome, string? Feedback, string? ReviewedUtc, int EvidenceCount, string EvidenceNeed);
