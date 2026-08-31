using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class DatabaseBackupStore
{
    private const int SchemaVersion = 1;
    private const int MaximumBackups = 50;
    private static readonly Regex IdentifierPattern = new("^[a-z0-9][a-z0-9_-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly string _connectionString;
    private readonly string _databasePath;
    private readonly string _backupRoot;
    private readonly object _mutationLock = new();

    public DatabaseBackupStore(string moduleRoot)
    {
        var dataRoot = Path.Combine(moduleRoot, "data"); Directory.CreateDirectory(dataRoot);
        _databasePath = Path.Combine(dataRoot, "ma-teacher.db");
        _backupRoot = Path.Combine(dataRoot, "backups"); Directory.CreateDirectory(_backupRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared, ForeignKeys = true,
        }.ToString();
        Initialize();
    }

    public DatabaseBackupOverview GetOverview()
    {
        using var connection = OpenSource(); var backups = new List<DatabaseBackupRecord>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, created_utc, file_name, bytes, sha256, state, last_verified_utc, verification_state, error
            FROM database_backup_receipts ORDER BY created_utc DESC, id;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var fileName = reader.GetString(2); var path = ResolveBackupPath(fileName); var present = File.Exists(path);
            var observedBytes = present ? new FileInfo(path).Length : 0;
            backups.Add(new(reader.GetString(0), reader.GetString(1), fileName, reader.GetInt64(3), reader.GetString(4),
                reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8), present, observedBytes,
                !present ? "missing" : observedBytes == reader.GetInt64(3) ? "present-size-matches" : "present-size-mismatch"));
        }
        var receiptFiles = backups.Select(value => value.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var observedFiles = Directory.EnumerateFiles(_backupRoot, "*.db", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path)!).OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumBackups + 1).ToArray();
        var orphanFiles = observedFiles.Take(MaximumBackups).Where(value => !receiptFiles.Contains(value)).ToArray();
        var orphanScanTruncated = observedFiles.Length > MaximumBackups;
        return new DatabaseBackupOverview(true, "install-root-sqlite", SchemaVersion, _backupRoot, MaximumBackups,
            "sqlite-online-backup-api", false, "sha256-byte-identity-only",
            backups, backups.Count(value => value.FilePresent), Math.Min(observedFiles.Length, MaximumBackups),
            observedFiles.Length >= MaximumBackups, orphanFiles, orphanScanTruncated, new[]
            {
                "Backups remain beneath the selected install root.",
                "Creation is manual, serialized and idempotent by caller-provided backup id.",
                "The application never automatically deletes a backup to make room.",
                "Size presence is a display hint; only explicit SHA-256 verification proves recorded byte identity.",
                "Restore, import, cloud sync and external-drive copying are not implemented.",
            });
    }

    public DatabaseBackupMutation CreateBackup(DatabaseBackupInput input)
    {
        var id = NormalizeId(input.Id);
        if (id is null) return new(false, "invalid", null, null, null, "Backup id must be 3-64 lowercase letters, numbers, hyphens or underscores.");
        lock (_mutationLock)
        {
            using var source = OpenSource();
            using (var existing = source.CreateCommand())
            {
                existing.CommandText = "SELECT state, file_name, sha256 FROM database_backup_receipts WHERE id=$id;";
                existing.Parameters.AddWithValue("$id", id); using var reader = existing.ExecuteReader();
                if (reader.Read()) return reader.GetString(0) == "created"
                    ? new(true, "already-present", id, reader.GetString(1), reader.GetString(2), null)
                    : new(false, "conflict", id, reader.GetString(1), null, "Backup id already owns a failed or incomplete receipt; use a new id.");
            }
            if (Directory.EnumerateFiles(_backupRoot, "*.db", SearchOption.TopDirectoryOnly).Take(MaximumBackups).Count() >= MaximumBackups)
                return new(false, "capacity-refused", id, null, null, $"Backup inventory reached {MaximumBackups}; archive or remove files manually before retrying.");
            var now = DateTimeOffset.UtcNow; var fileName = $"ma-teacher-{now:yyyyMMdd-HHmmss}-{id}.db"; var path = ResolveBackupPath(fileName);
            if (File.Exists(path)) return new(false, "conflict", id, fileName, null, "Generated backup path already exists.");
            try
            {
                var destinationString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
                using (var destination = new SqliteConnection(destinationString)) { destination.Open(); source.BackupDatabase(destination); }
                var info = new FileInfo(path); if (info.Length < 4096) throw new InvalidDataException("Backup is unexpectedly small.");
                var sha256 = HashFile(path);
                using var transaction = source.BeginTransaction(); using var receipt = source.CreateCommand(); receipt.Transaction = transaction;
                receipt.CommandText = """
                    INSERT INTO database_backup_receipts(id, created_utc, file_name, bytes, sha256, state, verification_state)
                    VALUES ($id, $createdUtc, $fileName, $bytes, $sha256, 'created', 'recorded-hash-not-reverified');
                    """;
                receipt.Parameters.AddWithValue("$id", id); receipt.Parameters.AddWithValue("$createdUtc", now.ToString("O"));
                receipt.Parameters.AddWithValue("$fileName", fileName); receipt.Parameters.AddWithValue("$bytes", info.Length);
                receipt.Parameters.AddWithValue("$sha256", sha256); receipt.ExecuteNonQuery(); transaction.Commit();
                return new(true, "created-recorded-hash-not-reverified", id, fileName, sha256, null);
            }
            catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException or CryptographicException or InvalidDataException)
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                var error = $"{exception.GetType().Name}: {exception.Message}"; if (error.Length > 500) error = error[..500];
                using var receipt = source.CreateCommand(); receipt.CommandText = """
                    INSERT INTO database_backup_receipts(id, created_utc, file_name, bytes, sha256, state, verification_state, error)
                    VALUES ($id, $createdUtc, $fileName, 0, '', 'failed', 'not-verified', $error);
                    """;
                receipt.Parameters.AddWithValue("$id", id); receipt.Parameters.AddWithValue("$createdUtc", now.ToString("O"));
                receipt.Parameters.AddWithValue("$fileName", fileName); receipt.Parameters.AddWithValue("$error", error); receipt.ExecuteNonQuery();
                return new(false, "failed", id, fileName, null, error);
            }
        }
    }

    public DatabaseBackupMutation VerifyBackup(DatabaseBackupVerifyInput input)
    {
        var id = NormalizeId(input.Id); if (id is null) return new(false, "invalid", null, null, null, "A valid backup id is required.");
        lock (_mutationLock)
        {
            using var connection = OpenSource(); string fileName; long expectedBytes; string expectedHash;
            using (var lookup = connection.CreateCommand())
            {
                lookup.CommandText = "SELECT file_name, bytes, sha256, state FROM database_backup_receipts WHERE id=$id;";
                lookup.Parameters.AddWithValue("$id", id); using var reader = lookup.ExecuteReader();
                if (!reader.Read() || reader.GetString(3) != "created") return new(false, "invalid", id, null, null, "Created backup receipt does not exist.");
                fileName = reader.GetString(0); expectedBytes = reader.GetInt64(1); expectedHash = reader.GetString(2);
            }
            var path = ResolveBackupPath(fileName); var now = DateTimeOffset.UtcNow.ToString("O");
            var state = "verified"; string? error = null; string? observedHash = null;
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Backup file is missing.");
                var bytes = new FileInfo(path).Length; observedHash = HashFile(path);
                if (bytes != expectedBytes || !observedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                { state = "verification-failed"; error = "Observed backup size or SHA-256 differs from the creation receipt."; }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
            { state = "verification-failed"; error = $"{exception.GetType().Name}: {exception.Message}"; }
            using var update = connection.CreateCommand(); update.CommandText = """
                UPDATE database_backup_receipts SET last_verified_utc=$now, verification_state=$state, error=$error WHERE id=$id;
                """;
            update.Parameters.AddWithValue("$now", now); update.Parameters.AddWithValue("$state", state);
            update.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value); update.Parameters.AddWithValue("$id", id); update.ExecuteNonQuery();
            return new(state == "verified", state, id, fileName, observedHash, error);
        }
    }

    private void Initialize()
    {
        using var connection = OpenSource(); using var command = connection.CreateCommand(); command.CommandText = """
            CREATE TABLE IF NOT EXISTS database_backup_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS database_backup_receipts (
                id TEXT PRIMARY KEY, created_utc TEXT NOT NULL, file_name TEXT NOT NULL, bytes INTEGER NOT NULL,
                sha256 TEXT NOT NULL, state TEXT NOT NULL, last_verified_utc TEXT NULL,
                verification_state TEXT NOT NULL, error TEXT NULL
            );
            INSERT INTO database_backup_meta(key, value) VALUES ('schema_version', $schemaVersion)
            ON CONFLICT(key) DO UPDATE SET value=excluded.value;
            """; command.Parameters.AddWithValue("$schemaVersion", SchemaVersion.ToString()); command.ExecuteNonQuery();
    }

    private SqliteConnection OpenSource()
    { var connection = new SqliteConnection(_connectionString); connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; command.ExecuteNonQuery(); return connection; }
    private string ResolveBackupPath(string fileName) { var safe = Path.GetFileName(fileName); var path = Path.GetFullPath(Path.Combine(_backupRoot, safe)); if (!path.StartsWith(Path.GetFullPath(_backupRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Backup path escaped its root."); return path; }
    private static string HashFile(string path) { using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant(); }
    private static string? NormalizeId(string? value) { var id = (value ?? "").Trim().ToLowerInvariant(); return IdentifierPattern.IsMatch(id) ? id : null; }
}

internal sealed record DatabaseBackupOverview(bool Ok, string DatabaseAuthority, int SchemaVersion, string BackupRoot,
    int MaximumBackups, string SnapshotMethod, bool ReceiptIncludedInSnapshot, string VerificationScope,
    IReadOnlyList<DatabaseBackupRecord> Backups, int PresentFiles, int ObservedDatabaseFiles, bool CapacityReached,
    IReadOnlyList<string> OrphanFiles, bool OrphanScanTruncated, IReadOnlyList<string> Boundaries);
internal sealed record DatabaseBackupRecord(string Id, string CreatedUtc, string FileName, long Bytes, string Sha256,
    string State, string? LastVerifiedUtc, string VerificationState, string? Error, bool FilePresent, long ObservedBytes, string PresenceState);
internal sealed record DatabaseBackupInput(string Id);
internal sealed record DatabaseBackupVerifyInput(string Id);
internal sealed record DatabaseBackupMutation(bool Ok, string State, string? Id, string? FileName, string? Sha256, string? Error);
