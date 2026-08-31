using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MATeacher.ModuleShell;

internal sealed class CurriculumDriftStore
{
    private readonly string _connectionString;

    public CurriculumDriftStore(string dataRoot)
    {
        Directory.CreateDirectory(dataRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(dataRoot, "ma-teacher.db"),
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
        EnsureSchema();
    }

    public DriftComparisonResult Compare(DriftComparisonInput input)
    {
        var scope = (input.ScopeKind ?? string.Empty).Trim().ToLowerInvariant();
        if (scope is not ("source" or "document"))
            return new DriftComparisonResult(false, null, "invalid", 0, 0, 0, "scopeKind must be source or document.");
        if (input.OlderRevisionId < 1 || input.NewerRevisionId < 1 || input.OlderRevisionId == input.NewerRevisionId)
            return new DriftComparisonResult(false, null, "invalid", 0, 0, 0, "Two different positive revision ids are required.");

        using var connection = OpenConnection();
        var older = ReadRevision(connection, scope, input.OlderRevisionId);
        var newer = ReadRevision(connection, scope, input.NewerRevisionId);
        if (older is null || newer is null)
            return new DriftComparisonResult(false, null, "invalid", 0, 0, 0, "One or both revisions do not exist.");
        if (!string.Equals(older.OwnerId, newer.OwnerId, StringComparison.Ordinal))
            return new DriftComparisonResult(false, null, "invalid", 0, 0, 0, "Revisions must belong to the same source or document.");
        if (!DateTimeOffset.TryParse(older.FetchedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var olderUtc)
            || !DateTimeOffset.TryParse(newer.FetchedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var newerUtc)
            || olderUtc >= newerUtc)
            return new DriftComparisonResult(false, null, "invalid", 0, 0, 0, "Older and newer revisions must follow stored capture time.");

        var olderItems = ReadItems(connection, scope, input.OlderRevisionId);
        var newerItems = ReadItems(connection, scope, input.NewerRevisionId);
        var olderHashes = olderItems.Keys.ToHashSet(StringComparer.Ordinal);
        var newerHashes = newerItems.Keys.ToHashSet(StringComparer.Ordinal);
        var added = newerHashes.Except(olderHashes, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var removed = olderHashes.Except(newerHashes, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var unchanged = olderHashes.Intersect(newerHashes, StringComparer.Ordinal).Count();
        var rawSame = older.Sha256.Equals(newer.Sha256, StringComparison.OrdinalIgnoreCase);
        var olderItemFingerprint = FingerprintItems(olderHashes);
        var newerItemFingerprint = FingerprintItems(newerHashes);
        var itemSetsSame = olderItemFingerprint == newerItemFingerprint;
        var state = rawSame && itemSetsSame ? "same-bytes-items-stable"
            : rawSame ? "same-bytes-extraction-changed-review-required"
            : olderItems.Count == 0 || newerItems.Count == 0 ? "review-required-extraction-coverage-unproven"
            : added.Length == 0 && removed.Length == 0 ? "review-required-bytes-changed-items-stable"
            : "review-required-item-delta";
        var identityText = $"{scope}|{input.OlderRevisionId}|{input.NewerRevisionId}|{older.Sha256}|{newer.Sha256}|{olderItemFingerprint}|{newerItemFingerprint}";
        var identityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityText))).ToLowerInvariant();
        var comparisonId = $"drift-{scope}-{identityHash[..20]}";
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        using var transaction = connection.BeginTransaction();
        using (var comparison = connection.CreateCommand())
        {
            comparison.Transaction = transaction;
            comparison.CommandText = """
                INSERT OR IGNORE INTO curriculum_revision_comparisons(id, scope_kind, owner_id,
                    older_revision_id, newer_revision_id, older_sha256, newer_sha256, state,
                    older_item_fingerprint, newer_item_fingerprint, older_items, newer_items,
                    added_items, removed_items, unchanged_items, created_utc)
                VALUES ($id, $scope, $ownerId, $olderRevisionId, $newerRevisionId, $olderSha256,
                    $newerSha256, $state, $olderItemFingerprint, $newerItemFingerprint,
                    $olderItems, $newerItems, $added, $removed, $unchanged, $createdUtc);
                """;
            comparison.Parameters.AddWithValue("$id", comparisonId);
            comparison.Parameters.AddWithValue("$scope", scope);
            comparison.Parameters.AddWithValue("$ownerId", older.OwnerId);
            comparison.Parameters.AddWithValue("$olderRevisionId", input.OlderRevisionId);
            comparison.Parameters.AddWithValue("$newerRevisionId", input.NewerRevisionId);
            comparison.Parameters.AddWithValue("$olderSha256", older.Sha256);
            comparison.Parameters.AddWithValue("$newerSha256", newer.Sha256);
            comparison.Parameters.AddWithValue("$state", state);
            comparison.Parameters.AddWithValue("$olderItemFingerprint", olderItemFingerprint);
            comparison.Parameters.AddWithValue("$newerItemFingerprint", newerItemFingerprint);
            comparison.Parameters.AddWithValue("$olderItems", olderItems.Count);
            comparison.Parameters.AddWithValue("$newerItems", newerItems.Count);
            comparison.Parameters.AddWithValue("$added", added.Length);
            comparison.Parameters.AddWithValue("$removed", removed.Length);
            comparison.Parameters.AddWithValue("$unchanged", unchanged);
            comparison.Parameters.AddWithValue("$createdUtc", now);
            comparison.ExecuteNonQuery();
        }
        foreach (var hash in added)
            InsertDelta(connection, transaction, comparisonId, "added", hash, null, newerItems[hash]);
        foreach (var hash in removed)
            InsertDelta(connection, transaction, comparisonId, "removed", hash, olderItems[hash], null);
        transaction.Commit();
        return new DriftComparisonResult(true, comparisonId, state, added.Length, removed.Length, unchanged, null);
    }

