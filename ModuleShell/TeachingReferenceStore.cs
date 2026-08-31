using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class TeachingReferenceStore
{
    private const int SchemaVersion = 1;
    private const string SnapshotDate = "2026-08-30";
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> Dispositions = new(StringComparer.Ordinal) { "current-reviewed", "needs-update", "retired-from-guidance", "deferred" };
    private static readonly HashSet<string> LinkStates = new(StringComparer.Ordinal) { "reachable", "redirected-reviewed", "unreachable", "not-checked" };
    private static readonly HashSet<string> RightsStates = new(StringComparer.Ordinal) { "reviewed", "needs-review", "not-applicable" };
    private static readonly HashSet<string> SummaryStates = new(StringComparer.Ordinal) { "matches-current-source", "needs-update", "not-checked" };
    private readonly string _connectionString;

    public TeachingReferenceStore(string moduleRoot)
    {
        var dataRoot = Path.GetFullPath(moduleRoot);
        Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public TeachingReferenceOverview GetOverview()
    {
        using var connection = OpenConnection();
        var sources = new List<TeachingReferenceSource>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, publisher, title, authority_class, source_url, published_date,
                       editorial_snapshot_date, scope, use_boundary, review_state
                FROM teaching_reference_sources ORDER BY sort_order, id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                sources.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9)));
        }

        var principles = new List<TeachingReferencePrinciple>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT p.id, p.source_id, p.category, p.title, p.summary, p.applicability,
                       p.caution, p.source_locator, p.evidence_state
                FROM teaching_reference_principles p
                JOIN teaching_reference_sources s ON s.id=p.source_id
                ORDER BY p.sort_order, p.id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                principles.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)));
        }

        var events = new List<TeachingReferenceEvent>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT event_id, actor, occurred_utc, activity, evidence_state, crew_activity
                FROM teaching_reference_events ORDER BY occurred_utc, event_id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                events.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }

        var fingerprints = sources.ToDictionary(source => source.Id,
            source => Fingerprint(source, principles.Where(principle => principle.SourceId == source.Id)), StringComparer.Ordinal);
        var reviews = new List<TeachingReferenceReview>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, source_id, source_fingerprint, reviewer_identity, disposition, link_state,
                       rights_state, summary_state, next_review_date, note, reviewed_utc
                FROM teaching_reference_reviews ORDER BY sequence, id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var sourceId = reader.GetString(1); var fingerprint = reader.GetString(2);
                reviews.Add(new(reader.GetString(0), sourceId, fingerprint, reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9),
                    reader.GetString(10), fingerprints.TryGetValue(sourceId, out var current) && current == fingerprint));
            }
        }
        var states = sources.Select(source =>
        {
            var sourceReviews = reviews.Where(review => review.SourceId == source.Id).ToArray(); var latest = sourceReviews.LastOrDefault();
            return new TeachingReferenceSourceState(source.Id, fingerprints[source.Id], latest?.Disposition,
                latest?.FingerprintCurrent ?? false, latest?.NextReviewDate, sourceReviews.Length);
        }).ToArray();

        return new TeachingReferenceOverview(true, "install-root-sqlite", SchemaVersion, SnapshotDate, sources, principles, events, states, reviews,
            new[]
            {
                "Reference summaries guide operator judgement; they are not accepted curriculum statements.",
                "The registry contains links and short original summaries, not copied guidance reports.",
                "Current-source review is required before a reference informs a classroom-ready lesson.",
                "No learning-style tailoring: the cited ITTECF says distinct identifiable learning styles are unsupported.",
                "No autonomous pedagogy selection or learner assessment is implemented.",
            });
    }

    public TeachingReferenceMutation ReviewSource(TeachingReferenceReviewInput input)
    {
        try
        {
            var id = RequireId(input.ReviewId, "review id"); var sourceId = RequireId(input.SourceId, "source id");
            var reviewer = RequireText(input.ReviewerIdentity, "reviewer identity", 2, 120);
            var disposition = RequireChoice(input.Disposition, "disposition", Dispositions);
            var linkState = RequireChoice(input.LinkState, "link state", LinkStates);
            var rightsState = RequireChoice(input.RightsState, "rights state", RightsStates);
            var summaryState = RequireChoice(input.SummaryState, "summary state", SummaryStates);
            var nextReviewDate = RequireDate(input.NextReviewDate); var note = RequireText(input.Note, "review note", 5, 4000);
            if (disposition == "current-reviewed" && linkState is not ("reachable" or "redirected-reviewed"))
                throw new ArgumentException("Current-reviewed requires a positively reviewed link state.");
            if (disposition == "current-reviewed" && rightsState is not ("reviewed" or "not-applicable"))
                throw new ArgumentException("Current-reviewed requires a resolved rights state.");
            if (disposition == "current-reviewed" && summaryState != "matches-current-source")
                throw new ArgumentException("Current-reviewed requires the editorial summary to match the reviewed source.");

            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            var snapshot = ReadSnapshot(connection, transaction, sourceId);
            if (snapshot is null) return Rollback(transaction, "invalid", id, "Teaching reference source does not exist.");
            TeachingReferenceMutation? duplicateResult = null;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction; existing.CommandText = """
                    SELECT source_id, source_fingerprint, reviewer_identity, disposition, link_state,
                           rights_state, summary_state, next_review_date, note
                    FROM teaching_reference_reviews WHERE id=$id;
                    """; existing.Parameters.AddWithValue("$id", id); using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    var same = reader.GetString(0) == sourceId && reader.GetString(1) == snapshot.Fingerprint
                        && reader.GetString(2) == reviewer && reader.GetString(3) == disposition && reader.GetString(4) == linkState
                        && reader.GetString(5) == rightsState && reader.GetString(6) == summaryState
                        && reader.GetString(7) == nextReviewDate && reader.GetString(8) == note;
                    duplicateResult = same ? new(true, "already-present", id, snapshot.Fingerprint, null)
                        : new(false, "conflict", id, snapshot.Fingerprint, "Review id already exists with different source evidence or content.");
                }
            }
            if (duplicateResult is not null) { transaction.Rollback(); return duplicateResult; }
            var now = DateTimeOffset.UtcNow.ToString("O"); using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction; insert.CommandText = """
                    INSERT INTO teaching_reference_reviews(id, source_id, source_fingerprint, reviewer_identity,
                        disposition, link_state, rights_state, summary_state, next_review_date, note, reviewed_utc)
                    VALUES ($id, $source, $fingerprint, $reviewer, $disposition, $link, $rights, $summary, $next, $note, $now);
                    """;
                insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$source", sourceId);
                insert.Parameters.AddWithValue("$fingerprint", snapshot.Fingerprint); insert.Parameters.AddWithValue("$reviewer", reviewer);
                insert.Parameters.AddWithValue("$disposition", disposition); insert.Parameters.AddWithValue("$link", linkState);
                insert.Parameters.AddWithValue("$rights", rightsState); insert.Parameters.AddWithValue("$summary", summaryState);
                insert.Parameters.AddWithValue("$next", nextReviewDate); insert.Parameters.AddWithValue("$note", note);
                insert.Parameters.AddWithValue("$now", now); insert.ExecuteNonQuery();
            }
            transaction.Commit(); return new(true, "review-recorded", id, snapshot.Fingerprint, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, null, exception.Message); }
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS teaching_reference_sources (
                    id TEXT PRIMARY KEY, publisher TEXT NOT NULL, title TEXT NOT NULL, authority_class TEXT NOT NULL,
                    source_url TEXT NOT NULL, published_date TEXT NOT NULL, editorial_snapshot_date TEXT NOT NULL,
                    scope TEXT NOT NULL, use_boundary TEXT NOT NULL, review_state TEXT NOT NULL, sort_order INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS teaching_reference_principles (
                    id TEXT PRIMARY KEY, source_id TEXT NOT NULL REFERENCES teaching_reference_sources(id), category TEXT NOT NULL,
                    title TEXT NOT NULL, summary TEXT NOT NULL, applicability TEXT NOT NULL, caution TEXT NOT NULL,
                    source_locator TEXT NOT NULL, evidence_state TEXT NOT NULL, sort_order INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS teaching_reference_events (
                    event_id TEXT PRIMARY KEY, actor TEXT NOT NULL, occurred_utc TEXT NOT NULL, activity TEXT NOT NULL,
                    evidence_state TEXT NOT NULL, crew_activity TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS teaching_reference_reviews (
                    sequence INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE,
                    source_id TEXT NOT NULL REFERENCES teaching_reference_sources(id), source_fingerprint TEXT NOT NULL,
                    reviewer_identity TEXT NOT NULL, disposition TEXT NOT NULL, link_state TEXT NOT NULL,
                    rights_state TEXT NOT NULL, summary_state TEXT NOT NULL, next_review_date TEXT NOT NULL,
                    note TEXT NOT NULL, reviewed_utc TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_teaching_reference_reviews_source
                    ON teaching_reference_reviews(source_id, sequence);
                """;
            command.ExecuteNonQuery();
        }

        UpsertSource(connection, transaction, new("national-curriculum-framework", "Department for Education",
            "National curriculum in England: framework for key stages 1 to 4", "statutory-curriculum-framework",
            "https://www.gov.uk/government/publications/national-curriculum-in-england-framework-for-key-stages-1-to-4/the-national-curriculum-in-england-framework-for-key-stages-1-to-4",
            "2014", SnapshotDate, "Maintained schools in England; KS1-KS4 subject framework, inclusion and cross-curricular expectations.",
            "Use for statutory scope and source discovery, not as proof that a lesson activity is pedagogically effective.", "editorial-source-review-required", 10));
        UpsertSource(connection, transaction, new("ittecf-2024", "Department for Education",
            "Initial Teacher Training and Early Career Framework", "professional-framework",
            "https://assets.publishing.service.gov.uk/media/661d24ac08c3be25cfbd3e61/Initial_Teacher_Training_and_Early_Career_Framework.pdf",
            "2024", SnapshotDate, "Evidence-informed professional framework covering expectations, learning, curriculum, classroom practice, adaptive teaching and assessment.",
            "Use as a professional-practice reference; it is not a learner assessment rubric or automatic lesson generator.", "editorial-source-review-required", 20));
        UpsertSource(connection, transaction, new("eef-toolkit-use", "Education Endowment Foundation", "Using the Toolkits",
            "evidence-interpretation-guidance", "https://educationendowmentfoundation.org.uk/education-evidence/using-the-toolkits",
            "current web guidance", SnapshotDate, "How to interpret the Teaching and Learning Toolkit and Early Years Toolkit.",
            "Treat approaches as context-dependent best bets requiring professional judgement, not deterministic prescriptions.", "editorial-source-review-required", 30));
        UpsertSource(connection, transaction, new("eef-metacognition-2025", "Education Endowment Foundation",
            "Metacognition and Self-Regulated Learning, second edition", "evidence-guidance-report",
            "https://educationendowmentfoundation.org.uk/education-evidence/guidance-reports/metacognition", "2025-11-13", SnapshotDate,
            "Primary and secondary guidance for supporting metacognitive knowledge and self-regulation.",
            "Use within subject teaching and local context; do not infer a universal script or measured learner impact.", "editorial-source-review-required", 40));

        UpsertPrinciple(connection, transaction, new("curriculum-core-not-complete-lesson", "national-curriculum-framework", "curriculum",
            "Curriculum content is a foundation, not a finished lesson",
            "The statutory framework provides an outline of core knowledge around which teachers develop lessons.",
            "Use accepted curriculum statements to anchor lesson objectives and content coverage.",
            "Do not treat a statutory statement as a complete sequence, explanation, activity or assessment.",
            "sections 3.1-3.4", "editorial-summary-source-linked-review-required", 10));
        UpsertPrinciple(connection, transaction, new("high-expectations-and-barriers", "national-curriculum-framework", "inclusion",
            "High expectations with responsive planning",
            "Planning should set ambitious expectations while responding to prior attainment and barriers to learning.",
            "Use learner context to identify support and access needs without lowering the curriculum goal by default.",
            "Accessibility support can require specialist or safeguarding input beyond this application.",
            "section 4", "editorial-summary-source-linked-review-required", 20));
        UpsertPrinciple(connection, transaction, new("adaptive-not-learning-styles", "ittecf-2024", "adaptive-teaching",
            "Adapt responsively, not by presumed learning style",
            "Responsive support should consider prior knowledge and barriers; distinct identifiable learning styles are not supported by evidence.",
            "Adjust scaffolding, practice, grouping or representation in response to observed need and formative evidence.",
            "Do not label learners as visual, auditory or kinaesthetic and generate fixed tracks from that label.",
            "Adaptive Teaching, Standard 5", "editorial-summary-source-linked-review-required", 30));
        UpsertPrinciple(connection, transaction, new("assessment-serves-decision", "ittecf-2024", "assessment",
            "Assessment must support a defined decision",
            "Assessment is useful when its purpose is clear and its information changes a teaching or learner action.",
            "Define the decision and evidence of understanding before authoring a check or task.",
            "Busy-looking work, confidence or fluent output is not sufficient evidence of understanding.",
            "Assessment, Standard 6", "editorial-summary-source-linked-review-required", 40));
        UpsertPrinciple(connection, transaction, new("toolkit-professional-judgement", "eef-toolkit-use", "evidence-use",
            "Evidence informs judgement; it does not replace it",
            "The EEF describes toolkit findings as best bets rather than definitive answers for an individual setting.",
            "Record why an approach fits this subject, learner, objective and context.",
            "Do not rank or auto-select teaching approaches from headline impact estimates alone.",
            "About the Toolkits", "editorial-summary-source-linked-review-required", 50));
        UpsertPrinciple(connection, transaction, new("metacognition-in-subject", "eef-metacognition-2025", "metacognition",
            "Teach planning, monitoring and evaluation within subjects",
            "Metacognitive and self-regulatory strategies are intended to help pupils plan, monitor and evaluate their learning and appear stronger when embedded in subject lessons.",
            "Use explicit modelling, prompts and reflection around a real subject task.",
            "A generic reflection box is not evidence that metacognition was taught or that attainment improved.",
            "overview", "editorial-summary-source-linked-review-required", 60));

        using (var receipt = connection.CreateCommand())
        {
            receipt.Transaction = transaction;
            receipt.CommandText = """
                INSERT OR IGNORE INTO teaching_reference_events(event_id, actor, occurred_utc, activity, evidence_state, crew_activity)
                VALUES ('ma-teacher-reference-registry-v1', 'Codex solo', $occurredUtc,
                        'Added a source-linked teaching-reference registry with statutory curriculum, ITTECF and EEF interpretation boundaries.',
                        'source-implemented-unverified',
                        'None. No external assistant, external automation, crew agent or browser agent performed product work. Web research was executed directly by Codex.');
                """;
            receipt.Parameters.AddWithValue("$occurredUtc", DateTimeOffset.UtcNow.ToString("O"));
            receipt.ExecuteNonQuery();
        }
        SetSchemaVersion(connection, transaction);
        transaction.Commit();
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

    private static void UpsertSource(SqliteConnection connection, SqliteTransaction transaction, TeachingReferenceSourceSeed value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO teaching_reference_sources(id, publisher, title, authority_class, source_url, published_date,
                editorial_snapshot_date, scope, use_boundary, review_state, sort_order)
            VALUES ($id, $publisher, $title, $authorityClass, $sourceUrl, $publishedDate, $snapshotDate, $scope, $boundary, $reviewState, $sortOrder)
            ON CONFLICT(id) DO UPDATE SET publisher=excluded.publisher, title=excluded.title,
                authority_class=excluded.authority_class, source_url=excluded.source_url, published_date=excluded.published_date,
                editorial_snapshot_date=excluded.editorial_snapshot_date, scope=excluded.scope,
                use_boundary=excluded.use_boundary, review_state=excluded.review_state, sort_order=excluded.sort_order;
            """;
        command.Parameters.AddWithValue("$id", value.Id); command.Parameters.AddWithValue("$publisher", value.Publisher);
        command.Parameters.AddWithValue("$title", value.Title); command.Parameters.AddWithValue("$authorityClass", value.AuthorityClass);
        command.Parameters.AddWithValue("$sourceUrl", value.SourceUrl); command.Parameters.AddWithValue("$publishedDate", value.PublishedDate);
        command.Parameters.AddWithValue("$snapshotDate", value.SnapshotDate); command.Parameters.AddWithValue("$scope", value.Scope);
        command.Parameters.AddWithValue("$boundary", value.UseBoundary); command.Parameters.AddWithValue("$reviewState", value.ReviewState);
        command.Parameters.AddWithValue("$sortOrder", value.SortOrder); command.ExecuteNonQuery();
    }

    private static void UpsertPrinciple(SqliteConnection connection, SqliteTransaction transaction, TeachingReferencePrincipleSeed value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO teaching_reference_principles(id, source_id, category, title, summary, applicability, caution,
                source_locator, evidence_state, sort_order)
            VALUES ($id, $sourceId, $category, $title, $summary, $applicability, $caution, $locator, $evidenceState, $sortOrder)
            ON CONFLICT(id) DO UPDATE SET source_id=excluded.source_id, category=excluded.category, title=excluded.title,
                summary=excluded.summary, applicability=excluded.applicability, caution=excluded.caution,
                source_locator=excluded.source_locator, evidence_state=excluded.evidence_state, sort_order=excluded.sort_order;
            """;
        command.Parameters.AddWithValue("$id", value.Id); command.Parameters.AddWithValue("$sourceId", value.SourceId);
        command.Parameters.AddWithValue("$category", value.Category); command.Parameters.AddWithValue("$title", value.Title);
        command.Parameters.AddWithValue("$summary", value.Summary); command.Parameters.AddWithValue("$applicability", value.Applicability);
        command.Parameters.AddWithValue("$caution", value.Caution); command.Parameters.AddWithValue("$locator", value.SourceLocator);
        command.Parameters.AddWithValue("$evidenceState", value.EvidenceState); command.Parameters.AddWithValue("$sortOrder", value.SortOrder);
        command.ExecuteNonQuery();
    }

    private static void SetSchemaVersion(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS teaching_reference_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            INSERT INTO teaching_reference_meta(key, value) VALUES ('schema_version', $value)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            """;
        command.Parameters.AddWithValue("$value", SchemaVersion.ToString());
        command.ExecuteNonQuery();
    }

    private static TeachingReferenceSnapshot? ReadSnapshot(SqliteConnection connection, SqliteTransaction transaction, string sourceId)
    {
        TeachingReferenceSource source;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction; command.CommandText = """
                SELECT id, publisher, title, authority_class, source_url, published_date,
                       editorial_snapshot_date, scope, use_boundary, review_state
                FROM teaching_reference_sources WHERE id=$id;
                """; command.Parameters.AddWithValue("$id", sourceId); using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            source = new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9));
        }
        var principles = new List<TeachingReferencePrinciple>(); using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction; command.CommandText = """
                SELECT id, source_id, category, title, summary, applicability, caution, source_locator, evidence_state
                FROM teaching_reference_principles WHERE source_id=$id ORDER BY sort_order, id;
                """; command.Parameters.AddWithValue("$id", sourceId); using var reader = command.ExecuteReader(); while (reader.Read())
                principles.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)));
        }
        return new(source.Id, Fingerprint(source, principles));
    }

    private static string Fingerprint(TeachingReferenceSource source, IEnumerable<TeachingReferencePrinciple> principles)
    {
        var canonical = new StringBuilder();
        foreach (var value in new[] { source.Id, source.Publisher, source.Title, source.AuthorityClass, source.SourceUrl,
            source.PublishedDate, source.EditorialSnapshotDate, source.Scope, source.UseBoundary, source.ReviewState }) AppendPart(canonical, value);
        foreach (var principle in principles.OrderBy(value => value.Id, StringComparer.Ordinal))
            foreach (var value in new[] { principle.Id, principle.SourceId, principle.Category, principle.Title, principle.Summary,
                principle.Applicability, principle.Caution, principle.SourceLocator, principle.EvidenceState }) AppendPart(canonical, value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }
    private static void AppendPart(StringBuilder target, string value) => target.Append(value.Length).Append(':').Append(value).Append(';');
    private static string RequireId(string? value, string field) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-64 lowercase letters, numbers, hyphens or underscores."); return normalized; }
    private static string RequireText(string? value, string field, int minimum, int maximum) { var normalized = (value ?? "").Trim(); if (normalized.Length < minimum || normalized.Length > maximum) throw new ArgumentException($"{field} must be {minimum}-{maximum} characters."); return normalized; }
    private static string RequireChoice(string? value, string field, HashSet<string> choices) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!choices.Contains(normalized)) throw new ArgumentException($"{field} is not supported."); return normalized; }
    private static string RequireDate(string? value) { var normalized = (value ?? "").Trim(); if (!DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new ArgumentException("next review date must use yyyy-MM-dd."); return normalized; }
    private static TeachingReferenceMutation Rollback(SqliteTransaction transaction, string state, string id, string error) { transaction.Rollback(); return new(false, state, id, null, error); }
}

