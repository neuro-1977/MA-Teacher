using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class CurriculumCoverageStore
{
    private const int SchemaVersion = 1;
    private const string SnapshotDate = "2026-08-30";
    private readonly string _connectionString;

    public CurriculumCoverageStore(string moduleRoot)
    {
        var dataRoot = Path.Combine(moduleRoot, "data");
        Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public CurriculumCoverageOverview GetOverview()
    {
        using var connection = OpenConnection();
        var lanes = new List<CurriculumCoverageLane>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, jurisdiction, age_scope, stage_model, subject_scope, source_title, source_url,
                       source_kind, coverage_state, evidence, gap, next_action, editorial_snapshot_date
                FROM curriculum_coverage_lanes ORDER BY sort_order, id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                lanes.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                    reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetString(12)));
        }
        var receipts = new List<CurriculumCoverageReceipt>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT event_id, actor, occurred_utc, activity, evidence_state, crew_activity FROM curriculum_coverage_events ORDER BY occurred_utc, event_id;";
            using var reader = command.ExecuteReader();
            while (reader.Read()) receipts.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }
        return new CurriculumCoverageOverview(true, "install-root-sqlite", SchemaVersion, SnapshotDate, "partial-england-ks1-ks4-only", lanes, receipts,
            new[]
            {
                "Jurisdictions and stage models must remain separate.",
                "A reference-identified lane is not configured source capture.",
                "Post-16 and adult provision cannot be inferred from the KS1-KS4 national curriculum.",
                "Qualification content, provider requirements and general teaching guidance are different authorities.",
                "No lane is complete until source capture, review, drift handling and representative runtime evidence exist.",
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
                CREATE TABLE IF NOT EXISTS curriculum_coverage_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS curriculum_coverage_lanes (
                    id TEXT PRIMARY KEY, jurisdiction TEXT NOT NULL, age_scope TEXT NOT NULL, stage_model TEXT NOT NULL,
                    subject_scope TEXT NOT NULL, source_title TEXT NOT NULL, source_url TEXT NOT NULL, source_kind TEXT NOT NULL,
                    coverage_state TEXT NOT NULL, evidence TEXT NOT NULL, gap TEXT NOT NULL, next_action TEXT NOT NULL,
                    editorial_snapshot_date TEXT NOT NULL, sort_order INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS curriculum_coverage_events (
                    event_id TEXT PRIMARY KEY, actor TEXT NOT NULL, occurred_utc TEXT NOT NULL, activity TEXT NOT NULL,
                    evidence_state TEXT NOT NULL, crew_activity TEXT NOT NULL
                );
                INSERT INTO curriculum_coverage_meta(key, value) VALUES ('schema_version', $schemaVersion)
                ON CONFLICT(key) DO UPDATE SET value=excluded.value;
                """;
            command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString());
            command.ExecuteNonQuery();
        }

        Upsert(connection, transaction, new("england-eyfs", "England", "birth to 5", "EYFS provider-specific frameworks",
            "seven areas of learning plus safeguarding, welfare and assessment requirements",
            "Early years foundation stage statutory framework",
            "https://www.gov.uk/government/publications/early-years-foundation-stage-framework--2", "statutory-framework-landing-page",
            "reference-identified-capture-not-configured",
            "The official landing page identifies separate childminder and group/school frameworks, including versions applying from September 2026.",
            "No provider type, effective-date selection, safeguarding boundary, capture path or extraction review exists.",
            "Design effective-date and provider-type authority before adding EYFS source capture.", SnapshotDate, 10));
        Upsert(connection, transaction, new("england-ks1-ks4", "England", "5 to 16", "Key Stages 1 to 4",
            "framework plus English, mathematics, science, history, languages and computing seeded",
            "National curriculum in England: framework for key stages 1 to 4",
            "https://www.gov.uk/government/publications/national-curriculum-in-england-framework-for-key-stages-1-to-4/the-national-curriculum-in-england-framework-for-key-stages-1-to-4",
            "statutory-curriculum-framework", "partial-capture-configured-source-unverified",
            "The source registry and capture pipeline cover the framework and six Captain-priority subjects in source.",
            "Art and design, citizenship, design and technology, geography, music, physical education, RE, RSE/health and subject-specific KS4 qualification content are not covered.",
            "Build and exercise current capture first, then add missing subjects through the same reviewed source contract.", SnapshotDate, 20));
        Upsert(connection, transaction, new("england-post16", "England", "16 to 19", "study programmes, A levels, T Levels and vocational pathways",
            "programme and qualification dependent",
            "Advice: funding rules for 16 to 19 provision 2026 to 2027",
            "https://www.gov.uk/government/publications/advice-funding-rules-for-16-to-19-provision/advice-funding-rules-for-16-to-19-provision-2026-to-2027",
            "provider-programme-guidance", "reference-only-no-curriculum-model",
            "The current guidance establishes programme and qualification boundaries but is not itself subject curriculum content.",
            "No qualification catalogue, awarding-body specification, T Level occupational source or effective-year model exists.",
            "Model qualification authority and academic year before capturing post-16 subject content.", SnapshotDate, 30));
        Upsert(connection, transaction, new("england-functional-english", "England", "post-16 and adult entry level to level 2",
            "Functional Skills Entry Levels 1-3 and Levels 1-2", "English speaking/listening/communication, reading and writing",
            "English Functional Skills: subject content",
            "https://www.gov.uk/government/publications/functional-skills-subject-content-english/english-functional-skills-subject-content",
            "qualification-subject-content", "reference-identified-capture-not-configured",
            "The official content defines progressive English outcomes and inclusive communication scope.",
            "No Functional Skills stage mapping, source capture, assessment authority or awarding-body specification model exists.",
            "Add Functional Skills as a separate qualification lane, never as a Key Stage alias.", SnapshotDate, 40));
        Upsert(connection, transaction, new("england-functional-maths", "England", "post-16 and adult entry level to level 2",
            "Functional Skills Entry Levels 1-3 and Levels 1-2", "number, measures/shape/space, information/data and problem solving",
            "Maths Functional Skills: subject content",
            "https://www.gov.uk/government/publications/functional-skills-subject-content-mathematics/subject-content-functional-skills-maths",
            "qualification-subject-content", "reference-identified-capture-not-configured",
            "The official content defines level-specific knowledge, skills and application expectations.",
            "No Functional Skills stage mapping, source capture, assessment authority or qualification version model exists.",
            "Add versioned qualification content and reviewed candidate extraction independently of school maths.", SnapshotDate, 50));
        Upsert(connection, transaction, new("england-functional-digital", "England", "post-16 and adult entry and level 1",
            "Digital Functional Skills qualification levels", "devices/information, creating/editing, communication, transactions and online safety",
            "Digital Functional Skills: subject content",
            "https://www.gov.uk/government/publications/digital-functional-skills-qualifications/digital-functional-skills-qualifications-subject-content",
            "qualification-subject-content", "reference-identified-capture-not-configured",
            "The official content covers applied digital activity for life and work.",
            "This is not equivalent to the KS1-KS4 computing curriculum; no practical environment or assessment model exists.",
            "Create a separate functional-digital lane with safe practical-task evidence boundaries.", SnapshotDate, 60));
        Upsert(connection, transaction, new("scotland-cfe", "Scotland", "3 to 18", "Curriculum for Excellence early, first, second, third/fourth and senior phases",
            "eight curriculum areas with experiences, outcomes and separate senior-phase qualifications",
            "Building the Curriculum", "https://education.gov.scot/curriculum-for-excellence/curriculum-for-excellence-documents/building-the-curriculum/",
            "national-curriculum-guidance", "jurisdiction-recognised-not-supported",
            "Education Scotland publishes a distinct 3-18 framework and assessment guidance.",
            "England key stages and source locators cannot be reused; no Scottish source authority is implemented.",
            "Design jurisdiction-scoped stage, subject, qualification and source models before capture.", SnapshotDate, 70));
        Upsert(connection, transaction, new("wales-cfw", "Wales", "3 to 16 and 14 to 16 progression", "Curriculum for Wales progression model",
            "six areas of learning and experience plus cross-curricular skills",
            "Curriculum for Wales", "https://hwb.gov.wales/curriculum-for-wales", "national-curriculum-guidance",
            "jurisdiction-recognised-not-supported",
            "Hwb publishes a distinct purposes, progression, curriculum-design and assessment model.",
            "England key-stage labels and subject taxonomy cannot be imposed; transition from the 2008 curriculum must be respected.",
            "Design jurisdiction and effective-curriculum-version handling before capture.", SnapshotDate, 80));
        Upsert(connection, transaction, new("northern-ireland", "Northern Ireland", "compulsory education P1 to Year 12",
            "Foundation Stage, Key Stages 1-4 with Northern Ireland year mapping",
            "primary and post-primary areas of learning",
            "Statutory curriculum", "https://www.education-ni.gov.uk/articles/statutory-curriculum", "statutory-curriculum-guidance",
            "jurisdiction-recognised-not-supported",
            "The Department of Education publishes a distinct 12-year curriculum and stage/year mapping.",
            "England age, year and subject mappings cannot be reused; no Northern Ireland source authority is implemented.",
            "Design jurisdiction-scoped stages, areas of learning and qualification links before capture.", SnapshotDate, 90));
        Upsert(connection, transaction, new("international-other", "Other jurisdictions", "all", "unknown until explicitly selected",
            "unknown", "No source selected", "", "unsupported", "not-supported",
            "MA-Teacher currently has no authority for curricula outside the named UK lanes.",
            "Language fluency or a model answer cannot establish legal, statutory or qualification authority.",
            "Require explicit jurisdiction, official-source registry, licensing and review policy before support.", SnapshotDate, 100));

        using (var receipt = connection.CreateCommand())
        {
            receipt.Transaction = transaction;
            receipt.CommandText = """
                INSERT OR IGNORE INTO curriculum_coverage_events(event_id, actor, occurred_utc, activity, evidence_state, crew_activity)
                VALUES ('ma-teacher-curriculum-coverage-v1', 'Codex solo', $occurredUtc,
                        'Mapped current England coverage plus EYFS, post-16, Functional Skills and devolved-jurisdiction gaps without claiming support.',
                        'source-implemented-unverified',
                        'None. No external assistant, external automation, crew agent or browser agent performed product work. Official-source web research was executed directly by Codex.');
                """;
            receipt.Parameters.AddWithValue("$occurredUtc", DateTimeOffset.UtcNow.ToString("O")); receipt.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery();
        return connection;
    }

    private static void Upsert(SqliteConnection connection, SqliteTransaction transaction, CurriculumCoverageLaneSeed value)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO curriculum_coverage_lanes(id, jurisdiction, age_scope, stage_model, subject_scope, source_title,
                source_url, source_kind, coverage_state, evidence, gap, next_action, editorial_snapshot_date, sort_order)
            VALUES ($id, $jurisdiction, $ageScope, $stageModel, $subjectScope, $sourceTitle, $sourceUrl, $sourceKind,
                $coverageState, $evidence, $gap, $nextAction, $snapshotDate, $sortOrder)
            ON CONFLICT(id) DO UPDATE SET jurisdiction=excluded.jurisdiction, age_scope=excluded.age_scope,
                stage_model=excluded.stage_model, subject_scope=excluded.subject_scope, source_title=excluded.source_title,
                source_url=excluded.source_url, source_kind=excluded.source_kind, coverage_state=excluded.coverage_state,
                evidence=excluded.evidence, gap=excluded.gap, next_action=excluded.next_action,
                editorial_snapshot_date=excluded.editorial_snapshot_date, sort_order=excluded.sort_order;
            """;
        command.Parameters.AddWithValue("$id", value.Id); command.Parameters.AddWithValue("$jurisdiction", value.Jurisdiction);
        command.Parameters.AddWithValue("$ageScope", value.AgeScope); command.Parameters.AddWithValue("$stageModel", value.StageModel);
        command.Parameters.AddWithValue("$subjectScope", value.SubjectScope); command.Parameters.AddWithValue("$sourceTitle", value.SourceTitle);
        command.Parameters.AddWithValue("$sourceUrl", value.SourceUrl); command.Parameters.AddWithValue("$sourceKind", value.SourceKind);
        command.Parameters.AddWithValue("$coverageState", value.CoverageState); command.Parameters.AddWithValue("$evidence", value.Evidence);
        command.Parameters.AddWithValue("$gap", value.Gap); command.Parameters.AddWithValue("$nextAction", value.NextAction);
        command.Parameters.AddWithValue("$snapshotDate", value.SnapshotDate); command.Parameters.AddWithValue("$sortOrder", value.SortOrder);
        command.ExecuteNonQuery();
    }
}

internal sealed record CurriculumCoverageOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, string EditorialSnapshotDate,
    string OverallState, IReadOnlyList<CurriculumCoverageLane> Lanes, IReadOnlyList<CurriculumCoverageReceipt> Receipts,
    IReadOnlyList<string> Rules);
internal sealed record CurriculumCoverageLane(string Id, string Jurisdiction, string AgeScope, string StageModel, string SubjectScope,
    string SourceTitle, string SourceUrl, string SourceKind, string CoverageState, string Evidence, string Gap, string NextAction,
    string EditorialSnapshotDate);
internal sealed record CurriculumCoverageReceipt(string EventId, string Actor, string OccurredUtc, string Activity,
    string EvidenceState, string CrewActivity);
internal sealed record CurriculumCoverageLaneSeed(string Id, string Jurisdiction, string AgeScope, string StageModel,
    string SubjectScope, string SourceTitle, string SourceUrl, string SourceKind, string CoverageState, string Evidence,
    string Gap, string NextAction, string SnapshotDate, int SortOrder);
