using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class CurriculumEvidenceStore
{
    private const string SchemaVersion = "2";
    private const int MaximumSourceBytes = 5 * 1024 * 1024;
    private static readonly HttpClient SourceClient = CreateSourceClient();
    private readonly string _connectionString;

    public CurriculumEvidenceStore(string dataRoot)
    {
        Directory.CreateDirectory(dataRoot);
        var databasePath = Path.Combine(dataRoot, "ma-teacher.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
        Initialize();
    }

    public object GetHealth()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM curriculum_sources;";
        var sourceCount = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        return new
        {
            ok = true,
            product = "MA-Teacher",
            version = "0.1.0",
            database = new
            {
                engine = "SQLite",
                location = "install-root/data/ma-teacher.db",
                schemaVersion = SchemaVersion,
                sourceCount,
            },
        };
    }

    public CurriculumOverview GetOverview()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.id, s.subject, s.title, s.authority, s.jurisdiction, s.stage_scope,
                   s.source_url, s.status, s.published_date, s.last_checked_utc,
                   s.is_statutory, s.scope_note,
                   r.fetched_utc, r.sha256, r.body_bytes
            FROM curriculum_sources s
            LEFT JOIN source_revisions r ON r.id = (
                SELECT candidate.id FROM source_revisions candidate
                WHERE candidate.source_id = s.id
                ORDER BY candidate.id DESC LIMIT 1)
            ORDER BY s.sort_order, s.subject, s.title;
            """;
        using var reader = command.ExecuteReader();
        var sources = new List<CurriculumSource>();
        while (reader.Read())
        {
            sources.Add(new CurriculumSource(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                reader.GetString(6), reader.GetString(7), reader.GetString(8),
                reader.GetString(9), reader.GetInt32(10) == 1, reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetInt64(14)));
        }
        reader.Close();

        var subjectLanes = new List<SubjectLane>();
        using (var laneCommand = connection.CreateCommand())
        {
            laneCommand.CommandText = "SELECT id, subject, stage_scope, source_id, teaching_focus, evidence_state FROM subject_lanes ORDER BY sort_order;";
            using var laneReader = laneCommand.ExecuteReader();
            while (laneReader.Read())
            {
                subjectLanes.Add(new SubjectLane(
                    laneReader.GetString(0), laneReader.GetString(1), laneReader.GetString(2),
                    laneReader.GetString(3), laneReader.GetString(4), laneReader.GetString(5)));
            }
        }

        var gates = new List<ImplementationGate>();
        using (var gateCommand = connection.CreateCommand())
        {
            gateCommand.CommandText = "SELECT id, sequence, title, required_evidence, status FROM implementation_gates ORDER BY sequence;";
            using var gateReader = gateCommand.ExecuteReader();
            while (gateReader.Read())
            {
                gates.Add(new ImplementationGate(
                    gateReader.GetString(0), gateReader.GetInt32(1), gateReader.GetString(2),
                    gateReader.GetString(3), gateReader.GetString(4)));
            }
        }

        return new CurriculumOverview(
            "England",
            "English National Curriculum",
            "REGISTERED_SOURCES_UNPARSED",
            "Source registration is evidence of provenance only. Objectives, lesson plans, assessments, and tutoring remain unimplemented.",
            new[]
            {
                new CurriculumStage("KS1", "5-7", "Years 1-2"),
                new CurriculumStage("KS2", "7-11", "Years 3-6"),
                new CurriculumStage("KS3", "11-14", "Years 7-9"),
                new CurriculumStage("KS4", "14-16", "Years 10-11"),
            },
            sources,
            subjectLanes,
            gates);
    }

    public async Task<SourceRefreshResult> RefreshSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = new List<(string Id, string Url)>();
        using (var connection = OpenConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, source_url FROM curriculum_sources ORDER BY sort_order, id;";
            using var reader = command.ExecuteReader();
            while (reader.Read()) sources.Add((reader.GetString(0), reader.GetString(1)));
        }

        var captured = 0;
        var unchanged = 0;
        var failed = new List<SourceRefreshFailure>();
        foreach (var source in sources)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, source.Url);
                using var response = await SourceClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    failed.Add(new SourceRefreshFailure(source.Id, $"HTTP {(int)response.StatusCode}"));
                    continue;
                }

                var declaredLength = response.Content.Headers.ContentLength;
                if (declaredLength > MaximumSourceBytes)
                {
                    failed.Add(new SourceRefreshFailure(source.Id, "Source exceeds the 5 MB capture boundary."));
                    continue;
                }

                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var body = new MemoryStream();
                var buffer = new byte[64 * 1024];
                while (true)
                {
                    var read = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
                    if (read == 0) break;
                    if (body.Length + read > MaximumSourceBytes)
                    {
                        throw new InvalidDataException("Source exceeds the 5 MB capture boundary.");
                    }
                    body.Write(buffer, 0, read);
                }

                var bytes = body.ToArray();
                var sha256 = Convert.ToHexString(SHA256.HashData(bytes));
                var compressed = Compress(bytes);
                var fetchedUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                var inserted = StoreRevision(
                    source.Id, source.Url, fetchedUtc, sha256, bytes.LongLength, compressed,
                    response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream",
                    response.Headers.ETag?.ToString(), response.Content.Headers.LastModified?.ToString("O", CultureInfo.InvariantCulture));
                if (inserted) captured++; else unchanged++;
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
            {
                failed.Add(new SourceRefreshFailure(source.Id, BoundMessage(exception.Message)));
            }
        }

        return new SourceRefreshResult(failed.Count == 0, captured, unchanged, failed.Count, failed);
    }

    public IReadOnlyList<SourceRevision> GetRevisionIndex()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.id, r.source_id, s.subject, r.fetched_utc, r.source_url,
                   r.content_type, r.sha256, r.body_bytes
            FROM source_revisions r
            INNER JOIN curriculum_sources s ON s.id = r.source_id
            ORDER BY r.id DESC;
            """;
        using var reader = command.ExecuteReader();
        var revisions = new List<SourceRevision>();
        while (reader.Read())
        {
            revisions.Add(new SourceRevision(
                reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetInt64(7)));
        }
        return revisions;
    }

    public CapturedSourceBody? GetRevisionBody(long revisionId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT content_type, sha256, body_bytes, body_gzip FROM source_revisions WHERE id = $id;";
        command.Parameters.AddWithValue("$id", revisionId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var expectedBytes = reader.GetInt64(2);
        if (expectedBytes < 0 || expectedBytes > MaximumSourceBytes) throw new InvalidDataException("Stored source length is outside the capture boundary.");
        var storedSha256 = reader.GetString(1);
        var compressed = (byte[])reader.GetValue(3);
        var body = Decompress(compressed, expectedBytes);
        var actualSha256 = Convert.ToHexString(SHA256.HashData(body));
        if (!actualSha256.Equals(storedSha256, StringComparison.OrdinalIgnoreCase)) return null;
        return new CapturedSourceBody(reader.GetString(0), storedSha256, body);
    }

    public IReadOnlyList<DevelopmentEvent> GetDevelopmentEvents()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, occurred_utc, actor, activity, response, evidence_state FROM development_events ORDER BY occurred_utc DESC, id DESC LIMIT 200;";
        using var reader = command.ExecuteReader();
        var events = new List<DevelopmentEvent>();
        while (reader.Read())
        {
            events.Add(new DevelopmentEvent(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        }
        return events;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var schema = connection.CreateCommand();
        schema.Transaction = transaction;
        schema.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_versions (
                version TEXT PRIMARY KEY,
                applied_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS curriculum_sources (
                id TEXT PRIMARY KEY,
                subject TEXT NOT NULL,
                title TEXT NOT NULL,
                authority TEXT NOT NULL,
                jurisdiction TEXT NOT NULL,
                stage_scope TEXT NOT NULL,
                source_url TEXT NOT NULL UNIQUE,
                status TEXT NOT NULL,
                published_date TEXT NOT NULL,
                last_checked_utc TEXT NOT NULL,
                is_statutory INTEGER NOT NULL CHECK (is_statutory IN (0, 1)),
                scope_note TEXT NOT NULL,
                sort_order INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS development_events (
                id TEXT PRIMARY KEY,
                occurred_utc TEXT NOT NULL,
                actor TEXT NOT NULL,
                activity TEXT NOT NULL,
                response TEXT NOT NULL,
                evidence_state TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS source_revisions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                source_id TEXT NOT NULL REFERENCES curriculum_sources(id) ON DELETE CASCADE,
                fetched_utc TEXT NOT NULL,
                source_url TEXT NOT NULL,
                http_status INTEGER NOT NULL,
                content_type TEXT NOT NULL,
                etag TEXT,
                last_modified TEXT,
                sha256 TEXT NOT NULL,
                body_bytes INTEGER NOT NULL,
                body_gzip BLOB NOT NULL,
                UNIQUE(source_id, sha256)
            );
            CREATE TABLE IF NOT EXISTS subject_lanes (
                id TEXT PRIMARY KEY,
                subject TEXT NOT NULL,
                stage_scope TEXT NOT NULL,
                source_id TEXT NOT NULL REFERENCES curriculum_sources(id),
                teaching_focus TEXT NOT NULL,
                evidence_state TEXT NOT NULL,
                sort_order INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS implementation_gates (
                id TEXT PRIMARY KEY,
                sequence INTEGER NOT NULL UNIQUE,
                title TEXT NOT NULL,
                required_evidence TEXT NOT NULL,
                status TEXT NOT NULL
            );
            INSERT OR IGNORE INTO schema_versions(version, applied_utc)
            VALUES ('1', '2026-08-30T00:00:00Z');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-evidence-spine',
                '2026-08-30T00:00:00Z',
                'Codex solo',
                'Added the install-root curriculum evidence database, official-source registry, read-only APIs, and provenance console.',
                'No external assistant or crew agent was used. Official GOV.UK source metadata was researched directly; compilation and runtime remain unverified until the next approved build.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO schema_versions(version, applied_utc)
            VALUES ('2', '2026-08-30T00:00:00Z');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-source-capture',
                '2026-08-30T00:00:01Z',
                'Codex solo',
                'Added bounded operator-triggered capture of official curriculum pages with SHA-256 revision identity and compressed database retention.',
                'Capture is never automatic and does not parse or promote source content. No external assistant or crew response exists for this slice.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-teaching-workspace-contract',
                '2026-08-30T00:00:02Z',
                'Codex solo',
                'Added the relational teaching-workspace contract for learner profiles, study plans, source-linked curriculum statements, evidence-linked lessons, assessments, teaching principles, and explicit capability gates.',
                'No external assistant, external automation, or crew agent participated. The schema and read-only overview API are source-complete but compilation, startup, persistence, and UI integration remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-learning-workspace-api',
                '2026-08-30T00:00:03Z',
                'Codex solo',
                'Added guarded same-origin learner and study-plan creation, bounded validation, caller-owned idempotency, conflict refusal, workspace reads, and transactional local activity evidence.',
                'No external assistant, external automation, or crew agent participated. Create and replay behavior is implemented in source; editing, archival, lesson authoring, compilation, startup, persistence, and UI behavior remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-learning-workspace-ui',
                '2026-08-30T00:00:04Z',
                'Codex solo',
                'Added the responsive local learning workspace for learner continuity, study-plan creation, active-plan evidence, teaching principles, and capability-gate visibility.',
                'No external assistant, external automation, or crew agent participated. The interface calls the guarded local APIs and contains no lesson generator, assessment engine, or tutor. Type checking, rendering, runtime persistence, and accessibility remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-curriculum-candidate-review',
                '2026-08-30T00:00:05Z',
                'Codex solo',
                'Added bounded deterministic extraction of curriculum text candidates from captured HTML revisions, immutable source locators and hashes, duplicate suppression, and explicit local accept or reject receipts.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. Extracted text remains unreviewed by default; parser coverage, candidate quality, API behavior, persistence, and UI review remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-curriculum-review-ui',
                '2026-08-30T00:00:06Z',
                'Codex solo',
                'Added the curriculum candidate queue, captured-revision selector, bounded scan action, provenance display, and explicit accept or reject controls to the local learning workspace.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. The UI exposes source state without upgrading it; type checking, rendering, interaction, accessibility, and review persistence remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-official-document-capture',
                '2026-08-30T00:00:07Z',
                'Codex solo',
                'Added bounded same-origin discovery of linked official PDF, ODT, and DOCX curriculum documents plus explicit per-document capture, SHA-256 revision identity, compression, and duplicate suppression.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. Discovery is allowlisted and non-recursive; document parsing, extraction quality, network behavior, persistence, and UI controls remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-document-redirect-hardening',
                '2026-08-30T00:00:08Z',
                'Codex solo',
                'Corrected the initial document transport before verification by disabling automatic redirects and validating every redirect target against the HTTPS official-host allowlist with a five-hop ceiling.',
                'The initial source would have validated only the final URL after HttpClient followed redirects. No build or network request had been run, so no unapproved host was contacted by MA-Teacher. No external assistant, external automation, or crew agent participated.',
                'SOURCE_CORRECTED_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-official-document-ui',
                '2026-08-30T00:00:09Z',
                'Codex solo',
                'Added linked-document discovery and explicit capture controls, document state, source revision, official link, latest hash, and byte evidence to the local curriculum workspace.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. The interface keeps discovery, capture, and parsing separate; type checking, rendering, network behavior, persistence, and accessibility remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-odt-docx-text-blocks',
                '2026-08-30T00:00:10Z',
                'Codex solo',
                'Added bounded ODT and DOCX ZIP/XML extraction into immutable unreviewed text blocks with document revision, parser identity, locator, ordinal, SHA-256, and parse receipts.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. PDF remains explicitly unsupported. Parser compilation, representative documents, block quality, persistence, and UI behavior remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-revision-integrity-check',
                '2026-08-30T00:00:11Z',
                'Codex solo',
                'Hardened captured source and document reads so decompressed bytes must reproduce the stored SHA-256 before discovery, extraction, or parsing can proceed.',
                'No corrupted revision was observed because runtime verification has not begun. This is preventative source hardening; no external assistant, external automation, or crew agent participated.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-document-parser-ui',
                '2026-08-30T00:00:12Z',
                'Codex solo',
                'Added ODT/DOCX extraction controls, explicit PDF lock state, and bounded unreviewed text-block provenance to the local curriculum workspace.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. Type checking, rendering, parse actions, block quality, persistence, and accessibility remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-document-media-normalization',
                '2026-08-30T00:00:13Z',
                'Codex solo',
                'Corrected the document capture-to-parser handoff so an allowed application/octet-stream response is stored under the already allowlisted PDF, ODT, or DOCX extension media type.',
                'The original source accepted generic binary media but would later refuse ODT/DOCX parsing. No build or network request had run before correction. Host and extension boundaries remain unchanged; no external assistant, external automation, or crew agent participated.',
                'SOURCE_CORRECTED_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-pdfpig-page-text',
                '2026-08-30T00:00:14Z',
                'Codex solo',
                'Selected and pinned managed PdfPig 0.1.16 under Apache-2.0 for strict page-addressed PDF text extraction, PDF signature and 2,000-page bounds, immutable page/offset locators, hashes, and unreviewed block receipts.',
                'Primary-source research used the official PdfPig repository, license, parser API, and NuGet package record. No external assistant, external automation, model, browser agent, or crew agent participated. Restore, compilation, representative PDFs, malformed input, extraction quality, memory behavior, and packaged license notices remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-document-block-candidates',
                '2026-08-30T00:00:15Z',
                'Codex solo',
                'Joined PDF, ODT, and DOCX text blocks to the existing curriculum candidate queue with deterministic phrase segmentation, source-page revision, document revision, block, locator, and hash evidence.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. Every result remains unreviewed and uses the same accept or reject lane; segmentation coverage, duplicates, representative source quality, compilation, persistence, and UI behavior remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-curriculum-revision-drift',
                '2026-08-30T00:00:16Z',
                'Codex solo',
                'Added immutable source/document revision comparisons, extraction-set fingerprints, added/removed item evidence, operator dispositions, and a local reconciliation interface without automatic curriculum mutation.',
                'No external assistant, external automation, model, browser agent, or crew agent participated. Comparisons require one owner and stored chronological order; no-impact is refused when extraction coverage is unproven. Compilation, persistence, repeat comparison, delta quality, and UI behavior remain unverified.',
                'SOURCE_COMPLETE_BUILD_UNVERIFIED');
            INSERT OR IGNORE INTO development_events(
                id, occurred_utc, actor, activity, response, evidence_state)
            VALUES (
                'ma-teacher-0.1.0-drift-identity-hardening',
                '2026-08-30T00:00:17Z',
                'Codex solo',
                'Corrected the initial drift design before verification so comparison identity includes extracted-item fingerprints, route parsing is explicit, revision chronology is proven from capture time, and no-impact cannot bypass missing extraction coverage.',
                'No comparison had run because the application has not been built or launched. The correction prevents improved extraction over unchanged bytes from reusing stale evidence. No external assistant, external automation, or crew agent participated.',
                'SOURCE_CORRECTED_BUILD_UNVERIFIED');
            """;
        schema.ExecuteNonQuery();

        foreach (var source in SeedSources)
        {
            using var seed = connection.CreateCommand();
            seed.Transaction = transaction;
            seed.CommandText = """
                INSERT INTO curriculum_sources(
                    id, subject, title, authority, jurisdiction, stage_scope,
                    source_url, status, published_date, last_checked_utc,
                    is_statutory, scope_note, sort_order)
                VALUES ($id, $subject, $title, 'Department for Education', 'England', $stageScope,
                    $sourceUrl, 'registered-unparsed', $publishedDate, '2026-08-30T00:00:00Z',
                    1, $scopeNote, $sortOrder)
                ON CONFLICT(id) DO UPDATE SET
                    subject = excluded.subject, title = excluded.title,
                    authority = excluded.authority, jurisdiction = excluded.jurisdiction,
                    stage_scope = excluded.stage_scope, source_url = excluded.source_url,
                    status = excluded.status, published_date = excluded.published_date,
                    last_checked_utc = excluded.last_checked_utc,
                    is_statutory = excluded.is_statutory,
                    scope_note = excluded.scope_note, sort_order = excluded.sort_order;
                """;
            seed.Parameters.AddWithValue("$id", source.Id);
            seed.Parameters.AddWithValue("$subject", source.Subject);
            seed.Parameters.AddWithValue("$title", source.Title);
            seed.Parameters.AddWithValue("$stageScope", source.StageScope);
            seed.Parameters.AddWithValue("$sourceUrl", source.SourceUrl);
            seed.Parameters.AddWithValue("$publishedDate", source.PublishedDate);
            seed.Parameters.AddWithValue("$scopeNote", source.ScopeNote);
            seed.Parameters.AddWithValue("$sortOrder", source.SortOrder);
            seed.ExecuteNonQuery();
        }

        using var handoff = connection.CreateCommand();
        handoff.Transaction = transaction;
        handoff.CommandText = """
            INSERT OR REPLACE INTO subject_lanes(id, subject, stage_scope, source_id, teaching_focus, evidence_state, sort_order) VALUES
                ('english', 'English', 'KS1-KS4', 'english-programmes', 'Spoken language, reading, writing, vocabulary, and communication across subjects.', 'INTERPRETIVE_SUMMARY_AWAITING_OBJECTIVE_PARSE', 10),
                ('maths', 'Maths', 'KS1-KS4', 'mathematics-programmes', 'Mathematical fluency, reasoning, problem solving, numeracy, and sense checking.', 'INTERPRETIVE_SUMMARY_AWAITING_OBJECTIVE_PARSE', 20),
                ('science', 'Science', 'KS1-KS4', 'science-programmes', 'Scientific knowledge, conceptual understanding, enquiry, observation, and evidence.', 'INTERPRETIVE_SUMMARY_AWAITING_OBJECTIVE_PARSE', 30),
                ('history', 'History', 'KS1-KS3', 'history-programmes', 'Chronology, historical enquiry, evidence, causation, change, and interpretations.', 'INTERPRETIVE_SUMMARY_AWAITING_OBJECTIVE_PARSE', 40),
                ('languages', 'Languages', 'KS2-KS3', 'languages-programmes', 'Listening, speaking, reading, writing, phonology, vocabulary, and grammar.', 'INTERPRETIVE_SUMMARY_AWAITING_OBJECTIVE_PARSE', 50),
                ('computing', 'Information technology and computing', 'KS1-KS4', 'computing-programmes', 'Computer science, information technology, digital literacy, creativity, and safe use.', 'INTERPRETIVE_SUMMARY_AWAITING_OBJECTIVE_PARSE', 60);
            INSERT OR REPLACE INTO implementation_gates(id, sequence, title, required_evidence, status) VALUES
                ('capture', 10, 'Capture current official sources', 'Successful bounded HTTP capture, SHA-256 identity, stored revision, and repeat-capture deduplication.', 'IMPLEMENTED_UNVERIFIED'),
                ('documents', 15, 'Discover and capture linked statutory documents', 'Allowlisted link discovery, explicit bounded capture, media validation, revision identity, and repeat-capture deduplication.', 'IMPLEMENTED_UNVERIFIED'),
                ('document-text', 17, 'Extract ODT and DOCX text blocks', 'Representative ODT/DOCX fixtures prove bounded ZIP/XML extraction, locator stability, hash identity, duplicate suppression, and malformed-input refusal.', 'IMPLEMENTED_UNVERIFIED'),
                ('pdf-text', 18, 'Extract PDF page text', 'Pinned PdfPig restore, license payload, compilation, representative statutory fixtures, page locators, bounded resource behavior, scanned-page state, and malformed-PDF refusal.', 'IMPLEMENTED_UNVERIFIED'),
                ('document-candidates', 19, 'Route document text into curriculum review', 'Representative PDF/ODT/DOCX blocks prove candidate segmentation, duplicate evidence links, locator integrity, review isolation, and repeat-run idempotency.', 'IMPLEMENTED_UNVERIFIED'),
                ('parse', 20, 'Parse statutory objectives', 'Parser distinguishes statutory text, non-statutory guidance, examples, headings, stage, and subject with source offsets.', 'NOT_IMPLEMENTED'),
                ('reconcile', 30, 'Reconcile revisions', 'Representative source/document revisions prove chronological comparison, extraction fingerprints, added/removed evidence, repeat identity, disposition refusal, and no automatic accepted-row mutation.', 'IMPLEMENTED_UNVERIFIED'),
                ('roles', 40, 'Establish adult and learner roles', 'Explicit local authority, consent, retention, deletion, and shared-device boundaries.', 'NOT_IMPLEMENTED'),
                ('lesson', 50, 'Prove one lesson workflow', 'Source-grounded plan, age-appropriate explanation, practice, feedback, misconception handling, and adult review.', 'NOT_IMPLEMENTED'),
                ('tutor', 60, 'Enable generated tutoring', 'Retrieval, citation, uncertainty, safety, evaluation, and rollback evidence meet the accepted lesson contract.', 'LOCKED');
            """;
        handoff.ExecuteNonQuery();

        transaction.Commit();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragmas = connection.CreateCommand();
        pragmas.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        pragmas.ExecuteNonQuery();
        return connection;
    }

    private bool StoreRevision(string sourceId, string sourceUrl, string fetchedUtc, string sha256, long bodyBytes, byte[] bodyGzip, string contentType, string? etag, string? lastModified)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT OR IGNORE INTO source_revisions(
                source_id, fetched_utc, source_url, http_status, content_type,
                etag, last_modified, sha256, body_bytes, body_gzip)
            VALUES ($sourceId, $fetchedUtc, $sourceUrl, 200, $contentType,
                $etag, $lastModified, $sha256, $bodyBytes, $bodyGzip);
            """;
        insert.Parameters.AddWithValue("$sourceId", sourceId);
        insert.Parameters.AddWithValue("$fetchedUtc", fetchedUtc);
        insert.Parameters.AddWithValue("$sourceUrl", sourceUrl);
        insert.Parameters.AddWithValue("$contentType", contentType);
        insert.Parameters.AddWithValue("$etag", (object?)etag ?? DBNull.Value);
        insert.Parameters.AddWithValue("$lastModified", (object?)lastModified ?? DBNull.Value);
        insert.Parameters.AddWithValue("$sha256", sha256);
        insert.Parameters.AddWithValue("$bodyBytes", bodyBytes);
        insert.Parameters.AddWithValue("$bodyGzip", bodyGzip);
        var inserted = insert.ExecuteNonQuery() == 1;
        if (inserted)
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE curriculum_sources
                SET status = 'captured-unparsed', last_checked_utc = $fetchedUtc
                WHERE id = $sourceId;
                """;
            update.Parameters.AddWithValue("$fetchedUtc", fetchedUtc);
            update.Parameters.AddWithValue("$sourceId", sourceId);
            update.ExecuteNonQuery();
        }
        transaction.Commit();
        return inserted;
    }

    private static byte[] Compress(byte[] source)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) gzip.Write(source);
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] source, long expectedBytes)
    {
        using var input = new MemoryStream(source, writable: false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream((int)expectedBytes);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = gzip.Read(buffer, 0, buffer.Length);
            if (read == 0) break;
            if (output.Length + read > MaximumSourceBytes) throw new InvalidDataException("Stored source expands beyond the capture boundary.");
            output.Write(buffer, 0, read);
        }
        if (output.Length != expectedBytes) throw new InvalidDataException("Stored source length does not match its revision receipt.");
        return output.ToArray();
    }

    private static HttpClient CreateSourceClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MA-Teacher/0.1.0 curriculum-evidence-capture");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
        return client;
    }

    private static string BoundMessage(string value)
    {
        var normalized = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= 180 ? normalized : normalized[..180];
    }

    private static bool IsIdentifier(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength) return false;
        foreach (var character in value)
        {
            if (!(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '.' or '_')) return false;
        }
        return true;
    }

    private static bool IsBoundedText(string? value, int minimumLength, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var length = value.Trim().Length;
        return length >= minimumLength && length <= maximumLength;
    }

    private static bool IsEvidenceState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80) return false;
        foreach (var character in value)
        {
            if (!(character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')) return false;
        }
        return true;
    }

    private static readonly SeedSource[] SeedSources =
    {
        new("england-framework", "All subjects", "National curriculum in England: framework for key stages 1 to 4", "KS1-KS4", "https://www.gov.uk/government/publications/national-curriculum-in-england-framework-for-key-stages-1-to-4", "2013-09-11", "Statutory framework and subject programme index; last framework update recorded by GOV.UK as 2014-12-02.", 0),
        new("english-programmes", "English", "National curriculum in England: English programmes of study", "KS1-KS4", "https://www.gov.uk/government/publications/national-curriculum-in-england-english-programmes-of-study", "2013-09-11", "Statutory programmes of study and attainment targets for English.", 10),
        new("mathematics-programmes", "Maths", "National curriculum in England: mathematics programmes of study", "KS1-KS4", "https://www.gov.uk/government/publications/national-curriculum-in-england-mathematics-programmes-of-study", "2013-09-11", "Statutory programmes of study and attainment targets for mathematics.", 20),
        new("science-programmes", "Science", "National curriculum in England: science programmes of study", "KS1-KS4", "https://www.gov.uk/government/publications/national-curriculum-in-england-science-programmes-of-study", "2013-09-11", "Statutory programmes of study and attainment targets for science.", 30),
        new("history-programmes", "History", "National curriculum in England: history programmes of study", "KS1-KS3", "https://www.gov.uk/government/publications/national-curriculum-in-england-history-programmes-of-study", "2013-09-11", "Statutory programmes of study and attainment targets for history at key stages 1 to 3.", 40),
        new("languages-programmes", "Languages", "National curriculum in England: languages programmes of study", "KS2-KS3", "https://www.gov.uk/government/publications/national-curriculum-in-england-languages-progammes-of-study", "2013-09-11", "Statutory foreign-language programme at KS2 and modern-foreign-language programme at KS3.", 50),
        new("computing-programmes", "Information technology and computing", "National curriculum in England: computing programmes of study", "KS1-KS4", "https://www.gov.uk/government/publications/national-curriculum-in-england-computing-programmes-of-study", "2013-09-11", "Statutory computing programmes spanning computer science, information technology, and digital literacy.", 60),
    };

    private sealed record SeedSource(string Id, string Subject, string Title, string StageScope, string SourceUrl, string PublishedDate, string ScopeNote, int SortOrder);
}

internal sealed record CurriculumOverview(string Jurisdiction, string Curriculum, string Status, string Boundary, IReadOnlyList<CurriculumStage> Stages, IReadOnlyList<CurriculumSource> Sources, IReadOnlyList<SubjectLane> SubjectLanes, IReadOnlyList<ImplementationGate> ImplementationGates);
internal sealed record CurriculumStage(string Id, string Ages, string Years);
internal sealed record CurriculumSource(string Id, string Subject, string Title, string Authority, string Jurisdiction, string StageScope, string SourceUrl, string Status, string PublishedDate, string LastCheckedUtc, bool IsStatutory, string ScopeNote, string? LatestFetchedUtc, string? LatestSha256, long? LatestBodyBytes);
internal sealed record DevelopmentEvent(string Id, string OccurredUtc, string Actor, string Activity, string Response, string EvidenceState);
internal sealed record SubjectLane(string Id, string Subject, string StageScope, string SourceId, string TeachingFocus, string EvidenceState);
internal sealed record ImplementationGate(string Id, int Sequence, string Title, string RequiredEvidence, string Status);
internal sealed record SourceRefreshFailure(string SourceId, string Reason);
internal sealed record SourceRefreshResult(bool Ok, int Captured, int Unchanged, int Failed, IReadOnlyList<SourceRefreshFailure> Failures);
internal sealed record SourceRevision(long Id, string SourceId, string Subject, string FetchedUtc, string SourceUrl, string ContentType, string Sha256, long BodyBytes);
internal sealed record CapturedSourceBody(string ContentType, string Sha256, byte[] Body);