internal sealed record TeachingReferenceOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, string EditorialSnapshotDate,
    IReadOnlyList<TeachingReferenceSource> Sources, IReadOnlyList<TeachingReferencePrinciple> Principles,
    IReadOnlyList<TeachingReferenceEvent> Events, IReadOnlyList<TeachingReferenceSourceState> SourceStates,
    IReadOnlyList<TeachingReferenceReview> Reviews, IReadOnlyList<string> Boundaries);
internal sealed record TeachingReferenceSource(string Id, string Publisher, string Title, string AuthorityClass, string SourceUrl,
    string PublishedDate, string EditorialSnapshotDate, string Scope, string UseBoundary, string ReviewState);
internal sealed record TeachingReferencePrinciple(string Id, string SourceId, string Category, string Title, string Summary,
    string Applicability, string Caution, string SourceLocator, string EvidenceState);
internal sealed record TeachingReferenceEvent(string EventId, string Actor, string OccurredUtc, string Activity, string EvidenceState, string CrewActivity);
internal sealed record TeachingReferenceSourceSeed(string Id, string Publisher, string Title, string AuthorityClass,
    string SourceUrl, string PublishedDate, string SnapshotDate, string Scope, string UseBoundary, string ReviewState, int SortOrder);
internal sealed record TeachingReferencePrincipleSeed(string Id, string SourceId, string Category, string Title, string Summary,
    string Applicability, string Caution, string SourceLocator, string EvidenceState, int SortOrder);
internal sealed record TeachingReferenceSourceState(string SourceId, string CurrentFingerprint, string? LatestDisposition,
    bool LatestReviewCurrent, string? NextReviewDate, int ReviewCount);
internal sealed record TeachingReferenceReview(string Id, string SourceId, string SourceFingerprint, string ReviewerIdentity,
    string Disposition, string LinkState, string RightsState, string SummaryState, string NextReviewDate, string Note,
    string ReviewedUtc, bool FingerprintCurrent);
internal sealed record TeachingReferenceReviewInput(string ReviewId, string SourceId, string ReviewerIdentity, string Disposition,
    string LinkState, string RightsState, string SummaryState, string NextReviewDate, string Note);
internal sealed record TeachingReferenceMutation(bool Ok, string State, string? Id, string? SourceFingerprint, string? Error);
internal sealed record TeachingReferenceSnapshot(string SourceId, string Fingerprint);
