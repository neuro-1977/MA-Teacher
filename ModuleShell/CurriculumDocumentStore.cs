using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MATeacher.ModuleShell;

internal sealed class CurriculumDocumentStore
{
    private const int MaximumDocumentBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".odt", ".docx"
    };
    private static readonly Regex LinkRegex = new("<a\\b[^>]*\\bhref\\s*=\\s*[\\\"'](?<url>[^\\\"'#]+)[\\\"'][^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static readonly HttpClient Client = CreateClient();
    private readonly string _connectionString;

    public CurriculumDocumentStore(string dataRoot)
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

    public CurriculumDocumentDiscoveryResult Discover(long sourceRevisionId)
    {
        if (sourceRevisionId < 1)
            return new CurriculumDocumentDiscoveryResult(false, sourceRevisionId, 0, 0, 0, "A positive source revision id is required.");

        using var connection = OpenConnection();
        string sourceId;
        string sourceUrl;
        string contentType;
        string storedSha256;
        long expectedBytes;
        byte[] compressed;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT source_id, source_url, content_type, body_bytes, sha256, body_gzip
                FROM source_revisions WHERE id=$id;
                """;
            command.Parameters.AddWithValue("$id", sourceRevisionId);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return new CurriculumDocumentDiscoveryResult(false, sourceRevisionId, 0, 0, 0, "Captured source revision not found.");
            sourceId = reader.GetString(0);
            sourceUrl = reader.GetString(1);
            contentType = reader.GetString(2);
            expectedBytes = reader.GetInt64(3);
            storedSha256 = reader.GetString(4);
            compressed = (byte[])reader.GetValue(5);
        }
        if (!contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            return new CurriculumDocumentDiscoveryResult(false, sourceRevisionId, 0, 0, 0, "Document discovery requires a captured HTML source revision.");
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var baseUri) || !IsAllowedUri(baseUri))
            return new CurriculumDocumentDiscoveryResult(false, sourceRevisionId, 0, 0, 0, "Captured source URL is outside the official-host boundary.");

        List<DiscoveredDocument> discovered;
        try
        {
            var sourceBody = Decompress(compressed, expectedBytes, 5 * 1024 * 1024);
            var actualSha256 = Convert.ToHexString(SHA256.HashData(sourceBody));
            if (!actualSha256.Equals(storedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Captured source SHA-256 verification failed.");
            var html = Encoding.UTF8.GetString(sourceBody);
            discovered = FindDocuments(sourceId, baseUri, html);
        }
        catch (Exception exception) when (exception is InvalidDataException or RegexMatchTimeoutException)
        {
            return new CurriculumDocumentDiscoveryResult(false, sourceRevisionId, 0, 0, 0, Bound(exception.Message));
        }

        var inserted = 0;
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        using var transaction = connection.BeginTransaction();
        foreach (var document in discovered)
        {
            var existed = DocumentExists(connection, transaction, sourceId, document.Url);
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO curriculum_documents(id, source_id, discovered_from_revision_id, title,
                    document_url, media_type_hint, discovery_state, first_seen_utc, last_seen_utc)
                VALUES ($id, $sourceId, $revisionId, $title, $url, $mediaType, 'discovered-uncaptured', $now, $now)
                ON CONFLICT(source_id, document_url) DO UPDATE SET
                    title=excluded.title,
                    discovered_from_revision_id=excluded.discovered_from_revision_id,
                    media_type_hint=excluded.media_type_hint,
                    last_seen_utc=excluded.last_seen_utc;
                """;
            insert.Parameters.AddWithValue("$id", document.Id);
            insert.Parameters.AddWithValue("$sourceId", sourceId);
            insert.Parameters.AddWithValue("$revisionId", sourceRevisionId);
            insert.Parameters.AddWithValue("$title", document.Title);
            insert.Parameters.AddWithValue("$url", document.Url);
            insert.Parameters.AddWithValue("$mediaType", document.MediaTypeHint);
            insert.Parameters.AddWithValue("$now", now);
            insert.ExecuteNonQuery();
            if (!existed) inserted++;
        }
        transaction.Commit();
        return new CurriculumDocumentDiscoveryResult(true, sourceRevisionId, discovered.Count, inserted, discovered.Count - inserted, null);
    }

    public async Task<CurriculumDocumentCaptureResult> CaptureAsync(string? documentId, CancellationToken cancellationToken)
    {
        var id = (documentId ?? string.Empty).Trim().ToLowerInvariant();
        if (id.Length is < 8 or > 96 || id.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_')))
            return new CurriculumDocumentCaptureResult(false, id, "invalid", 0, null, "A valid document id is required.");

        using var connection = OpenConnection();
        string url;
        using (var lookup = connection.CreateCommand())
        {
            lookup.CommandText = "SELECT document_url FROM curriculum_documents WHERE id=$id;";
            lookup.Parameters.AddWithValue("$id", id);
            url = lookup.ExecuteScalar() as string ?? string.Empty;
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsAllowedUri(uri))
            return new CurriculumDocumentCaptureResult(false, id, "invalid", 0, null, "Document URL is outside the official-host boundary.");

        try
        {
            using var response = await SendDocumentRequestAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new CurriculumDocumentCaptureResult(false, id, "http-failure", 0, null, $"HTTP {(int)response.StatusCode}");
            var finalUri = response.RequestMessage?.RequestUri;
            if (finalUri is null || !IsAllowedUri(finalUri))
                return new CurriculumDocumentCaptureResult(false, id, "redirect-refused", 0, null, "Redirect left the official-host boundary.");
            var declared = response.Content.Headers.ContentLength;
            if (declared is > MaximumDocumentBytes)
                return new CurriculumDocumentCaptureResult(false, id, "too-large", declared.Value, null, "Document exceeds the 25 MB capture boundary.");
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            if (!IsAllowedContentType(contentType, finalUri))
                return new CurriculumDocumentCaptureResult(false, id, "media-refused", 0, null, $"Unsupported document media type: {Bound(contentType)}");
            var storedContentType = contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
                ? MediaTypeFromExtension(Path.GetExtension(finalUri.AbsolutePath))
                : contentType;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            var body = await ReadBoundedAsync(input, cancellationToken);
            var hash = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            using var transaction = connection.BeginTransaction();
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT OR IGNORE INTO curriculum_document_revisions(document_id, fetched_utc, final_url,
                    http_status, content_type, sha256, body_bytes, body_gzip)
                VALUES ($id, $fetchedUtc, $finalUrl, 200, $contentType, $sha256, $bodyBytes, $bodyGzip);
                """;
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$fetchedUtc", now);
            insert.Parameters.AddWithValue("$finalUrl", finalUri.AbsoluteUri);
            insert.Parameters.AddWithValue("$contentType", storedContentType);
            insert.Parameters.AddWithValue("$sha256", hash);
            insert.Parameters.AddWithValue("$bodyBytes", body.LongLength);
            insert.Parameters.AddWithValue("$bodyGzip", Compress(body));
            var inserted = insert.ExecuteNonQuery() == 1;
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE curriculum_documents SET discovery_state='captured-unparsed', last_seen_utc=$now WHERE id=$id;";
            update.Parameters.AddWithValue("$now", now);
            update.Parameters.AddWithValue("$id", id);
            update.ExecuteNonQuery();
            transaction.Commit();
            return new CurriculumDocumentCaptureResult(true, id, inserted ? "captured-unparsed" : "unchanged", body.LongLength, hash, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException or InvalidDataException)
        {
            return new CurriculumDocumentCaptureResult(false, id, "capture-failed", 0, null, Bound(exception.Message));
        }
    }

    public IReadOnlyList<CurriculumDocument> GetDocuments()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.id, d.source_id, d.discovered_from_revision_id, d.title, d.document_url,
                d.media_type_hint, d.discovery_state, d.first_seen_utc, d.last_seen_utc,
                r.id, r.fetched_utc, r.content_type, r.sha256, r.body_bytes
            FROM curriculum_documents d
            LEFT JOIN curriculum_document_revisions r ON r.id=(
                SELECT candidate.id FROM curriculum_document_revisions candidate
                WHERE candidate.document_id=d.id ORDER BY candidate.id DESC LIMIT 1)
            ORDER BY d.source_id, d.title, d.id LIMIT 500;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<CurriculumDocument>();
        while (reader.Read())
            values.Add(new CurriculumDocument(reader.GetString(0), reader.GetString(1), reader.GetInt64(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetInt64(9), reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetInt64(13)));
        return values;
    }

    public CapturedDocumentBody? GetRevisionBody(long revisionId)
    {
        if (revisionId < 1) return null;
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT document_id, content_type, sha256, body_bytes, body_gzip
            FROM curriculum_document_revisions WHERE id=$id;
            """;
        command.Parameters.AddWithValue("$id", revisionId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var expectedBytes = reader.GetInt64(3);
        var compressed = (byte[])reader.GetValue(4);
        var storedSha256 = reader.GetString(2);
        var body = Decompress(compressed, expectedBytes, MaximumDocumentBytes);
        var actualSha256 = Convert.ToHexString(SHA256.HashData(body));
        if (!actualSha256.Equals(storedSha256, StringComparison.OrdinalIgnoreCase)) return null;
        return new CapturedDocumentBody(reader.GetString(0), reader.GetString(1), storedSha256, body);
    }

    public IReadOnlyList<CurriculumDocumentRevision> GetRevisionIndex()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, document_id, fetched_utc, final_url, content_type, sha256, body_bytes
            FROM curriculum_document_revisions ORDER BY id DESC LIMIT 1000;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<CurriculumDocumentRevision>();
        while (reader.Read())
            values.Add(new CurriculumDocumentRevision(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetInt64(6)));
        return values;
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS curriculum_document_schema_versions(version INTEGER PRIMARY KEY, applied_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS curriculum_documents(
                id TEXT PRIMARY KEY,
                source_id TEXT NOT NULL REFERENCES curriculum_sources(id) ON DELETE CASCADE,
                discovered_from_revision_id INTEGER NOT NULL REFERENCES source_revisions(id) ON DELETE RESTRICT,
                title TEXT NOT NULL,
                document_url TEXT NOT NULL,
                media_type_hint TEXT NOT NULL,
                discovery_state TEXT NOT NULL,
                first_seen_utc TEXT NOT NULL,
                last_seen_utc TEXT NOT NULL,
                UNIQUE(source_id, document_url)
            );
            CREATE TABLE IF NOT EXISTS curriculum_document_revisions(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                document_id TEXT NOT NULL REFERENCES curriculum_documents(id) ON DELETE CASCADE,
                fetched_utc TEXT NOT NULL,
                final_url TEXT NOT NULL,
                http_status INTEGER NOT NULL,
                content_type TEXT NOT NULL,
                sha256 TEXT NOT NULL,
                body_bytes INTEGER NOT NULL,
                body_gzip BLOB NOT NULL,
                UNIQUE(document_id, sha256)
            );
            CREATE INDEX IF NOT EXISTS idx_curriculum_documents_source ON curriculum_documents(source_id, discovery_state);
            CREATE INDEX IF NOT EXISTS idx_curriculum_document_revisions_document ON curriculum_document_revisions(document_id, id DESC);
            INSERT OR IGNORE INTO curriculum_document_schema_versions(version, applied_utc) VALUES (1, '2026-08-30T00:00:00Z');
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

    private static List<DiscoveredDocument> FindDocuments(string sourceId, Uri baseUri, string html)
    {
        var values = new List<DiscoveredDocument>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in LinkRegex.Matches(html))
        {
            if (values.Count >= 200) break;
            var decoded = WebUtility.HtmlDecode(match.Groups["url"].Value.Trim());
            if (!Uri.TryCreate(baseUri, decoded, out var uri) || !IsAllowedUri(uri)) continue;
            var extension = Path.GetExtension(uri.AbsolutePath);
            if (!AllowedExtensions.Contains(extension)) continue;
            var canonical = uri.GetLeftPart(UriPartial.Path);
            if (!seen.Add(canonical)) continue;
            var title = NormalizeText(match.Groups["text"].Value);
            if (string.IsNullOrWhiteSpace(title)) title = Path.GetFileNameWithoutExtension(uri.AbsolutePath).Replace('-', ' ').Replace('_', ' ');
            if (title.Length > 240) title = title[..240];
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
            values.Add(new DiscoveredDocument($"doc-{sourceId}-{hash[..16]}", title, canonical, MediaTypeFromExtension(extension)));
        }
        return values;
    }

    private static bool IsAllowedUri(Uri uri) => TrustedLearningSourcePolicy.TryValidate(uri, out _);
    private static bool IsAllowedContentType(string contentType, Uri uri)
    {
        if (!AllowedExtensions.Contains(Path.GetExtension(uri.AbsolutePath))) return false;
        return contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.oasis.opendocument.text", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);
    }
    private static string MediaTypeFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".odt" => "application/vnd.oasis.opendocument.text",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };
    private static string NormalizeText(string value) => WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(TagRegex.Replace(value, " ")), " ").Trim();
    private static bool DocumentExists(SqliteConnection connection, SqliteTransaction transaction, string sourceId, string url)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM curriculum_documents WHERE source_id=$sourceId AND document_url=$url;";
        command.Parameters.AddWithValue("$sourceId", sourceId);
        command.Parameters.AddWithValue("$url", url);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }
    private static async Task<HttpResponseMessage> SendDocumentRequestAsync(Uri initialUri, CancellationToken cancellationToken)
    {
        var current = initialUri;
        for (var redirect = 0; redirect <= 5; redirect++)
        {
            if (!IsAllowedUri(current)) throw new InvalidDataException("Document request left the official-host boundary.");
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oasis.opendocument.text"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!IsRedirect(response.StatusCode)) return response;
            var location = response.Headers.Location;
            response.Dispose();
            if (location is null) throw new InvalidDataException("Document redirect omitted its target.");
            current = location.IsAbsoluteUri ? location : new Uri(current, location);
        }
        throw new InvalidDataException("Document redirect limit exceeded.");
    }
    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther
        or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    private static async Task<byte[]> ReadBoundedAsync(Stream input, CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (output.Length + read > MaximumDocumentBytes) throw new InvalidDataException("Document exceeds the 25 MB capture boundary.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }
    private static byte[] Compress(byte[] body)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) gzip.Write(body);
        return output.ToArray();
    }
    private static byte[] Decompress(byte[] body, long expectedBytes, int maximumBytes)
    {
        if (expectedBytes < 0 || expectedBytes > maximumBytes) throw new InvalidDataException("Captured source size is outside the discovery boundary.");
        using var input = new MemoryStream(body, writable: false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream((int)expectedBytes);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = gzip.Read(buffer, 0, buffer.Length);
            if (read == 0) break;
            if (output.Length + read > maximumBytes) throw new InvalidDataException("Captured source expands beyond the discovery boundary.");
            output.Write(buffer, 0, read);
        }
        if (output.Length != expectedBytes || output.Length > maximumBytes) throw new InvalidDataException("Captured source decompression length mismatch.");
        return output.ToArray();
    }
    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MA-Teacher/0.1.0 official-document-capture");
        return client;
    }
    private static string Bound(string value) => value.Length <= 240 ? value : value[..240];
}

internal sealed record CurriculumDocumentDiscoveryInput(long RevisionId);
internal sealed record CurriculumDocumentCaptureInput(string DocumentId);
internal sealed record CurriculumDocumentDiscoveryResult(bool Ok, long RevisionId, int Discovered, int Inserted, int Existing, string? Error);
internal sealed record CurriculumDocumentCaptureResult(bool Ok, string DocumentId, string State, long BodyBytes, string? Sha256, string? Error);
internal sealed record CurriculumDocument(string Id, string SourceId, long DiscoveredFromRevisionId, string Title,
    string DocumentUrl, string MediaTypeHint, string DiscoveryState, string FirstSeenUtc, string LastSeenUtc,
    long? LatestRevisionId, string? LatestFetchedUtc, string? LatestContentType, string? LatestSha256, long? LatestBodyBytes);
internal sealed record DiscoveredDocument(string Id, string Title, string Url, string MediaTypeHint);
internal sealed record CapturedDocumentBody(string DocumentId, string ContentType, string Sha256, byte[] Body);
internal sealed record CurriculumDocumentRevision(long Id, string DocumentId, string FetchedUtc, string FinalUrl,
    string ContentType, string Sha256, long BodyBytes);
