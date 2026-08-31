using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class AccessibilityReviewStore
{
    private const int SchemaVersion = 1;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,95}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly string[] CriterionIds =
    [
        "keyboard-operation", "focus-order-visible", "screen-reader-semantics", "contrast-noncolor",
        "zoom-reflow", "reduced-motion", "error-recovery", "reading-demand",
    ];
    private static readonly HashSet<string> CriterionSet = new(CriterionIds, StringComparer.Ordinal);
    private static readonly HashSet<string> Outcomes = new(StringComparer.Ordinal) { "met", "failed", "not-observed", "not-applicable" };
    private static readonly HashSet<string> Decisions = new(StringComparer.Ordinal) { "observed-usable", "changes-required", "deferred" };
    private readonly string _connectionString;

    public AccessibilityReviewStore(string moduleRoot)
    {
        var dataRoot = Path.GetFullPath(moduleRoot); Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public AccessibilityReviewOverview GetOverview()
    {
        var reviews = new List<AccessibilityReviewRecord>(); using var connection = OpenConnection(); using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.id, r.surface_id, r.reviewer_identity, r.decision, r.runtime_environment,
                   r.input_and_assistive_technology, r.viewport_and_zoom, r.unresolved_failures, r.reviewed_utc,
                   COUNT(c.criterion_id), SUM(CASE WHEN c.outcome='failed' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN c.outcome='not-observed' THEN 1 ELSE 0 END)
            FROM accessibility_reviews r LEFT JOIN accessibility_review_criteria c ON c.review_id=r.id
            GROUP BY r.id ORDER BY r.sequence, r.id;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read()) reviews.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
            reader.GetInt32(9), reader.IsDBNull(10) ? 0 : reader.GetInt32(10), reader.IsDBNull(11) ? 0 : reader.GetInt32(11)));
        return new(true, "install-root-sqlite", SchemaVersion, CriterionIds, reviews,
        [
            "A review records one human observation of one named surface in one stated environment; it is not universal accessibility certification.",
            "Reviewer identity and environment are recorded claims, not authentication or independent verification.",
            "Observed-usable is refused while any criterion failed, was not observed, or unresolved failures is not exactly none.",
            "Not-applicable requires an evidence note and does not erase the criterion from history.",
            "Reviews do not change project readiness, lesson approval, learner state, packaging status or another surface.",
        ]);
    }

    public AccessibilityReviewMutation Record(AccessibilityReviewInput input)
    {
        try
        {
            var id = RequireId(input.ReviewId, "review id"); var surface = RequireId(input.SurfaceId, "surface id");
            var reviewer = RequireText(input.ReviewerIdentity, "reviewer identity", 2, 120);
            var decision = RequireChoice(input.Decision, "decision", Decisions);
            var environment = RequireText(input.RuntimeEnvironment, "runtime environment", 5, 2000);
            var inputTechnology = RequireText(input.InputAndAssistiveTechnology, "input and assistive technology", 4, 2000);
            var viewport = RequireText(input.ViewportAndZoom, "viewport and zoom", 4, 1000);
            var unresolved = RequireText(input.UnresolvedFailures, "unresolved failures", 4, 4000);
            var criteria = NormalizeCriteria(input.Criteria);
            if (decision == "observed-usable" && criteria.Any(value => value.Outcome is "failed" or "not-observed"))
                throw new ArgumentException("Observed-usable requires every criterion to be met or explicitly not-applicable.");
            if (decision == "observed-usable" && !unresolved.Equals("none", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Observed-usable requires unresolved failures to be exactly 'none'.");

            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT surface_id, reviewer_identity, decision, runtime_environment,
                           input_and_assistive_technology, viewport_and_zoom, unresolved_failures
                    FROM accessibility_reviews WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id); var found = false; var same = false;
                using (var reader = existing.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        found = true; same = reader.GetString(0) == surface && reader.GetString(1) == reviewer
                            && reader.GetString(2) == decision && reader.GetString(3) == environment
                            && reader.GetString(4) == inputTechnology && reader.GetString(5) == viewport
                            && reader.GetString(6) == unresolved;
                    }
                }
                if (found)
                {
                    same = same && ReadCriteria(connection, transaction, id).SequenceEqual(criteria);
                    transaction.Rollback();
                    return same ? new(true, "already-present", id, null)
                        : new(false, "conflict", id, "Review id already exists with different accessibility evidence.");
                }
            }
            var now = DateTimeOffset.UtcNow.ToString("O");
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO accessibility_reviews(id, surface_id, reviewer_identity, decision, runtime_environment,
                        input_and_assistive_technology, viewport_and_zoom, unresolved_failures, reviewed_utc)
                    VALUES ($id, $surface, $reviewer, $decision, $environment, $inputTechnology, $viewport, $unresolved, $now);
                    """;
                insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$surface", surface);
                insert.Parameters.AddWithValue("$reviewer", reviewer); insert.Parameters.AddWithValue("$decision", decision);
                insert.Parameters.AddWithValue("$environment", environment); insert.Parameters.AddWithValue("$inputTechnology", inputTechnology);
                insert.Parameters.AddWithValue("$viewport", viewport); insert.Parameters.AddWithValue("$unresolved", unresolved);
                insert.Parameters.AddWithValue("$now", now); insert.ExecuteNonQuery();
            }
            foreach (var criterion in criteria)
            {
                using var insert = connection.CreateCommand(); insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO accessibility_review_criteria(review_id, criterion_id, outcome, evidence) VALUES ($review, $criterion, $outcome, $evidence);";
                insert.Parameters.AddWithValue("$review", id); insert.Parameters.AddWithValue("$criterion", criterion.CriterionId);
                insert.Parameters.AddWithValue("$outcome", criterion.Outcome); insert.Parameters.AddWithValue("$evidence", criterion.Evidence);
                insert.ExecuteNonQuery();
            }
            transaction.Commit(); return new(true, "accessibility-review-recorded", id, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    private void Initialize()
    {
        using var connection = OpenConnection(); using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS accessibility_review_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS accessibility_reviews (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE, surface_id TEXT NOT NULL,
                reviewer_identity TEXT NOT NULL, decision TEXT NOT NULL, runtime_environment TEXT NOT NULL,
                input_and_assistive_technology TEXT NOT NULL, viewport_and_zoom TEXT NOT NULL,
                unresolved_failures TEXT NOT NULL, reviewed_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS accessibility_review_criteria (
                review_id TEXT NOT NULL REFERENCES accessibility_reviews(id) ON DELETE CASCADE,
                criterion_id TEXT NOT NULL, outcome TEXT NOT NULL, evidence TEXT NOT NULL,
                PRIMARY KEY(review_id, criterion_id)
            );
            INSERT INTO accessibility_review_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            CREATE INDEX IF NOT EXISTS idx_accessibility_reviews_surface ON accessibility_reviews(surface_id, sequence);
            """;
        command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString()); command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }
    private static IReadOnlyList<AccessibilityCriterionResult> NormalizeCriteria(IReadOnlyList<AccessibilityCriterionInput>? input)
    {
        if (input is null || input.Count != CriterionIds.Length) throw new ArgumentException($"Exactly {CriterionIds.Length} accessibility criteria are required.");
        var values = new Dictionary<string, AccessibilityCriterionResult>(StringComparer.Ordinal);
        foreach (var item in input)
        {
            var id = (item.CriterionId ?? "").Trim().ToLowerInvariant(); if (!CriterionSet.Contains(id)) throw new ArgumentException($"Unknown accessibility criterion {id}.");
            var outcome = RequireChoice(item.Outcome, $"outcome for {id}", Outcomes); var evidence = RequireText(item.Evidence, $"evidence for {id}", 5, 4000);
            if (!values.TryAdd(id, new(id, outcome, evidence))) throw new ArgumentException($"Criterion {id} appears more than once.");
        }
        if (values.Count != CriterionIds.Length) throw new ArgumentException("Every accessibility criterion is required.");
        return CriterionIds.Select(id => values[id]).ToArray();
    }
    private static IReadOnlyList<AccessibilityCriterionResult> ReadCriteria(SqliteConnection connection, SqliteTransaction transaction, string reviewId)
    {
        var values = new Dictionary<string, AccessibilityCriterionResult>(StringComparer.Ordinal); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT criterion_id, outcome, evidence FROM accessibility_review_criteria WHERE review_id=$id;"; command.Parameters.AddWithValue("$id", reviewId);
        using var reader = command.ExecuteReader(); while (reader.Read()) values[reader.GetString(0)] = new(reader.GetString(0), reader.GetString(1), reader.GetString(2));
        return CriterionIds.Where(values.ContainsKey).Select(id => values[id]).ToArray();
    }
    private static string RequireId(string? value, string field) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-96 lowercase letters, numbers, hyphens or underscores."); return normalized; }
    private static string RequireText(string? value, string field, int minimum, int maximum) { var normalized = (value ?? "").Trim(); if (normalized.Length < minimum || normalized.Length > maximum) throw new ArgumentException($"{field} must be {minimum}-{maximum} characters."); return normalized; }
    private static string RequireChoice(string? value, string field, HashSet<string> choices) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!choices.Contains(normalized)) throw new ArgumentException($"{field} is not supported."); return normalized; }
}

internal sealed record AccessibilityReviewOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, IReadOnlyList<string> CriterionIds,
    IReadOnlyList<AccessibilityReviewRecord> Reviews, IReadOnlyList<string> Boundaries);
internal sealed record AccessibilityReviewRecord(string Id, string SurfaceId, string ReviewerIdentity, string Decision,
    string RuntimeEnvironment, string InputAndAssistiveTechnology, string ViewportAndZoom, string UnresolvedFailures,
    string ReviewedUtc, int CriterionCount, int FailedCount, int NotObservedCount);
internal sealed record AccessibilityReviewInput(string ReviewId, string SurfaceId, string ReviewerIdentity, string Decision,
    string RuntimeEnvironment, string InputAndAssistiveTechnology, string ViewportAndZoom, string UnresolvedFailures,
    IReadOnlyList<AccessibilityCriterionInput>? Criteria);
internal sealed record AccessibilityCriterionInput(string CriterionId, string Outcome, string Evidence);
internal sealed record AccessibilityCriterionResult(string CriterionId, string Outcome, string Evidence);
internal sealed record AccessibilityReviewMutation(bool Ok, string State, string? Id, string? Error);