    public DriftDispositionResult RecordDisposition(DriftDispositionInput input)
    {
        var id = (input.ComparisonId ?? string.Empty).Trim().ToLowerInvariant();
        if (id.Length is < 12 or > 96 || id.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_')))
            return new DriftDispositionResult(false, id, "invalid", "A valid comparison id is required.");
        var decision = (input.Decision ?? string.Empty).Trim().ToLowerInvariant();
        if (decision is not ("reviewed-no-impact" or "reviewed-action-required" or "deferred"))
            return new DriftDispositionResult(false, id, "invalid", "decision must be reviewed-no-impact, reviewed-action-required, or deferred.");
        var note = (input.Note ?? string.Empty).Trim();
        if (note.Length > 1000)
            return new DriftDispositionResult(false, id, "invalid", "note must not exceed 1000 characters.");

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        string comparisonState;
        using (var exists = connection.CreateCommand())
        {
            exists.Transaction = transaction;
            exists.CommandText = "SELECT state FROM curriculum_revision_comparisons WHERE id=$id;";
            exists.Parameters.AddWithValue("$id", id);
            comparisonState = exists.ExecuteScalar() as string ?? string.Empty;
            if (comparisonState.Length == 0)
            {
                transaction.Rollback();
                return new DriftDispositionResult(false, id, "invalid", "Comparison does not exist.");
            }
        }
        if (decision == "reviewed-no-impact" && comparisonState.Contains("coverage-unproven", StringComparison.Ordinal))
        {
            transaction.Rollback();
            return new DriftDispositionResult(false, id, "refused", "No-impact disposition is unavailable while extraction coverage is unproven.");
        }
        using (var duplicate = connection.CreateCommand())
        {
            duplicate.Transaction = transaction;
            duplicate.CommandText = """
                SELECT COUNT(*) FROM curriculum_revision_dispositions
                WHERE comparison_id=$id AND decision=$decision AND note=$note;
                """;
            duplicate.Parameters.AddWithValue("$id", id);
            duplicate.Parameters.AddWithValue("$decision", decision);
            duplicate.Parameters.AddWithValue("$note", note);
            if (Convert.ToInt32(duplicate.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
            {
                transaction.Rollback();
                return new DriftDispositionResult(true, id, "already-recorded", null);
            }
        }
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO curriculum_revision_dispositions(comparison_id, decision, note, reviewer, occurred_utc)
            VALUES ($id, $decision, $note, 'local-operator', $occurredUtc);
            """;
        insert.Parameters.AddWithValue("$id", id);
        insert.Parameters.AddWithValue("$decision", decision);
        insert.Parameters.AddWithValue("$note", note);
        insert.Parameters.AddWithValue("$occurredUtc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        insert.ExecuteNonQuery();
        transaction.Commit();
        return new DriftDispositionResult(true, id, decision, null);
    }

    public IReadOnlyList<DriftComparison> GetComparisons()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id, c.scope_kind, c.owner_id, c.older_revision_id, c.newer_revision_id,
                c.older_sha256, c.newer_sha256, c.state, c.older_items, c.newer_items,
                c.added_items, c.removed_items, c.unchanged_items, c.created_utc,
                d.decision, d.note, d.occurred_utc
            FROM curriculum_revision_comparisons c
            LEFT JOIN curriculum_revision_dispositions d ON d.sequence=(
                SELECT candidate.sequence FROM curriculum_revision_dispositions candidate
                WHERE candidate.comparison_id=c.id ORDER BY candidate.sequence DESC LIMIT 1)
            ORDER BY c.created_utc DESC, c.id LIMIT 200;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<DriftComparison>();
        while (reader.Read())
            values.Add(new DriftComparison(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3),
                reader.GetInt64(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetInt32(8),
                reader.GetInt32(9), reader.GetInt32(10), reader.GetInt32(11), reader.GetInt32(12), reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14), reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16)));
        return values;
    }

    public IReadOnlyList<DriftDeltaItem> GetDeltaItems(string comparisonId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT change_kind, identity_hash, older_item_id, newer_item_id
            FROM curriculum_revision_delta_items WHERE comparison_id=$id
            ORDER BY change_kind, identity_hash LIMIT 2000;
            """;
        command.Parameters.AddWithValue("$id", comparisonId);
        using var reader = command.ExecuteReader();
        var values = new List<DriftDeltaItem>();
        while (reader.Read())
            values.Add(new DriftDeltaItem(reader.GetString(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        return values;
    }

    private static RevisionIdentity? ReadRevision(SqliteConnection connection, string scope, long id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = scope == "source"
            ? "SELECT source_id, sha256, fetched_utc FROM source_revisions WHERE id=$id;"
            : "SELECT document_id, sha256, fetched_utc FROM curriculum_document_revisions WHERE id=$id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? new RevisionIdentity(reader.GetString(0), reader.GetString(1), reader.GetString(2)) : null;
    }

    private static Dictionary<string, string> ReadItems(SqliteConnection connection, string scope, long id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = scope == "source"
            ? "SELECT statement_sha256, id FROM curriculum_statements WHERE source_revision_id=$id;"
            : "SELECT text_sha256, id FROM curriculum_document_text_blocks WHERE document_revision_id=$id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.Read()) values.TryAdd(reader.GetString(0), reader.GetString(1));
        return values;
    }

    private static string FingerprintItems(IEnumerable<string> hashes)
    {
        var manifest = string.Join("\n", hashes.OrderBy(value => value, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
    }

    private static void InsertDelta(SqliteConnection connection, SqliteTransaction transaction, string comparisonId,
        string changeKind, string identityHash, string? olderItemId, string? newerItemId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT OR IGNORE INTO curriculum_revision_delta_items(comparison_id, change_kind, identity_hash,
                older_item_id, newer_item_id)
            VALUES ($comparisonId, $changeKind, $identityHash, $olderItemId, $newerItemId);
            """;
        command.Parameters.AddWithValue("$comparisonId", comparisonId);
        command.Parameters.AddWithValue("$changeKind", changeKind);
        command.Parameters.AddWithValue("$identityHash", identityHash);
        command.Parameters.AddWithValue("$olderItemId", (object?)olderItemId ?? DBNull.Value);
        command.Parameters.AddWithValue("$newerItemId", (object?)newerItemId ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS curriculum_drift_schema_versions(version INTEGER PRIMARY KEY, applied_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS curriculum_revision_comparisons(
                id TEXT PRIMARY KEY,
                scope_kind TEXT NOT NULL,
                owner_id TEXT NOT NULL,
                older_revision_id INTEGER NOT NULL,
                newer_revision_id INTEGER NOT NULL,
                older_sha256 TEXT NOT NULL,
                newer_sha256 TEXT NOT NULL,
                state TEXT NOT NULL,
                older_item_fingerprint TEXT NOT NULL,
                newer_item_fingerprint TEXT NOT NULL,
                older_items INTEGER NOT NULL,
                newer_items INTEGER NOT NULL,
                added_items INTEGER NOT NULL,
                removed_items INTEGER NOT NULL,
                unchanged_items INTEGER NOT NULL,
                created_utc TEXT NOT NULL,
                UNIQUE(scope_kind, older_revision_id, newer_revision_id, older_sha256, newer_sha256,
                    older_item_fingerprint, newer_item_fingerprint)
            );
            CREATE TABLE IF NOT EXISTS curriculum_revision_delta_items(
                comparison_id TEXT NOT NULL REFERENCES curriculum_revision_comparisons(id) ON DELETE CASCADE,
                change_kind TEXT NOT NULL CHECK(change_kind IN ('added','removed')),
                identity_hash TEXT NOT NULL,
                older_item_id TEXT NULL,
                newer_item_id TEXT NULL,
                PRIMARY KEY(comparison_id, change_kind, identity_hash)
            );
            CREATE TABLE IF NOT EXISTS curriculum_revision_dispositions(
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                comparison_id TEXT NOT NULL REFERENCES curriculum_revision_comparisons(id) ON DELETE CASCADE,
                decision TEXT NOT NULL,
                note TEXT NOT NULL,
                reviewer TEXT NOT NULL,
                occurred_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_curriculum_revision_comparisons_owner
                ON curriculum_revision_comparisons(scope_kind, owner_id, created_utc DESC);
            CREATE INDEX IF NOT EXISTS idx_curriculum_revision_dispositions_comparison
                ON curriculum_revision_dispositions(comparison_id, sequence DESC);
            INSERT OR IGNORE INTO curriculum_drift_schema_versions(version, applied_utc)
                VALUES (1, '2026-08-30T00:00:00Z');
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
        return connection;
    }
}

internal sealed record DriftComparisonInput(string ScopeKind, long OlderRevisionId, long NewerRevisionId);
internal sealed record DriftComparisonResult(bool Ok, string? ComparisonId, string State, int Added, int Removed, int Unchanged, string? Error);
internal sealed record DriftDispositionInput(string ComparisonId, string Decision, string Note);
internal sealed record DriftDispositionResult(bool Ok, string ComparisonId, string State, string? Error);
internal sealed record RevisionIdentity(string OwnerId, string Sha256, string FetchedUtc);
internal sealed record DriftComparison(string Id, string ScopeKind, string OwnerId, long OlderRevisionId,
    long NewerRevisionId, string OlderSha256, string NewerSha256, string State, int OlderItems, int NewerItems,
    int AddedItems, int RemovedItems, int UnchangedItems, string CreatedUtc, string? LatestDecision,
    string? LatestNote, string? LatestDispositionUtc);
internal sealed record DriftDeltaItem(string ChangeKind, string IdentityHash, string? OlderItemId, string? NewerItemId);
