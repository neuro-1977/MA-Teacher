using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

internal sealed record DevelopmentBreadcrumbRecord(
    string Id,
    string RecordedUtc,
    string Actor,
    string Workstream,
    string Behavior,
    string Reference,
    string EvidenceState,
    string EvidenceDetail,
    string VerificationState,
    string VerificationDetail,
    string CrewActivity,
    string CrewResponse,
    bool ExternalAssistantUsed,
    bool ExternalAutomationUsed,
    string ContentSha256,
    bool IntegrityValid);

internal sealed record DevelopmentBreadcrumbWrite(
    string Id,
    string RecordedUtc,
    string Actor,
    string Workstream,
    string Behavior,
    string Reference,
    string EvidenceState,
    string EvidenceDetail,
    string VerificationState,
    string VerificationDetail,
    string CrewActivity,
    string CrewResponse,
    bool ExternalAssistantUsed,
    bool ExternalAutomationUsed);

internal sealed record DevelopmentBreadcrumbIntegrityAudit(
    int Total,
    int Valid,
    int MissingIntegrity,
    int MismatchedIntegrity,
    IReadOnlyList<string> IssueIds,
    bool IssuesTruncated);

internal sealed record DevelopmentBreadcrumbContext(
    int RequestedLimit,
    int ReturnedCount,
    bool CompleteHistory,
    bool IntegrityClean,
    string? FirstBreadcrumbId,
    string? LastBreadcrumbId,
    DevelopmentBreadcrumbIntegrityAudit Integrity,
    IReadOnlyList<DevelopmentBreadcrumbRecord> Records);

internal sealed record DevelopmentBreadcrumbCursor(string RecordedUtc, string Id);

internal sealed record DevelopmentBreadcrumbPage(
    int RequestedLimit,
    int ReturnedCount,
    bool HasMore,
    DevelopmentBreadcrumbCursor? NextOlderCursor,
    IReadOnlyList<DevelopmentBreadcrumbRecord> Records);

internal sealed record DevelopmentBreadcrumbMutation(
    bool Ok,
    string State,
    bool Inserted,
    DevelopmentBreadcrumbRecord? Record,
    string? Error);

internal static class DevelopmentBreadcrumbStore
{
    private sealed record Breadcrumb(string Id, string Behavior, string Reference);
    private static readonly string[] EvidenceStates = ["source-present", "not-run", "observed", "human-reviewed", "accepted", "failed", "unsupported", "not-applicable"];
    private static readonly string[] VerificationStates = ["not-run", "observed", "accepted", "failed", "not-applicable"];

    private static readonly Breadcrumb[] CurrentSoloContinuation =
    [
        new("ma-teacher-010-public-release", "Serenity built the standalone MA-Teacher 0.1.0 public preview with engineering assistance from OpenAI Codex, including local learner records, evidence-linked lesson planning, work submission, human review, backups, public documentation, installed-payload self-testing and automated Windows installer publication.", "README.md; docs/INSTALLER.md; docs/TEACHER_GUIDE.md; docs/STUDENT_GUIDE.md; docs/DEVELOPMENT.md"),
    ];

