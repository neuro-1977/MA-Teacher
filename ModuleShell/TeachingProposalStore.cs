using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class TeachingProposalStore
{
    private const int SchemaVersion = 1;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> ProposalKinds = new(StringComparer.Ordinal)
    {
        "lesson-outline", "explanation", "worked-example", "guided-practice", "independent-practice", "check-draft", "differentiation", "feedback-draft",
    };
    private static readonly HashSet<string> ProducerKinds = new(StringComparer.Ordinal)
    {
        "human", "local-model", "browser-assisted-agent", "external-agent", "imported",
    };
    private static readonly HashSet<string> ReviewDecisions = new(StringComparer.Ordinal)
    {
        "accepted-for-editing", "rejected", "deferred",
    };
    private readonly string _connectionString;

    public TeachingProposalStore(string moduleRoot)
    {
        var dataRoot = Path.Combine(moduleRoot, "data");
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

    public TeachingProposalOverview GetOverview()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.id, p.study_plan_id, plan.subject, plan.learning_stage, p.proposal_kind,
                   p.producer_kind, p.producer_identity, p.recorded_by, p.content, p.rationale,
                   p.limitations, p.status, p.created_utc,
                   (SELECT COUNT(*) FROM teaching_proposal_evidence evidence WHERE evidence.proposal_id=p.id),
                   review.id, review.reviewer_identity, review.decision, review.note, review.reviewed_utc
            FROM teaching_proposals p
            JOIN study_plans plan ON plan.id=p.study_plan_id
            LEFT JOIN teaching_proposal_reviews review ON review.sequence=(
                SELECT MAX(candidate.sequence) FROM teaching_proposal_reviews candidate WHERE candidate.proposal_id=p.id)
            ORDER BY p.created_utc, p.id;
            """;
        using var reader = command.ExecuteReader();
        var proposals = new List<TeachingProposalRecord>();
        while (reader.Read())
        {
            proposals.Add(new TeachingProposalRecord(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9),
                reader.GetString(10), reader.GetString(11), reader.GetString(12), reader.GetInt32(13),
                reader.IsDBNull(14) ? null : reader.GetString(14), reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16), reader.IsDBNull(17) ? null : reader.GetString(17),
                reader.IsDBNull(18) ? null : reader.GetString(18)));
        }
        return new TeachingProposalOverview(true, "install-root-sqlite", SchemaVersion, proposals,
        [
            "A proposal is unverified draft material, never accepted curriculum or a lesson mutation.",
            "Producer and reviewer identity fields are recorded claims; this store cannot prove who controlled the client.",
            "Accepted-for-editing permits deliberate human editing only and does not approve, publish or apply content.",
            "No model is invoked by this store, API or workspace surface.",
            "No proposal or review implies learner ability, need, diagnosis, score or mastery.",
        ]);
    }

    public TeachingProposalMutation CreateProposal(TeachingProposalInput input)
    {
        try
        {
            var id = RequireId(input.Id, "proposal id");
            var studyPlanId = RequireId(input.StudyPlanId, "study plan id");
            var proposalKind = RequireChoice(input.ProposalKind, "proposal kind", ProposalKinds);
            var producerKind = RequireChoice(input.ProducerKind, "producer kind", ProducerKinds);
            var producerIdentity = RequireText(input.ProducerIdentity, "producer identity", 2, 160);
            var recordedBy = RequireText(input.RecordedBy, "recorded by", 2, 120);
            var content = RequireText(input.Content, "proposal content", 20, 16000);
            var rationale = RequireText(input.Rationale, "rationale", 10, 4000);
            var limitations = RequireText(input.Limitations, "limitations", 5, 4000);
            var evidenceIds = (input.CurriculumCandidateIds ?? Array.Empty<string>())
                .Select(value => RequireId(value, "candidate id"))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (evidenceIds.Length is < 1 or > 12) throw new ArgumentException("A proposal requires 1-12 accepted curriculum candidates.");

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            string subject;
            string learningStage;
            using (var plan = connection.CreateCommand())
            {
                plan.Transaction = transaction;
                plan.CommandText = "SELECT subject, learning_stage, status FROM study_plans WHERE id=$id;";
                plan.Parameters.AddWithValue("$id", studyPlanId);
                using var reader = plan.ExecuteReader();
                if (!reader.Read()) return Rollback(transaction, "invalid", id, "Study plan does not exist.");
                subject = reader.GetString(0);
                learningStage = reader.GetString(1);
                if (reader.GetString(2) != "active") return Rollback(transaction, "invalid", id, "Study plan is not active.");
            }

            foreach (var candidateId in evidenceIds)
            {
                using var evidence = connection.CreateCommand();
                evidence.Transaction = transaction;
                evidence.CommandText = """
                    SELECT COUNT(*) FROM curriculum_statements
                    WHERE id=$id AND subject=$subject AND learning_stage=$stage AND review_state='accepted';
                    """;
                evidence.Parameters.AddWithValue("$id", candidateId);
                evidence.Parameters.AddWithValue("$subject", subject);
                evidence.Parameters.AddWithValue("$stage", learningStage);
                if (Convert.ToInt64(evidence.ExecuteScalar()) != 1)
                    return Rollback(transaction, "invalid", id, $"Candidate {candidateId} is not accepted evidence for this plan subject and stage.");
            }

            var exists = false;
            var sameContent = false;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT study_plan_id, proposal_kind, producer_kind, producer_identity, recorded_by,
                           content, rationale, limitations, status FROM teaching_proposals WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id);
                using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    exists = true;
                    sameContent = reader.GetString(0) == studyPlanId && reader.GetString(1) == proposalKind
                        && reader.GetString(2) == producerKind && reader.GetString(3) == producerIdentity
                        && reader.GetString(4) == recordedBy && reader.GetString(5) == content
                        && reader.GetString(6) == rationale && reader.GetString(7) == limitations
                        && reader.GetString(8) == "proposed-unreviewed";
                }
            }
            if (exists)
            {
                var linked = new List<string>();
                using var links = connection.CreateCommand();
                links.Transaction = transaction;
                links.CommandText = "SELECT curriculum_statement_id FROM teaching_proposal_evidence WHERE proposal_id=$id ORDER BY curriculum_statement_id;";
                links.Parameters.AddWithValue("$id", id);
                using var reader = links.ExecuteReader();
                while (reader.Read()) linked.Add(reader.GetString(0));
                var same = sameContent && linked.SequenceEqual(evidenceIds, StringComparer.Ordinal);
                transaction.Rollback();
                return same ? new(true, "already-present", id, null) : new(false, "conflict", id, "Proposal id already exists with different content or evidence.");
            }

            var now = DateTimeOffset.UtcNow.ToString("O");
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO teaching_proposals(
                        id, study_plan_id, proposal_kind, producer_kind, producer_identity, recorded_by,
                        content, rationale, limitations, status, created_utc)
                    VALUES ($id, $plan, $kind, $producerKind, $producer, $recordedBy,
                            $content, $rationale, $limitations, 'proposed-unreviewed', $now);
                    """;
                insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$plan", studyPlanId);
                insert.Parameters.AddWithValue("$kind", proposalKind); insert.Parameters.AddWithValue("$producerKind", producerKind);
                insert.Parameters.AddWithValue("$producer", producerIdentity); insert.Parameters.AddWithValue("$recordedBy", recordedBy);
                insert.Parameters.AddWithValue("$content", content); insert.Parameters.AddWithValue("$rationale", rationale);
                insert.Parameters.AddWithValue("$limitations", limitations); insert.Parameters.AddWithValue("$now", now);
                insert.ExecuteNonQuery();
            }
            foreach (var candidateId in evidenceIds)
            {
                using var link = connection.CreateCommand();
                link.Transaction = transaction;
                link.CommandText = "INSERT INTO teaching_proposal_evidence(proposal_id, curriculum_statement_id) VALUES ($proposal, $candidate);";
                link.Parameters.AddWithValue("$proposal", id); link.Parameters.AddWithValue("$candidate", candidateId);
                link.ExecuteNonQuery();
            }
            InsertEvent(connection, transaction, "proposal", id, "created-unreviewed", now, recordedBy);
            transaction.Commit();
            return new(true, "created-unreviewed", id, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    public TeachingProposalMutation ReviewProposal(TeachingProposalReviewInput input)
    {
        try
        {
            var reviewId = RequireId(input.ReviewId, "review id");
            var proposalId = RequireId(input.ProposalId, "proposal id");
            var reviewer = RequireText(input.ReviewerIdentity, "reviewer identity", 2, 120);
            var decision = RequireChoice(input.Decision, "decision", ReviewDecisions);
            var note = RequireText(input.Note, "review note", 5, 4000);
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var proposal = connection.CreateCommand())
            {
                proposal.Transaction = transaction;
                proposal.CommandText = "SELECT COUNT(*) FROM teaching_proposals WHERE id=$id;";
                proposal.Parameters.AddWithValue("$id", proposalId);
                if (Convert.ToInt64(proposal.ExecuteScalar()) != 1)
                    return Rollback(transaction, "invalid", reviewId, "Proposal does not exist.");
            }
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = "SELECT proposal_id, reviewer_identity, decision, note FROM teaching_proposal_reviews WHERE id=$id;";
                existing.Parameters.AddWithValue("$id", reviewId);
                using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    var same = reader.GetString(0) == proposalId && reader.GetString(1) == reviewer
                        && reader.GetString(2) == decision && reader.GetString(3) == note;
                    transaction.Rollback();
                    return same ? new(true, "already-present", reviewId, null) : new(false, "conflict", reviewId, "Review id already exists with different content.");
                }
            }
            var now = DateTimeOffset.UtcNow.ToString("O");
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO teaching_proposal_reviews(id, proposal_id, reviewer_identity, decision, note, reviewed_utc)
                    VALUES ($id, $proposal, $reviewer, $decision, $note, $now);
                    """;
                insert.Parameters.AddWithValue("$id", reviewId); insert.Parameters.AddWithValue("$proposal", proposalId);
                insert.Parameters.AddWithValue("$reviewer", reviewer); insert.Parameters.AddWithValue("$decision", decision);
                insert.Parameters.AddWithValue("$note", note); insert.Parameters.AddWithValue("$now", now);
                insert.ExecuteNonQuery();
            }
            InsertEvent(connection, transaction, "proposal-review", reviewId, decision, now, reviewer);
            transaction.Commit();
            return new(true, "review-recorded", reviewId, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS teaching_proposal_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS teaching_proposals (
                id TEXT PRIMARY KEY,
                study_plan_id TEXT NOT NULL REFERENCES study_plans(id),
                proposal_kind TEXT NOT NULL,
                producer_kind TEXT NOT NULL,
                producer_identity TEXT NOT NULL,
                recorded_by TEXT NOT NULL,
                content TEXT NOT NULL,
                rationale TEXT NOT NULL,
                limitations TEXT NOT NULL,
                status TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS teaching_proposal_evidence (
                proposal_id TEXT NOT NULL REFERENCES teaching_proposals(id) ON DELETE CASCADE,
                curriculum_statement_id TEXT NOT NULL REFERENCES curriculum_statements(id),
                PRIMARY KEY(proposal_id, curriculum_statement_id)
            );
            CREATE TABLE IF NOT EXISTS teaching_proposal_reviews (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT NOT NULL UNIQUE,
                proposal_id TEXT NOT NULL REFERENCES teaching_proposals(id),
                reviewer_identity TEXT NOT NULL,
                decision TEXT NOT NULL,
                note TEXT NOT NULL,
                reviewed_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS teaching_proposal_events (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                occurred_utc TEXT NOT NULL,
                entity_kind TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                action TEXT NOT NULL,
                actor TEXT NOT NULL
            );
            INSERT INTO teaching_proposal_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            CREATE INDEX IF NOT EXISTS idx_teaching_proposals_plan ON teaching_proposals(study_plan_id, created_utc);
            CREATE INDEX IF NOT EXISTS idx_teaching_proposal_reviews_proposal ON teaching_proposal_reviews(proposal_id, sequence);
            """;
        command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString());
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

    private static string RequireId(string? value, string field)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-64 lowercase letters, numbers, hyphens or underscores.");
        return normalized;
    }

    private static string RequireText(string? value, string field, int minimum, int maximum)
    {
        var normalized = (value ?? "").Trim();
        if (normalized.Length < minimum || normalized.Length > maximum) throw new ArgumentException($"{field} must be {minimum}-{maximum} characters.");
        return normalized;
    }

    private static string RequireChoice(string? value, string field, HashSet<string> choices)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        if (!choices.Contains(normalized)) throw new ArgumentException($"{field} is not supported.");
        return normalized;
    }

    private static TeachingProposalMutation Rollback(SqliteTransaction transaction, string state, string id, string error)
    {
        transaction.Rollback();
        return new(false, state, id, error);
    }

    private static void InsertEvent(SqliteConnection connection, SqliteTransaction transaction, string kind, string id, string action, string now, string actor)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO teaching_proposal_events(occurred_utc, entity_kind, entity_id, action, actor) VALUES ($now, $kind, $id, $action, $actor);";
        command.Parameters.AddWithValue("$now", now); command.Parameters.AddWithValue("$kind", kind);
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$actor", actor); command.ExecuteNonQuery();
    }
}

internal sealed record TeachingProposalOverview(bool Ok, string DatabaseAuthority, int SchemaVersion,
    IReadOnlyList<TeachingProposalRecord> Proposals, IReadOnlyList<string> Boundaries);
internal sealed record TeachingProposalRecord(string Id, string StudyPlanId, string Subject, string LearningStage,
    string ProposalKind, string ProducerKind, string ProducerIdentity, string RecordedBy, string Content,
    string Rationale, string Limitations, string Status, string CreatedUtc, int EvidenceCount,
    string? LatestReviewId, string? LatestReviewerIdentity, string? LatestDecision, string? LatestReviewNote, string? LatestReviewedUtc);
internal sealed record TeachingProposalInput(string Id, string StudyPlanId, string ProposalKind, string ProducerKind,
    string ProducerIdentity, string RecordedBy, string Content, string Rationale, string Limitations,
    IReadOnlyList<string>? CurriculumCandidateIds);
internal sealed record TeachingProposalReviewInput(string ReviewId, string ProposalId, string ReviewerIdentity, string Decision, string Note);
internal sealed record TeachingProposalMutation(bool Ok, string State, string? Id, string? Error);
