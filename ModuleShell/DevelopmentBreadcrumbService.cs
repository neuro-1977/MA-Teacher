using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed class DevelopmentBreadcrumbService
{
    private readonly string _connectionString;

    public DevelopmentBreadcrumbService(string moduleRoot)
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
    }

    public DevelopmentBreadcrumbContext GetContext(int recordLimit = 200, int issueLimit = 20)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var context = DevelopmentBreadcrumbStore.ReadContext(connection, transaction, recordLimit, issueLimit);
        transaction.Commit();
        return context;
    }

    public DevelopmentBreadcrumbPage GetPage(string? beforeUtc, string? beforeId, int recordLimit = 200)
    {
        if (string.IsNullOrWhiteSpace(beforeUtc) != string.IsNullOrWhiteSpace(beforeId))
            throw new ArgumentException("beforeUtc and beforeId must be supplied together.");

        var cursor = string.IsNullOrWhiteSpace(beforeUtc)
            ? null
            : new DevelopmentBreadcrumbCursor(beforeUtc!, beforeId!);
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var page = DevelopmentBreadcrumbStore.ReadPageBefore(connection, transaction, cursor, recordLimit);
        transaction.Commit();
        return page;
    }

    public DevelopmentBreadcrumbMutation Append(DevelopmentBreadcrumbWrite write)
    {
        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var inserted = DevelopmentBreadcrumbStore.Insert(connection, transaction, write);
            var record = DevelopmentBreadcrumbStore.ReadById(connection, write.Id, transaction)
                ?? throw new InvalidDataException("Persisted breadcrumb could not be read back.");
            transaction.Commit();
            return new DevelopmentBreadcrumbMutation(true, inserted ? "inserted" : "already-present", inserted, record, null);
        }
        catch (ArgumentException exception)
        {
            return new DevelopmentBreadcrumbMutation(false, "invalid", false, null, exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return new DevelopmentBreadcrumbMutation(false, "conflict", false, null, exception.Message);
        }
        catch (SqliteException)
        {
            return new DevelopmentBreadcrumbMutation(false, "failed", false, null, "Canonical breadcrumb persistence failed.");
        }
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
}