    internal static void InsertCurrentSoloContinuation(SqliteConnection connection, SqliteTransaction transaction)
    {
        using (var create = connection.CreateCommand())
        {
            create.Transaction = transaction;
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS development_breadcrumbs (
                    breadcrumb_id TEXT PRIMARY KEY,
                    recorded_utc TEXT NOT NULL,
                    actor TEXT NOT NULL CHECK (length(trim(actor)) BETWEEN 1 AND 120),
                    workstream TEXT NOT NULL CHECK (length(trim(workstream)) BETWEEN 1 AND 200),
                    behavior TEXT NOT NULL CHECK (length(trim(behavior)) BETWEEN 1 AND 2000),
                    reference TEXT NOT NULL CHECK (length(trim(reference)) BETWEEN 1 AND 2000),
                    evidence_state TEXT NOT NULL CHECK (evidence_state IN ('source-present', 'not-run', 'observed', 'human-reviewed', 'accepted', 'failed', 'unsupported', 'not-applicable')),
                    evidence_detail TEXT NOT NULL CHECK (length(trim(evidence_detail)) BETWEEN 1 AND 4000),
                    verification_state TEXT NOT NULL CHECK (verification_state IN ('not-run', 'observed', 'accepted', 'failed', 'not-applicable')),
                    verification_detail TEXT NOT NULL CHECK (length(trim(verification_detail)) BETWEEN 1 AND 4000),
                    crew_activity TEXT NOT NULL CHECK (length(trim(crew_activity)) BETWEEN 1 AND 1000),
                    crew_response TEXT NOT NULL CHECK (length(trim(crew_response)) BETWEEN 1 AND 2000),
                    external_assistant_used INTEGER NOT NULL CHECK (external_assistant_used IN (0, 1)),
                    external_automation_used INTEGER NOT NULL CHECK (external_automation_used IN (0, 1))
                );

                CREATE INDEX IF NOT EXISTS ix_development_breadcrumbs_recorded
                    ON development_breadcrumbs(recorded_utc, breadcrumb_id);

                CREATE TABLE IF NOT EXISTS development_breadcrumb_integrity (
                    breadcrumb_id TEXT PRIMARY KEY,
                    content_sha256 TEXT NOT NULL CHECK (length(content_sha256) = 64),
                    FOREIGN KEY (breadcrumb_id) REFERENCES development_breadcrumbs(breadcrumb_id) ON DELETE RESTRICT
                );
                """;
            create.ExecuteNonQuery();
        }

        foreach (var breadcrumb in CurrentSoloContinuation)
        {
            Insert(connection, transaction, new DevelopmentBreadcrumbWrite(
                breadcrumb.Id,
                "2026-08-30T00:00:00Z",
                "Serenity with OpenAI Codex assistance",
                "MA-Teacher public development",
                breadcrumb.Behavior,
                breadcrumb.Reference,
                "source-present",
                "The public source and product documentation identify the exact implemented boundaries.",
                "accepted",
                "The public release gate passed source-boundary scanning, dependency audit, typecheck, production build, packaged self-test, silent install, installed self-test and silent uninstall.",
                "Serenity led the product build; OpenAI Codex provided engineering and release assistance.",
                "The verified 0.1.0 release remains human-reviewed, local-first and explicit about unproven curriculum and safeguarding boundaries.",
                true,
                false));
        }
    }

    internal static bool Insert(SqliteConnection connection, SqliteTransaction transaction, DevelopmentBreadcrumbWrite write)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(write);
        var canonicalWrite = NormalizeWrite(write);
        ValidateWrite(canonicalWrite);

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT OR IGNORE INTO development_breadcrumbs (
                breadcrumb_id, recorded_utc, actor, workstream, behavior, reference,
                evidence_state, evidence_detail, verification_state, verification_detail,
                crew_activity, crew_response, external_assistant_used, external_automation_used
            ) VALUES (
                $id, $recorded, $actor, $workstream, $behavior, $reference,
                $evidenceState, $evidenceDetail, $verificationState, $verificationDetail,
                $crewActivity, $crewResponse, $externalAssistantUsed, $externalAutomationUsed
            );
            """;
        insert.Parameters.AddWithValue("$id", canonicalWrite.Id);
        insert.Parameters.AddWithValue("$recorded", canonicalWrite.RecordedUtc);
        insert.Parameters.AddWithValue("$actor", canonicalWrite.Actor);
        insert.Parameters.AddWithValue("$workstream", canonicalWrite.Workstream);
        insert.Parameters.AddWithValue("$behavior", canonicalWrite.Behavior);
        insert.Parameters.AddWithValue("$reference", canonicalWrite.Reference);
        insert.Parameters.AddWithValue("$evidenceState", canonicalWrite.EvidenceState);
        insert.Parameters.AddWithValue("$evidenceDetail", canonicalWrite.EvidenceDetail);
        insert.Parameters.AddWithValue("$verificationState", canonicalWrite.VerificationState);
        insert.Parameters.AddWithValue("$verificationDetail", canonicalWrite.VerificationDetail);
        insert.Parameters.AddWithValue("$crewActivity", canonicalWrite.CrewActivity);
        insert.Parameters.AddWithValue("$crewResponse", canonicalWrite.CrewResponse);
        insert.Parameters.AddWithValue("$externalAssistantUsed", canonicalWrite.ExternalAssistantUsed ? 1 : 0);
        insert.Parameters.AddWithValue("$externalAutomationUsed", canonicalWrite.ExternalAutomationUsed ? 1 : 0);
        var inserted = insert.ExecuteNonQuery() == 1;
        if (!inserted)
        {
            var existing = ReadWriteById(connection, transaction, canonicalWrite.Id)
                ?? throw new InvalidDataException($"Breadcrumb {canonicalWrite.Id} was not inserted and could not be read back.");
            if (!string.Equals(ComputeContentHash(existing), ComputeContentHash(canonicalWrite), StringComparison.Ordinal))
                throw new InvalidDataException($"Breadcrumb {canonicalWrite.Id} already exists with different immutable content.");
        }
        EnsureIntegrityForId(connection, transaction, canonicalWrite.Id);
        return inserted;
    }

    internal static IReadOnlyList<DevelopmentBreadcrumbRecord> ReadAll(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT b.breadcrumb_id, b.recorded_utc, b.actor, b.workstream, b.behavior, b.reference,
                   b.evidence_state, b.evidence_detail, b.verification_state, b.verification_detail,
                   b.crew_activity, b.crew_response, b.external_assistant_used, b.external_automation_used,
                   COALESCE(i.content_sha256, '')
              FROM development_breadcrumbs b
              LEFT JOIN development_breadcrumb_integrity i ON i.breadcrumb_id = b.breadcrumb_id
             ORDER BY b.recorded_utc ASC, b.breadcrumb_id ASC;
            """;

        using var reader = command.ExecuteReader();
        var records = new List<DevelopmentBreadcrumbRecord>();
        while (reader.Read())
        {
            records.Add(ReadRecord(reader));
        }

        return records;
    }

    internal static DevelopmentBreadcrumbRecord? ReadById(SqliteConnection connection, string id, SqliteTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ValidateIdentifier(id);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT b.breadcrumb_id, b.recorded_utc, b.actor, b.workstream, b.behavior, b.reference,
                   b.evidence_state, b.evidence_detail, b.verification_state, b.verification_detail,
                   b.crew_activity, b.crew_response, b.external_assistant_used, b.external_automation_used,
                   COALESCE(i.content_sha256, '')
              FROM development_breadcrumbs b
              LEFT JOIN development_breadcrumb_integrity i ON i.breadcrumb_id = b.breadcrumb_id
             WHERE b.breadcrumb_id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRecord(reader) : null;
    }

    internal static IReadOnlyList<DevelopmentBreadcrumbRecord> ReadRecent(SqliteConnection connection, int requestedLimit = 200, SqliteTransaction? transaction = null)
    {
        var limit = Math.Clamp(requestedLimit, 1, 1000);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT recent.breadcrumb_id, recent.recorded_utc, recent.actor, recent.workstream, recent.behavior, recent.reference,
                   recent.evidence_state, recent.evidence_detail, recent.verification_state, recent.verification_detail,
                   recent.crew_activity, recent.crew_response, recent.external_assistant_used, recent.external_automation_used,
                   COALESCE(i.content_sha256, '')
              FROM (
                    SELECT b.breadcrumb_id, b.recorded_utc, b.actor, b.workstream, b.behavior, b.reference,
                           b.evidence_state, b.evidence_detail, b.verification_state, b.verification_detail,
                           b.crew_activity, b.crew_response, b.external_assistant_used, b.external_automation_used
                      FROM development_breadcrumbs b
                     ORDER BY b.recorded_utc DESC, b.breadcrumb_id DESC
                     LIMIT $limit
                   ) recent
              LEFT JOIN development_breadcrumb_integrity i ON i.breadcrumb_id = recent.breadcrumb_id
             ORDER BY recent.recorded_utc ASC, recent.breadcrumb_id ASC;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var records = new List<DevelopmentBreadcrumbRecord>();
        while (reader.Read()) records.Add(ReadRecord(reader));
        return records;
    }

    internal static DevelopmentBreadcrumbIntegrityAudit AuditIntegrity(SqliteConnection connection, int requestedIssueLimit = 20, SqliteTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        var issueLimit = Math.Clamp(requestedIssueLimit, 1, 100);
        var records = ReadAll(connection, transaction);
        var valid = 0;
        var missing = 0;
        var mismatched = 0;
        var issueIds = new List<string>();

        foreach (var record in records)
        {
            if (record.IntegrityValid)
            {
                valid++;
                continue;
            }

            if (string.IsNullOrEmpty(record.ContentSha256)) missing++;
            else mismatched++;
            if (issueIds.Count < issueLimit) issueIds.Add(record.Id);
        }

        return new DevelopmentBreadcrumbIntegrityAudit(
            records.Count,
            valid,
            missing,
            mismatched,
            issueIds,
            missing + mismatched > issueIds.Count);
    }

    internal static DevelopmentBreadcrumbContext ReadContext(SqliteConnection connection, SqliteTransaction transaction, int requestedRecordLimit = 200, int requestedIssueLimit = 20)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        var limit = Math.Clamp(requestedRecordLimit, 1, 1000);
        var records = ReadRecent(connection, limit, transaction);
        var integrity = AuditIntegrity(connection, requestedIssueLimit, transaction);
        return new DevelopmentBreadcrumbContext(
            limit,
            records.Count,
            records.Count == integrity.Total,
            integrity.Total == integrity.Valid && integrity.MissingIntegrity == 0 && integrity.MismatchedIntegrity == 0,
            records.Count == 0 ? null : records[0].Id,
            records.Count == 0 ? null : records[^1].Id,
            integrity,
            records);
    }

    internal static DevelopmentBreadcrumbPage ReadPageBefore(SqliteConnection connection, SqliteTransaction transaction, DevelopmentBreadcrumbCursor? before = null, int requestedLimit = 200)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        if (before is not null)
        {
            ValidateUtc(before.RecordedUtc, nameof(before.RecordedUtc));
            ValidateIdentifier(before.Id);
        }

        var limit = Math.Clamp(requestedLimit, 1, 1000);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT b.breadcrumb_id, b.recorded_utc, b.actor, b.workstream, b.behavior, b.reference,
                   b.evidence_state, b.evidence_detail, b.verification_state, b.verification_detail,
                   b.crew_activity, b.crew_response, b.external_assistant_used, b.external_automation_used,
                   COALESCE(i.content_sha256, '')
              FROM development_breadcrumbs b
              LEFT JOIN development_breadcrumb_integrity i ON i.breadcrumb_id = b.breadcrumb_id
             WHERE $hasCursor = 0
                OR b.recorded_utc < $beforeUtc
                OR (b.recorded_utc = $beforeUtc AND b.breadcrumb_id < $beforeId)
             ORDER BY b.recorded_utc DESC, b.breadcrumb_id DESC
             LIMIT $take;
            """;
        command.Parameters.AddWithValue("$hasCursor", before is null ? 0 : 1);
        command.Parameters.AddWithValue("$beforeUtc", before?.RecordedUtc ?? string.Empty);
        command.Parameters.AddWithValue("$beforeId", before?.Id ?? string.Empty);
        command.Parameters.AddWithValue("$take", limit + 1);

        using var reader = command.ExecuteReader();
        var descending = new List<DevelopmentBreadcrumbRecord>();
        while (reader.Read()) descending.Add(ReadRecord(reader));

        var hasMore = descending.Count > limit;
        if (hasMore) descending.RemoveAt(descending.Count - 1);
        var nextOlder = hasMore && descending.Count > 0
            ? new DevelopmentBreadcrumbCursor(descending[^1].RecordedUtc, descending[^1].Id)
            : null;
        descending.Reverse();
        return new DevelopmentBreadcrumbPage(limit, descending.Count, hasMore, nextOlder, descending);
    }

    private static DevelopmentBreadcrumbRecord ReadRecord(SqliteDataReader reader)
    {
        var write = ReadWrite(reader);
        var storedHash = reader.GetString(14);
        return new DevelopmentBreadcrumbRecord(
            write.Id,
            write.RecordedUtc,
            write.Actor,
            write.Workstream,
            write.Behavior,
            write.Reference,
            write.EvidenceState,
            write.EvidenceDetail,
            write.VerificationState,
            write.VerificationDetail,
            write.CrewActivity,
            write.CrewResponse,
            write.ExternalAssistantUsed,
            write.ExternalAutomationUsed,
            storedHash,
            storedHash.Length == 64 && string.Equals(storedHash, ComputeContentHash(write), StringComparison.Ordinal));
    }

    private static DevelopmentBreadcrumbWrite ReadWrite(SqliteDataReader reader)
        => new(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            reader.GetString(10),
            reader.GetString(11),
            reader.GetInt64(12) == 1,
            reader.GetInt64(13) == 1);

    private static DevelopmentBreadcrumbWrite? ReadWriteById(SqliteConnection connection, SqliteTransaction transaction, string id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT breadcrumb_id, recorded_utc, actor, workstream, behavior, reference,
                   evidence_state, evidence_detail, verification_state, verification_detail,
                   crew_activity, crew_response, external_assistant_used, external_automation_used
              FROM development_breadcrumbs
             WHERE breadcrumb_id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadWrite(reader) : null;
    }

    private static void EnsureIntegrityForId(SqliteConnection connection, SqliteTransaction transaction, string id)
    {
        DevelopmentBreadcrumbWrite existing;
        using (var read = connection.CreateCommand())
        {
            read.Transaction = transaction;
            read.CommandText = """
                SELECT breadcrumb_id, recorded_utc, actor, workstream, behavior, reference,
                       evidence_state, evidence_detail, verification_state, verification_detail,
                       crew_activity, crew_response, external_assistant_used, external_automation_used
                  FROM development_breadcrumbs
                 WHERE breadcrumb_id = $id;
                """;
            read.Parameters.AddWithValue("$id", id);
            using var reader = read.ExecuteReader();
            if (!reader.Read()) throw new InvalidOperationException($"Breadcrumb {id} was not available for integrity calculation.");
            existing = ReadWrite(reader);
        }

        var computed = ComputeContentHash(existing);
        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT OR IGNORE INTO development_breadcrumb_integrity (breadcrumb_id, content_sha256) VALUES ($id, $hash);";
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$hash", computed);
            insert.ExecuteNonQuery();
        }

        using var verify = connection.CreateCommand();
        verify.Transaction = transaction;
        verify.CommandText = "SELECT content_sha256 FROM development_breadcrumb_integrity WHERE breadcrumb_id = $id;";
        verify.Parameters.AddWithValue("$id", id);
        var stored = Convert.ToString(verify.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (!string.Equals(stored, computed, StringComparison.Ordinal))
            throw new InvalidDataException($"Breadcrumb integrity mismatch for {id}; existing integrity evidence was not overwritten.");
    }

    private static string ComputeContentHash(DevelopmentBreadcrumbWrite write)
    {
        var canonical = new StringBuilder();
        AppendCanonical(canonical, write.Id);
        AppendCanonical(canonical, write.RecordedUtc);
        AppendCanonical(canonical, write.Actor);
        AppendCanonical(canonical, write.Workstream);
        AppendCanonical(canonical, write.Behavior);
        AppendCanonical(canonical, write.Reference);
        AppendCanonical(canonical, write.EvidenceState);
        AppendCanonical(canonical, write.EvidenceDetail);
        AppendCanonical(canonical, write.VerificationState);
        AppendCanonical(canonical, write.VerificationDetail);
        AppendCanonical(canonical, write.CrewActivity);
        AppendCanonical(canonical, write.CrewResponse);
        AppendCanonical(canonical, write.ExternalAssistantUsed ? "1" : "0");
        AppendCanonical(canonical, write.ExternalAutomationUsed ? "1" : "0");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static void AppendCanonical(StringBuilder target, string value)
    {
        target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        target.Append(':');
        target.Append(value);
        target.Append('\n');
    }

    private static void ValidateWrite(DevelopmentBreadcrumbWrite write)
    {
        ValidateIdentifier(write.Id);
        ValidateUtc(write.RecordedUtc, nameof(write.RecordedUtc));

        ValidateBounded(write.Actor, 120, nameof(write.Actor));
        ValidateBounded(write.Workstream, 200, nameof(write.Workstream));
        ValidateBounded(write.Behavior, 2000, nameof(write.Behavior));
        ValidateBounded(write.Reference, 2000, nameof(write.Reference));
        ValidateChoice(write.EvidenceState, EvidenceStates, nameof(write.EvidenceState));
        ValidateBounded(write.EvidenceDetail, 4000, nameof(write.EvidenceDetail));
        ValidateChoice(write.VerificationState, VerificationStates, nameof(write.VerificationState));
        ValidateBounded(write.VerificationDetail, 4000, nameof(write.VerificationDetail));
        ValidateBounded(write.CrewActivity, 1000, nameof(write.CrewActivity));
        ValidateBounded(write.CrewResponse, 2000, nameof(write.CrewResponse));
    }

    private static DevelopmentBreadcrumbWrite NormalizeWrite(DevelopmentBreadcrumbWrite write)
        => write with
        {
            Actor = write.Actor?.Trim() ?? string.Empty,
            Workstream = write.Workstream?.Trim() ?? string.Empty,
            Behavior = write.Behavior?.Trim() ?? string.Empty,
            Reference = write.Reference?.Trim() ?? string.Empty,
            EvidenceDetail = write.EvidenceDetail?.Trim() ?? string.Empty,
            VerificationDetail = write.VerificationDetail?.Trim() ?? string.Empty,
            CrewActivity = write.CrewActivity?.Trim() ?? string.Empty,
            CrewResponse = write.CrewResponse?.Trim() ?? string.Empty,
        };

    private static void ValidateIdentifier(string value)
    {
        ValidateBounded(value, 160, "Id");
        foreach (var character in value)
        {
            if (!(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.'))
                throw new ArgumentException("Breadcrumb id may contain only lowercase ASCII letters, digits, dash, underscore and dot.", "Id");
        }
    }

    private static void ValidateUtc(string value, string field)
    {
        if (!DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var recorded) || recorded.Offset != TimeSpan.Zero)
            throw new ArgumentException($"{field} must use UTC yyyy-MM-ddTHH:mm:ssZ format.", field);
    }

    private static void ValidateBounded(string value, int maximum, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximum)
            throw new ArgumentException($"{field} must contain 1 to {maximum} non-whitespace characters.", field);
    }

    private static void ValidateChoice(string value, string[] allowed, string field)
    {
        if (!allowed.Contains(value, StringComparer.Ordinal))
            throw new ArgumentException($"{field} is not a recognized canonical state.", field);
    }
}
