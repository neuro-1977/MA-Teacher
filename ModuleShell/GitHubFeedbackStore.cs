using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal sealed record GitHubFeedbackCommentInput(string Id, string Author, string Body, string Url, string CreatedAt, string UpdatedAt);
internal sealed record GitHubFeedbackIssueInput(int Number, string NodeId, string State, string Title, string Body, string Url,
    string Author, string CreatedAt, string UpdatedAt, IReadOnlyList<string>? Labels, IReadOnlyList<GitHubFeedbackCommentInput>? Comments);
internal sealed record GitHubFeedbackBatchImport(string Repository, IReadOnlyList<GitHubFeedbackIssueInput>? Issues);
internal sealed record GitHubFeedbackRecord(string Repository, int Number, string State, string Title, string Body, string Url,
    string Author, string CreatedAt, string UpdatedAt, IReadOnlyList<string> Labels, IReadOnlyList<GitHubFeedbackCommentInput> Comments,
    string ContentSha256, string ImportedUtc);
internal sealed record GitHubFeedbackMutation(bool Ok, string State, int Received, int Inserted, int Updated, int Unchanged, string? Error);

internal sealed class GitHubFeedbackStore
{
    private const string CanonicalRepository = "neuro-1977/MA-Teacher";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _connectionString;

    public GitHubFeedbackStore(string moduleRoot)
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

