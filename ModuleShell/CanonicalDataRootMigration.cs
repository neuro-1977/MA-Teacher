using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal static class CanonicalDataRootMigration
{
    internal static void Migrate(string suppliedDataRoot)
    {
        var dataRoot = Path.GetFullPath(suppliedDataRoot);
        Directory.CreateDirectory(dataRoot);
        var legacyRoot = Path.Combine(dataRoot, "data");
        var legacyDatabase = Path.Combine(legacyRoot, "ma-teacher.db");
        if (!File.Exists(legacyDatabase)) return;

        var canonicalDatabase = Path.Combine(dataRoot, "ma-teacher.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = canonicalDatabase,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = false,
        }.ToString();

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using (var setup = connection.CreateCommand())
            {
                setup.CommandText = "PRAGMA busy_timeout=5000; ATTACH DATABASE $legacy AS legacy;";
                setup.Parameters.AddWithValue("$legacy", legacyDatabase);
                setup.ExecuteNonQuery();
            }

            try
            {
                var tables = new List<(string Name, string Sql)>();
                using (var inventory = connection.CreateCommand())
                {
                    inventory.CommandText = "SELECT name, sql FROM legacy.sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND sql IS NOT NULL ORDER BY name;";
                    using var reader = inventory.ExecuteReader();
                    while (reader.Read()) tables.Add((reader.GetString(0), reader.GetString(1)));
                }

                using var transaction = connection.BeginTransaction();
                foreach (var table in tables)
                {
                    var createSql = AddIfNotExists(table.Sql);
                    using (var create = connection.CreateCommand())
                    {
                        create.Transaction = transaction;
                        create.CommandText = createSql;
                        create.ExecuteNonQuery();
                    }

                    var columns = new List<string>();
                    using (var columnInventory = connection.CreateCommand())
                    {
                        columnInventory.Transaction = transaction;
                        columnInventory.CommandText = $"PRAGMA legacy.table_info({Quote(table.Name)});";
                        using var reader = columnInventory.ExecuteReader();
                        while (reader.Read()) columns.Add(reader.GetString(1));
                    }
                    if (columns.Count == 0) continue;

                    var columnList = string.Join(", ", columns.Select(Quote));
                    using (var copy = connection.CreateCommand())
                    {
                        copy.Transaction = transaction;
                        copy.CommandText = $"INSERT OR IGNORE INTO main.{Quote(table.Name)} ({columnList}) SELECT {columnList} FROM legacy.{Quote(table.Name)};";
                        copy.ExecuteNonQuery();
                    }

                    using var verify = connection.CreateCommand();
                    verify.Transaction = transaction;
                    verify.CommandText = $"SELECT COUNT(*) FROM (SELECT {columnList} FROM legacy.{Quote(table.Name)} EXCEPT SELECT {columnList} FROM main.{Quote(table.Name)});";
                    if (Convert.ToInt64(verify.ExecuteScalar()) != 0)
                        throw new InvalidOperationException($"Legacy table {table.Name} could not be reproduced exactly in the canonical database.");
                }
                transaction.Commit();
            }
            finally
            {
                using var detach = connection.CreateCommand();
                detach.CommandText = "DETACH DATABASE legacy;";
                detach.ExecuteNonQuery();
            }
        }

        // The legacy database may have been opened earlier in this process and returned to
        // Microsoft.Data.Sqlite's pool. Release those idle Windows handles only after the
        // canonical transaction and exact row verification have both succeeded.
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { legacyDatabase, legacyDatabase + "-wal", legacyDatabase + "-shm" })
            if (File.Exists(path)) File.Delete(path);
        if (Directory.Exists(legacyRoot) && !Directory.EnumerateFileSystemEntries(legacyRoot).Any())
            Directory.Delete(legacyRoot);
    }

    private static string AddIfNotExists(string sql)
    {
        const string prefix = "CREATE TABLE ";
        return sql.StartsWith("CREATE TABLE IF NOT EXISTS ", StringComparison.OrdinalIgnoreCase)
            ? sql
            : sql.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? prefix + "IF NOT EXISTS " + sql[prefix.Length..]
                : sql;
    }

    private static string Quote(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
