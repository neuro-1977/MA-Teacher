using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class DataStewardshipStore
{
    private const int SchemaVersion = 1;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly InventorySpec[] InventorySpecs =
    [
        new("learner-profiles", "Learner profiles", "learner_profiles", "created_utc", "Local identity and accessibility/preference context."),
        new("study-plans", "Study plans", "study_plans", "created_utc", "Learner-linked subject, stage and goal records."),
        new("workspace-attempts", "Workspace attempts", "assessment_attempts", "created_utc", "Earlier learner response evidence in the teaching workspace."),
        new("practice-attempts", "Practice attempts", "learning_check_attempts", "submitted_utc", "Learner-owned responses and bounded human review evidence."),
        new("teaching-sessions", "Teaching-session receipts", "teaching_session_receipts", "recorded_utc", "Claimed delivery evidence bound to approved lesson fingerprints."),
        new("database-backups", "Database backup receipts", "database_backup_receipts", "created_utc", "Snapshot inventory evidence; each file may contain every database category."),
    ];
    private readonly string _connectionString;

    public DataStewardshipStore(string moduleRoot)
    {
        var dataRoot = Path.GetFullPath(moduleRoot); Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public DataStewardshipOverview GetOverview()
    {
        using var connection = OpenConnection();
        var inventory = InventorySpecs.Select(spec => ReadInventory(connection, spec)).ToArray();
        var policies = new List<DataRetentionPolicy>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, recorded_by, next_review_date, learner_record_days, attempt_record_days,
                       session_receipt_days, backup_days, rationale, deletion_authority, recorded_utc
                FROM data_retention_policy_records ORDER BY sequence, id;
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read()) policies.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6), reader.GetString(7),
                reader.GetString(8), reader.GetString(9)));
        }
        return new(true, "install-root-sqlite", SchemaVersion, inventory, policies.LastOrDefault(), policies,
        [
            "Inventory counts and timestamps describe current database rows only; they do not identify legal obligations or prove backup-file contents.",
            "Retention windows are operator-authored planning targets. MA-Teacher does not automatically delete, archive, export or redact records.",
            "A backup can preserve data after a live row is later removed; backup review is a separate explicit responsibility.",
            "Recorded-by identity is a claim, not authentication, authorization or legal approval.",
            "Deletion, withdrawal, restore, export and legal-compliance workflows remain unavailable and unproven.",
        ]);
    }

    public DataStewardshipMutation RecordPolicy(DataRetentionPolicyInput input)
    {
        try
        {
            if (!input.AcknowledgesNoAutomaticDeletion) throw new ArgumentException("Confirm that recording a policy does not delete, archive or export any data.");
            var id = RequireId(input.PolicyId, "policy id"); var actor = RequireText(input.RecordedBy, "recorded by", 2, 120);
            var reviewDate = RequireDate(input.NextReviewDate); var learnerDays = RequireDays(input.LearnerRecordDays, "learner record days");
            var attemptDays = RequireDays(input.AttemptRecordDays, "attempt record days");
            var sessionDays = RequireDays(input.SessionReceiptDays, "session receipt days"); var backupDays = RequireDays(input.BackupDays, "backup days");
            var rationale = RequireText(input.Rationale, "rationale", 20, 4000); const string deletionAuthority = "manual-reviewed-only-not-implemented";
            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT recorded_by, next_review_date, learner_record_days, attempt_record_days,
                           session_receipt_days, backup_days, rationale, deletion_authority
                    FROM data_retention_policy_records WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id); var found = false; var same = false;
                using (var reader = existing.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        found = true; same = reader.GetString(0) == actor && reader.GetString(1) == reviewDate
                            && reader.GetInt32(2) == learnerDays && reader.GetInt32(3) == attemptDays
                            && reader.GetInt32(4) == sessionDays && reader.GetInt32(5) == backupDays
                            && reader.GetString(6) == rationale && reader.GetString(7) == deletionAuthority;
                    }
                }
                if (found)
                {
                    transaction.Rollback();
                    return same ? new(true, "already-present", id, null)
                        : new(false, "conflict", id, "Policy id already exists with different stewardship evidence.");
                }
            }
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO data_retention_policy_records(id, recorded_by, next_review_date, learner_record_days,
                        attempt_record_days, session_receipt_days, backup_days, rationale, deletion_authority, recorded_utc)
                    VALUES ($id, $actor, $reviewDate, $learnerDays, $attemptDays, $sessionDays, $backupDays,
                        $rationale, $deletionAuthority, $recordedUtc);
                    """;
                insert.Parameters.AddWithValue("$id", id); insert.Parameters.AddWithValue("$actor", actor);
                insert.Parameters.AddWithValue("$reviewDate", reviewDate); insert.Parameters.AddWithValue("$learnerDays", learnerDays);
                insert.Parameters.AddWithValue("$attemptDays", attemptDays); insert.Parameters.AddWithValue("$sessionDays", sessionDays);
                insert.Parameters.AddWithValue("$backupDays", backupDays); insert.Parameters.AddWithValue("$rationale", rationale);
                insert.Parameters.AddWithValue("$deletionAuthority", deletionAuthority);
                insert.Parameters.AddWithValue("$recordedUtc", DateTimeOffset.UtcNow.ToString("O")); insert.ExecuteNonQuery();
            }
            transaction.Commit(); return new(true, "policy-recorded-no-automatic-action", id, null);
        }
        catch (ArgumentException exception) { return new(false, "invalid", null, exception.Message); }
    }

    private void Initialize()
    {
        using var connection = OpenConnection(); using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS data_stewardship_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS data_retention_policy_records (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE,
                recorded_by TEXT NOT NULL, next_review_date TEXT NOT NULL,
                learner_record_days INTEGER NOT NULL, attempt_record_days INTEGER NOT NULL,
                session_receipt_days INTEGER NOT NULL, backup_days INTEGER NOT NULL,
                rationale TEXT NOT NULL, deletion_authority TEXT NOT NULL, recorded_utc TEXT NOT NULL
            );
            INSERT INTO data_stewardship_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            """;
        command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString(CultureInfo.InvariantCulture)); command.ExecuteNonQuery();
    }

    private static DataInventoryRecord ReadInventory(SqliteConnection connection, InventorySpec spec)
    {
        using (var exists = connection.CreateCommand())
        {
            exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";
            exists.Parameters.AddWithValue("$name", spec.TableName);
            if (Convert.ToInt32(exists.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                return new(spec.Id, spec.Label, spec.Purpose, false, 0, null, null);
        }
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*), MIN({spec.TimestampColumn}), MAX({spec.TimestampColumn}) FROM {spec.TableName};";
        using var reader = command.ExecuteReader(); reader.Read();
        return new(spec.Id, spec.Label, spec.Purpose, true, reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private SqliteConnection OpenConnection()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }
    private static string RequireId(string? value, string field) { var normalized = (value ?? "").Trim().ToLowerInvariant(); if (!IdentifierPattern.IsMatch(normalized)) throw new ArgumentException($"{field} must be 3-64 lowercase letters, numbers, hyphens or underscores."); return normalized; }
    private static string RequireText(string? value, string field, int minimum, int maximum) { var normalized = (value ?? "").Trim(); if (normalized.Length < minimum || normalized.Length > maximum) throw new ArgumentException($"{field} must be {minimum}-{maximum} characters."); return normalized; }
    private static string RequireDate(string? value) { var normalized = (value ?? "").Trim(); if (!DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new ArgumentException("next review date must use YYYY-MM-DD."); return normalized; }
    private static int RequireDays(int value, string field) { if (value is < 1 or > 3650) throw new ArgumentException($"{field} must be 1-3650."); return value; }
    private sealed record InventorySpec(string Id, string Label, string TableName, string TimestampColumn, string Purpose);
}

internal sealed record DataStewardshipOverview(bool Ok, string DatabaseAuthority, int SchemaVersion,
    IReadOnlyList<DataInventoryRecord> Inventory, DataRetentionPolicy? CurrentPolicy,
    IReadOnlyList<DataRetentionPolicy> Policies, IReadOnlyList<string> Boundaries);
internal sealed record DataInventoryRecord(string Id, string Label, string Purpose, bool TablePresent, int RecordCount, string? OldestUtc, string? NewestUtc);
internal sealed record DataRetentionPolicy(string Id, string RecordedBy, string NextReviewDate, int LearnerRecordDays,
    int AttemptRecordDays, int SessionReceiptDays, int BackupDays, string Rationale, string DeletionAuthority, string RecordedUtc);
internal sealed record DataRetentionPolicyInput(string PolicyId, string RecordedBy, string NextReviewDate, int LearnerRecordDays,
    int AttemptRecordDays, int SessionReceiptDays, int BackupDays, string Rationale, bool AcknowledgesNoAutomaticDeletion);
internal sealed record DataStewardshipMutation(bool Ok, string State, string? Id, string? Error);