    public GitHubFeedbackMutation Import(GitHubFeedbackBatchImport batch)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(batch);
            if (!string.Equals(batch.Repository?.Trim(), CanonicalRepository, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Feedback repository must be {CanonicalRepository}.");
            var issues = batch.Issues?.ToArray() ?? [];
            if (issues.Length > 200) throw new ArgumentException("A feedback import is limited to 200 issues.");

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var inserted = 0;
            var updated = 0;
            var unchanged = 0;
            foreach (var issue in issues)
            {
                var normalized = Normalize(issue);
                var hash = ComputeHash(normalized);
                using var existingCommand = connection.CreateCommand();
                existingCommand.Transaction = transaction;
                existingCommand.CommandText = "SELECT content_sha256 FROM github_feedback WHERE repository=$repository AND issue_number=$number;";
                existingCommand.Parameters.AddWithValue("$repository", CanonicalRepository);
                existingCommand.Parameters.AddWithValue("$number", normalized.Number);
                var existing = existingCommand.ExecuteScalar() as string;
                if (string.Equals(existing, hash, StringComparison.Ordinal))
                {
                    unchanged++;
                    continue;
                }

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO github_feedback (
                        repository, issue_number, node_id, state, title, body, url, author,
                        created_utc, updated_utc, labels_json, comments_json, content_sha256, imported_utc
                    ) VALUES (
                        $repository, $number, $nodeId, $state, $title, $body, $url, $author,
                        $created, $updated, $labels, $comments, $hash, $imported
                    ) ON CONFLICT(repository, issue_number) DO UPDATE SET
                        node_id=excluded.node_id, state=excluded.state, title=excluded.title, body=excluded.body,
                        url=excluded.url, author=excluded.author, created_utc=excluded.created_utc,
                        updated_utc=excluded.updated_utc, labels_json=excluded.labels_json,
                        comments_json=excluded.comments_json, content_sha256=excluded.content_sha256,
                        imported_utc=excluded.imported_utc;
                    """;
                command.Parameters.AddWithValue("$repository", CanonicalRepository);
                command.Parameters.AddWithValue("$number", normalized.Number);
                command.Parameters.AddWithValue("$nodeId", normalized.NodeId);
                command.Parameters.AddWithValue("$state", normalized.State);
                command.Parameters.AddWithValue("$title", normalized.Title);
                command.Parameters.AddWithValue("$body", normalized.Body);
                command.Parameters.AddWithValue("$url", normalized.Url);
                command.Parameters.AddWithValue("$author", normalized.Author);
                command.Parameters.AddWithValue("$created", normalized.CreatedAt);
                command.Parameters.AddWithValue("$updated", normalized.UpdatedAt);
                command.Parameters.AddWithValue("$labels", JsonSerializer.Serialize(normalized.Labels, JsonOptions));
                command.Parameters.AddWithValue("$comments", JsonSerializer.Serialize(normalized.Comments, JsonOptions));
                command.Parameters.AddWithValue("$hash", hash);
                command.Parameters.AddWithValue("$imported", DateTimeOffset.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
                if (existing is null) inserted++; else updated++;
            }
            transaction.Commit();
            return new GitHubFeedbackMutation(true, "imported", issues.Length, inserted, updated, unchanged, null);
        }
        catch (ArgumentException exception)
        {
            return new GitHubFeedbackMutation(false, "invalid", 0, 0, 0, 0, exception.Message);
        }
        catch (SqliteException)
        {
            return new GitHubFeedbackMutation(false, "failed", 0, 0, 0, 0, "Canonical feedback persistence failed.");
        }
    }

    public IReadOnlyList<GitHubFeedbackRecord> GetQueue(string? state, int limit)
    {
        var normalizedState = string.IsNullOrWhiteSpace(state) ? "all" : state.Trim().ToLowerInvariant();
        if (normalizedState is not ("all" or "open" or "closed")) throw new ArgumentException("Feedback state must be all, open, or closed.");
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT repository, issue_number, state, title, body, url, author, created_utc, updated_utc,
                   labels_json, comments_json, content_sha256, imported_utc
            FROM github_feedback
            WHERE $state='all' OR state=$state
            ORDER BY CASE state WHEN 'open' THEN 0 ELSE 1 END, updated_utc DESC, issue_number DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$state", normalizedState);
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 500));
        using var reader = command.ExecuteReader();
        var records = new List<GitHubFeedbackRecord>();
        while (reader.Read())
        {
            records.Add(new GitHubFeedbackRecord(reader.GetString(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
                JsonSerializer.Deserialize<List<string>>(reader.GetString(9), JsonOptions) ?? [],
                JsonSerializer.Deserialize<List<GitHubFeedbackCommentInput>>(reader.GetString(10), JsonOptions) ?? [],
                reader.GetString(11), reader.GetString(12)));
        }
        return records;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS github_feedback (
                repository TEXT NOT NULL,
                issue_number INTEGER NOT NULL CHECK (issue_number > 0),
                node_id TEXT NOT NULL,
                state TEXT NOT NULL CHECK (state IN ('open','closed')),
                title TEXT NOT NULL CHECK (length(title) BETWEEN 1 AND 500),
                body TEXT NOT NULL,
                url TEXT NOT NULL,
                author TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                labels_json TEXT NOT NULL,
                comments_json TEXT NOT NULL,
                content_sha256 TEXT NOT NULL CHECK (length(content_sha256)=64),
                imported_utc TEXT NOT NULL,
                PRIMARY KEY (repository, issue_number)
            );
            CREATE INDEX IF NOT EXISTS ix_github_feedback_state_updated
                ON github_feedback(state, updated_utc DESC, issue_number DESC);
            """;
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

    private static GitHubFeedbackIssueInput Normalize(GitHubFeedbackIssueInput issue)
    {
        if (issue.Number <= 0) throw new ArgumentException("Issue numbers must be positive.");
        var state = Required(issue.State, 10, "state").ToLowerInvariant();
        if (state is not ("open" or "closed")) throw new ArgumentException($"Issue {issue.Number} has an invalid state.");
        var url = Required(issue.Url, 1000, "url");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(parsed.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            !parsed.AbsolutePath.StartsWith("/neuro-1977/MA-Teacher/issues/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Issue {issue.Number} has an invalid canonical URL.");
        var labels = (issue.Labels ?? []).Select(label => Required(label, 100, "label")).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(label => label, StringComparer.OrdinalIgnoreCase).Take(50).ToArray();
        var comments = (issue.Comments ?? []).Select(comment => new GitHubFeedbackCommentInput(
            Required(comment.Id, 300, "comment id"),
            Optional(comment.Author, 200), Optional(comment.Body, 50000), Required(comment.Url, 1000, "comment url"),
            IsoUtc(comment.CreatedAt, "comment createdAt"), IsoUtc(comment.UpdatedAt, "comment updatedAt")))
            .OrderBy(comment => comment.CreatedAt, StringComparer.Ordinal).ThenBy(comment => comment.Id, StringComparer.Ordinal).Take(500).ToArray();
        return new GitHubFeedbackIssueInput(issue.Number, Required(issue.NodeId, 300, "nodeId"), state,
            Required(issue.Title, 500, "title"), Optional(issue.Body, 100000), url, Optional(issue.Author, 200),
            IsoUtc(issue.CreatedAt, "createdAt"), IsoUtc(issue.UpdatedAt, "updatedAt"), labels, comments);
    }

    private static string ComputeHash(GitHubFeedbackIssueInput issue)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(issue, JsonOptions))));

    private static string Required(string? value, int maximum, string field)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length is 0 || normalized.Length > maximum) throw new ArgumentException($"Feedback {field} must contain 1 to {maximum} characters.");
        return normalized;
    }

    private static string Optional(string? value, int maximum)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length > maximum) throw new ArgumentException($"Feedback text exceeds {maximum} characters.");
        return normalized;
    }

    private static string IsoUtc(string? value, string field)
        => DateTimeOffset.TryParse(value, out var parsed) ? parsed.ToUniversalTime().ToString("O")
            : throw new ArgumentException($"Feedback {field} must be a valid timestamp.");
}
