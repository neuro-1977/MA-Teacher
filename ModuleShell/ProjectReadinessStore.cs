using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class ProjectReadinessStore
{
    private const int SchemaVersion = 1;
    private readonly string _connectionString;

    public ProjectReadinessStore(string moduleRoot)
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

    public ProjectReadinessBoard GetBoard()
    {
        using var connection = OpenConnection();
        var gates = new List<ProjectReadinessGate>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, area, capability, state, evidence, next_evidence, owner_boundary
            FROM project_readiness_gates ORDER BY sort_order, id;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
            gates.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6)));
        var receipts = new List<ProjectReadinessReceipt>();
        using (var receiptCommand = connection.CreateCommand())
        {
            receiptCommand.CommandText = "SELECT event_id, actor, recorded_date, activity, evidence_state, crew_activity FROM project_readiness_receipts ORDER BY recorded_date, event_id;";
            using var receiptReader = receiptCommand.ExecuteReader();
            while (receiptReader.Read()) receipts.Add(new(receiptReader.GetString(0), receiptReader.GetString(1),
                receiptReader.GetString(2), receiptReader.GetString(3), receiptReader.GetString(4), receiptReader.GetString(5)));
        }
        var productVersion = typeof(ProjectReadinessStore).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        return new ProjectReadinessBoard(true, "install-root-sqlite", SchemaVersion, productVersion, "not-complete", gates, receipts,
            new[]
            {
                "Source implementation is not build or runtime proof.",
                "No capability may advance from implemented-unverified without evidence named by its gate.",
                "No model assistance may become accepted curriculum or assessment truth without explicit review.",
                "Future agents should update these gates transactionally with the evidence they actually gather.",
            });
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS project_readiness_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS project_readiness_gates (
                    id TEXT PRIMARY KEY, area TEXT NOT NULL, capability TEXT NOT NULL, state TEXT NOT NULL,
                    evidence TEXT NOT NULL, next_evidence TEXT NOT NULL, owner_boundary TEXT NOT NULL, sort_order INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS project_readiness_receipts (
                    event_id TEXT PRIMARY KEY, actor TEXT NOT NULL, recorded_date TEXT NOT NULL, activity TEXT NOT NULL,
                    evidence_state TEXT NOT NULL, crew_activity TEXT NOT NULL
                );
                INSERT INTO project_readiness_meta(key, value) VALUES ('schema_version', $schemaVersion)
                ON CONFLICT(key) DO UPDATE SET value=excluded.value;
                """;
            command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString());
            command.ExecuteNonQuery();
        }

        Upsert(connection, transaction, new("source-registry", "curriculum", "Official source registry and versioned HTML capture",
            "implemented-unverified", "Source, redirect, size, hash and intent boundaries exist in source.",
            "Build; capture one allowlisted page; verify persisted hash, duplicate handling and restart survival.", "operator-controlled network capture", 10));
        Upsert(connection, transaction, new("document-capture", "curriculum", "PDF, ODT and DOCX discovery and capture",
            "implemented-unverified", "Allowlisted discovery, bounded download, redirect and immutable revision source exists.",
            "Capture one document of each supported type and prove body integrity after restart.", "operator-controlled network capture", 20));
        Upsert(connection, transaction, new("document-extraction", "curriculum", "Bounded document text extraction",
            "implemented-unverified", "PdfPig, ODT and DOCX extraction paths and immutable block schema exist.",
            "Run representative text PDFs and office documents; prove bounds; record image-only PDF refusal.", "machine extraction remains unreviewed", 30));
        Upsert(connection, transaction, new("candidate-review", "curriculum", "Deterministic curriculum candidate review",
            "implemented-unverified", "HTML and document block candidates share explicit accept/reject state.",
            "Exercise accepted, rejected, duplicate, hash mismatch and restart cases.", "human review required", 40));
        Upsert(connection, transaction, new("revision-drift", "curriculum", "Source and extraction drift reconciliation",
            "implemented-unverified", "Byte and extracted-item fingerprints plus guarded dispositions exist.",
            "Create controlled revision changes and prove every drift state and no automatic lesson mutation.", "operator disposition required", 50));
        Upsert(connection, transaction, new("learner-plans", "learning", "Local learner profiles and study plans",
            "implemented-unverified", "Idempotent local mutations, privacy boundary and workspace UI exist.",
            "Create, replay, conflict and restart-test disposable learner and plan records.", "local-only personally identifying data", 60));
        Upsert(connection, transaction, new("lesson-drafting", "learning", "Evidence-linked structured lesson drafts",
            "implemented-unverified", "Active-plan, reviewed-candidate, subject, exact-stage, section and idempotency gates exist.",
            "Draft one English and one science lesson; prove evidence-link replay/conflict and responsive UI.", "operator-authored draft, subject facts unverified", 70));
        Upsert(connection, transaction, new("lesson-review", "learning", "Exact-fingerprint human lesson review gate",
            "implemented-unverified", "Immutable criterion-complete review records and current-fingerprint approval gating exist in source.",
            "Build; prove fingerprint determinism, every criterion/refusal state, immutable replay/conflict, stale review behavior, restart and practice lock/unlock.", "reviewer identity is recorded but not authenticated", 75));
        Upsert(connection, transaction, new("teaching-references", "pedagogy", "Source-linked teaching reference registry",
            "review-workflow-implemented-unverified", "Statutory, ITTECF and EEF summaries are separated from curriculum truth; immutable fingerprint-bound freshness dispositions exist in source.",
            "Build; review links, rights and summaries; prove replay/conflict, stale fingerprints, next-review dates, restart and no registry mutation.", "professional judgement required", 80));
        Upsert(connection, transaction, new("assessment", "learning", "Evidence-linked practice checks and reviewed learner attempts",
            "manual-workflow-implemented-unverified", "Operator-authored criteria, local learner responses and immutable human review outcomes exist in source; automated scoring does not.",
            "Build and exercise create/replay/conflict, learner ownership, unreviewed submission, human review, restart and privacy boundaries.", "must not claim automated scoring or broad mastery", 90));
        Upsert(connection, transaction, new("progress-evidence", "learning", "Read-only learner and subject progress evidence ledger",
            "implemented-unverified", "Source aggregates recorded outcomes while retaining each prompt, criteria, response, review and evidence count; it calculates no score.",
            "Build and compare summaries against controlled attempts; verify filters, zero-attempt plans, restart and no hidden grading.", "counts are not grades or mastery", 95));
        Upsert(connection, transaction, new("ai-assistance", "assistance", "Model-proposed teaching and differentiation",
            "proposal-review-foundation-implemented-unverified", "A model-agnostic evidence-linked proposal inbox and immutable review lane exist in source; no model invocation or automatic application exists.",
            "Build; prove create/replay/conflict, exact evidence gates, producer identity, immutable review history, restart and no lesson/curriculum mutation.", "model proposes; operator deliberately edits", 100));
        Upsert(connection, transaction, new("accessibility", "quality", "Keyboard, screen-reader, contrast and adaptation review",
            "not-verified", "Responsive CSS and semantic labels exist in source; no accessibility audit exists.",
            "Perform keyboard, focus, screen-reader, zoom, narrow viewport and contrast checks on a built app.", "specialist review may be required", 110));
        Upsert(connection, transaction, new("build-runtime", "delivery", "Compile, typecheck and disposable runtime smoke",
            "not-run", "A requirement-by-requirement verification contract exists; no validation command was authorized or executed for the 0.2.0 work.",
            "Run restore/build/typecheck, launch with disposable install root, inspect logs and exercise guarded APIs.", "future verification lane", 120));
        Upsert(connection, transaction, new("installer", "delivery", "Fresh installer, upgrade and uninstall",
            "not-built", "The prior 0.1.0 installer does not prove or package current 0.2.0 source.",
            "After build/runtime proof, package licenses and exact source, hash artifact, test install/upgrade/uninstall and single-root storage.", "future release lane", 130));
        Upsert(connection, transaction, new("local-backup", "storage", "Manual consistent local database snapshots and hash verification",
            "implemented-unverified", "Source creates serialized SQLite snapshots under data/backups, records SHA-256 and verifies on demand without automatic deletion.",
            "Build; create and verify a disposable backup under concurrent reads; prove capacity refusal, missing/tampered files, restart and single-root storage.", "restore and deletion deliberately absent", 125));
        InsertReceipt(connection, transaction, "ma-teacher-020-evidence-foundation", "Codex solo", "2026-08-30",
            "Added install-root curriculum capture, document extraction, candidate review, revision drift, learner, plan and evidence-linked lesson foundations.");
        InsertReceipt(connection, transaction, "ma-teacher-020-reference-and-coverage", "Codex solo", "2026-08-30",
            "Added source-linked teaching references plus explicit age, qualification and jurisdiction coverage gaps.");
        InsertReceipt(connection, transaction, "ma-teacher-020-lesson-reader", "Codex solo", "2026-08-30",
            "Added read-only lesson detail retrieval and a provenance-preserving lesson reader; assessment and AI generation remain absent.");
        InsertReceipt(connection, transaction, "ma-teacher-020-manual-learning-checks", "Codex solo", "2026-08-30",
            "Added evidence-linked operator-authored checks, learner responses and human review outcomes; automated scoring remains absent.");
        InsertReceipt(connection, transaction, "ma-teacher-020-progress-evidence", "Codex solo", "2026-08-30",
            "Added a read-only attempt ledger and learner/subject outcome counts without grades, ranking, mastery or automated recommendations.");
        InsertReceipt(connection, transaction, "ma-teacher-020-verification-contract", "Codex solo", "2026-08-30",
            "Added an API map and requirement-by-requirement verification contract; no command, runtime, interaction or package result is claimed.");
        InsertReceipt(connection, transaction, "ma-teacher-020-local-backups", "Codex solo", "2026-08-30",
            "Added manual install-root SQLite snapshots, SHA-256 receipts and on-demand verification; restore, deletion and cloud sync remain absent.");
        InsertReceipt(connection, transaction, "ma-teacher-020-workspace-navigation", "Codex solo", "2026-08-30",
            "Added stable semantic anchors and a persistent workspace navigator; corrected backup handling for unexpectedly small snapshots.");
        global::DevelopmentBreadcrumbStore.InsertCurrentSoloContinuation(connection, transaction);
        InsertReceipt(connection, transaction, "ma-teacher-020-getting-started", "Codex solo", "2026-08-30",
            "Added a read-only first-run journey driven by persisted learner, plan, curriculum, lesson, check, attempt and human-review counts.");
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

    private static void Upsert(SqliteConnection connection, SqliteTransaction transaction, ProjectReadinessGateSeed value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO project_readiness_gates(id, area, capability, state, evidence, next_evidence, owner_boundary, sort_order)
            VALUES ($id, $area, $capability, $state, $evidence, $nextEvidence, $ownerBoundary, $sortOrder)
            ON CONFLICT(id) DO UPDATE SET area=excluded.area, capability=excluded.capability, state=excluded.state,
                evidence=excluded.evidence, next_evidence=excluded.next_evidence,
                owner_boundary=excluded.owner_boundary, sort_order=excluded.sort_order;
            """;
        command.Parameters.AddWithValue("$id", value.Id); command.Parameters.AddWithValue("$area", value.Area);
        command.Parameters.AddWithValue("$capability", value.Capability); command.Parameters.AddWithValue("$state", value.State);
        command.Parameters.AddWithValue("$evidence", value.Evidence); command.Parameters.AddWithValue("$nextEvidence", value.NextEvidence);
        command.Parameters.AddWithValue("$ownerBoundary", value.OwnerBoundary); command.Parameters.AddWithValue("$sortOrder", value.SortOrder);
        command.ExecuteNonQuery();
    }

    private static void InsertReceipt(SqliteConnection connection, SqliteTransaction transaction, string eventId,
        string actor, string recordedDate, string activity)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO project_readiness_receipts(event_id, actor, recorded_date, activity, evidence_state, crew_activity)
            VALUES ($eventId, $actor, $recordedDate, $activity, 'source-implemented-unverified',
                    'None. No external assistant, external automation or crew agent performed this product work.');
            """;
        command.Parameters.AddWithValue("$eventId", eventId); command.Parameters.AddWithValue("$actor", actor);
        command.Parameters.AddWithValue("$recordedDate", recordedDate); command.Parameters.AddWithValue("$activity", activity);
        command.ExecuteNonQuery();
    }
}

internal sealed record ProjectReadinessBoard(bool Ok, string DatabaseAuthority, int SchemaVersion, string ProductVersion, string CompletionState,
    IReadOnlyList<ProjectReadinessGate> Gates, IReadOnlyList<ProjectReadinessReceipt> Receipts, IReadOnlyList<string> Rules);
internal sealed record ProjectReadinessGate(string Id, string Area, string Capability, string State, string Evidence,
    string NextEvidence, string OwnerBoundary);
internal sealed record ProjectReadinessReceipt(string EventId, string Actor, string RecordedDate, string Activity,
    string EvidenceState, string CrewActivity);
internal sealed record ProjectReadinessGateSeed(string Id, string Area, string Capability, string State, string Evidence,
    string NextEvidence, string OwnerBoundary, int SortOrder);
