using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace MATeacher.ModuleShell;

internal sealed class CurriculumDocumentParserStore
{
    private const string ParserId = "ma-teacher-document-text-v2-pdfpig-0.1.16";
    private const int MaximumXmlBytes = 40 * 1024 * 1024;
    private const int MaximumBlocks = 10_000;
    private const int MaximumBlockCharacters = 4_000;
    private const int MaximumTotalCharacters = 10 * 1024 * 1024;
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
    private readonly string _connectionString;

    public CurriculumDocumentParserStore(string dataRoot)
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

    public DocumentParseResult Parse(long revisionId, CapturedDocumentBody revision)
    {
        if (revisionId < 1)
            return new DocumentParseResult(false, revisionId, "invalid", 0, 0, 0, "A positive document revision id is required.");
        var format = ResolveFormat(revision.ContentType);
        if (format is null)
        {
            RecordReceipt(revisionId, "unsupported-format", 0, 0, "No reviewed parser is available for this media type.");
            return new DocumentParseResult(false, revisionId, "unsupported-format", 0, 0, 0,
                revision.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                    ? "PDF parsing remains locked until a reviewed parser is selected."
                    : "No reviewed parser is available for this media type.");
        }

        List<DocumentTextDraft> drafts;
        try
        {
            drafts = ExtractBlocks(revisionId, revision.Body, format);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            var error = Bound(exception.Message);
            RecordReceipt(revisionId, "parse-failed", 0, 0, error);
            return new DocumentParseResult(false, revisionId, "parse-failed", 0, 0, 0, error);
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        var inserted = 0;
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        foreach (var draft in drafts)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT OR IGNORE INTO curriculum_document_text_blocks(id, document_revision_id, ordinal,
                    source_locator, text_content, text_sha256, parser_id, extraction_state, created_utc)
                VALUES ($id, $revisionId, $ordinal, $locator, $text, $sha256, $parserId,
                    'machine-extracted-unreviewed', $createdUtc);
                """;
            insert.Parameters.AddWithValue("$id", draft.Id);
            insert.Parameters.AddWithValue("$revisionId", revisionId);
            insert.Parameters.AddWithValue("$ordinal", draft.Ordinal);
            insert.Parameters.AddWithValue("$locator", draft.SourceLocator);
            insert.Parameters.AddWithValue("$text", draft.Text);
            insert.Parameters.AddWithValue("$sha256", draft.Sha256);
            insert.Parameters.AddWithValue("$parserId", ParserId);
            insert.Parameters.AddWithValue("$createdUtc", now);
            inserted += insert.ExecuteNonQuery();
        }
        InsertReceipt(connection, transaction, revisionId, "blocks-recorded-unreviewed", drafts.Count, inserted, string.Empty, now);
        transaction.Commit();
        return new DocumentParseResult(true, revisionId, "blocks-recorded-unreviewed", drafts.Count, inserted,
            drafts.Count - inserted, null);
    }

    public IReadOnlyList<DocumentTextBlock> GetBlocks()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT b.id, b.document_revision_id, r.document_id, b.ordinal, b.source_locator,
                b.text_content, b.text_sha256, b.parser_id, b.extraction_state, b.created_utc
            FROM curriculum_document_text_blocks b
            JOIN curriculum_document_revisions r ON r.id=b.document_revision_id
            ORDER BY b.document_revision_id DESC, b.ordinal, b.id LIMIT 5000;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<DocumentTextBlock>();
        while (reader.Read())
            values.Add(new DocumentTextBlock(reader.GetString(0), reader.GetInt64(1), reader.GetString(2), reader.GetInt32(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8), reader.GetString(9)));
        return values;
    }

    private static List<DocumentTextDraft> ExtractBlocks(long revisionId, byte[] body, string format)
    {
        if (format == "pdf") return ExtractPdfBlocks(revisionId, body);
        if (body.Length < 4 || body[0] != (byte)'P' || body[1] != (byte)'K')
            throw new InvalidDataException("Document does not have the required ZIP container signature.");
        using var input = new MemoryStream(body, writable: false);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: false);
        var entryName = format == "docx" ? "word/document.xml" : "content.xml";
        var entry = archive.GetEntry(entryName) ?? throw new InvalidDataException($"Required {entryName} entry is missing.");
        if (entry.Length < 1 || entry.Length > MaximumXmlBytes)
            throw new InvalidDataException("Document XML entry is outside the 40 MB parser boundary.");
        using var entryStream = entry.Open();
        using var bounded = ReadBounded(entryStream, entry.Length);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaximumXmlBytes,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };
        using var xmlReader = XmlReader.Create(bounded, settings);
        var document = XDocument.Load(xmlReader, LoadOptions.None);
        var paragraphs = format == "docx"
            ? document.Descendants().Where(element => element.Name.LocalName == "p")
                .Select(element => string.Concat(element.Descendants().Where(node => node.Name.LocalName == "t").Select(node => node.Value)))
            : document.Descendants().Where(element => element.Name.LocalName is "p" or "h").Select(element => element.Value);

        var values = new List<DocumentTextDraft>();
        var totalCharacters = 0;
        var ordinal = 0;
        foreach (var paragraph in paragraphs)
        {
            ordinal++;
            var normalized = WhiteSpaceRegex.Replace(paragraph, " ").Trim();
            if (normalized.Length == 0) continue;
            for (var offset = 0; offset < normalized.Length; offset += MaximumBlockCharacters)
            {
                if (values.Count >= MaximumBlocks) throw new InvalidDataException("Document exceeds the 10,000 block parser boundary.");
                var length = Math.Min(MaximumBlockCharacters, normalized.Length - offset);
                totalCharacters += length;
                if (totalCharacters > MaximumTotalCharacters) throw new InvalidDataException("Document text exceeds the 10 MB character boundary.");
                var text = normalized.Substring(offset, length);
                var locator = $"{entryName}:paragraph:{ordinal}:offset:{offset}";
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
                values.Add(new DocumentTextDraft($"block-{revisionId}-{ordinal}-{offset}-{hash[..12]}", ordinal,
                    locator, text, hash));
            }
        }
        return values;
    }

    private static List<DocumentTextDraft> ExtractPdfBlocks(long revisionId, byte[] body)
    {
        if (body.Length < 5 || body[0] != (byte)'%' || body[1] != (byte)'P' || body[2] != (byte)'D' || body[3] != (byte)'F' || body[4] != (byte)'-')
            throw new InvalidDataException("Document does not have the required PDF signature.");
        using var document = PdfDocument.Open(body, new ParsingOptions { UseLenientParsing = false });
        if (document.NumberOfPages < 1 || document.NumberOfPages > 2000)
            throw new InvalidDataException("PDF page count is outside the 1-2,000 page boundary.");
        var values = new List<DocumentTextDraft>();
        var totalCharacters = 0;
        for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
        {
            var page = document.GetPage(pageNumber);
            var normalized = WhiteSpaceRegex.Replace(ContentOrderTextExtractor.GetText(page), " ").Trim();
            if (normalized.Length == 0) continue;
            for (var offset = 0; offset < normalized.Length; offset += MaximumBlockCharacters)
            {
                if (values.Count >= MaximumBlocks) throw new InvalidDataException("PDF exceeds the 10,000 block parser boundary.");
                var length = Math.Min(MaximumBlockCharacters, normalized.Length - offset);
                totalCharacters += length;
                if (totalCharacters > MaximumTotalCharacters) throw new InvalidDataException("PDF text exceeds the 10 MB character boundary.");
                var text = normalized.Substring(offset, length);
                var locator = $"pdf:page:{pageNumber}:offset:{offset}";
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
                values.Add(new DocumentTextDraft($"block-{revisionId}-page-{pageNumber}-{offset}-{hash[..12]}",
                    pageNumber, locator, text, hash));
            }
        }
        return values;
    }

    private static MemoryStream ReadBounded(Stream input, long expectedBytes)
    {
        var output = new MemoryStream((int)expectedBytes);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = input.Read(buffer, 0, buffer.Length);
            if (read == 0) break;
            if (output.Length + read > MaximumXmlBytes) throw new InvalidDataException("Document XML expands beyond the parser boundary.");
            output.Write(buffer, 0, read);
        }
        if (output.Length != expectedBytes) throw new InvalidDataException("Document XML length does not match its ZIP entry metadata.");
        output.Position = 0;
        return output;
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS curriculum_document_parser_schema_versions(version INTEGER PRIMARY KEY, applied_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS curriculum_document_text_blocks(
                id TEXT PRIMARY KEY,
                document_revision_id INTEGER NOT NULL REFERENCES curriculum_document_revisions(id) ON DELETE CASCADE,
                ordinal INTEGER NOT NULL,
                source_locator TEXT NOT NULL,
                text_content TEXT NOT NULL,
                text_sha256 TEXT NOT NULL,
                parser_id TEXT NOT NULL,
                extraction_state TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                UNIQUE(document_revision_id, source_locator, text_sha256)
            );
            CREATE TABLE IF NOT EXISTS curriculum_document_parse_receipts(
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                document_revision_id INTEGER NOT NULL REFERENCES curriculum_document_revisions(id) ON DELETE CASCADE,
                parser_id TEXT NOT NULL,
                state TEXT NOT NULL,
                blocks_found INTEGER NOT NULL,
                blocks_inserted INTEGER NOT NULL,
                error_text TEXT NOT NULL,
                occurred_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_curriculum_document_text_blocks_revision
                ON curriculum_document_text_blocks(document_revision_id, ordinal);
            CREATE INDEX IF NOT EXISTS idx_curriculum_document_parse_receipts_revision
                ON curriculum_document_parse_receipts(document_revision_id, sequence DESC);
            INSERT OR IGNORE INTO curriculum_document_parser_schema_versions(version, applied_utc)
                VALUES (1, '2026-08-30T00:00:00Z');
            """;
        command.ExecuteNonQuery();
    }

    private void RecordReceipt(long revisionId, string state, int found, int inserted, string error)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        InsertReceipt(connection, transaction, revisionId, state, found, inserted, error,
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        transaction.Commit();
    }

    private static void InsertReceipt(SqliteConnection connection, SqliteTransaction transaction, long revisionId,
        string state, int found, int inserted, string error, string occurredUtc)
    {
        using var receipt = connection.CreateCommand();
        receipt.Transaction = transaction;
        receipt.CommandText = """
            INSERT INTO curriculum_document_parse_receipts(document_revision_id, parser_id, state,
                blocks_found, blocks_inserted, error_text, occurred_utc)
            VALUES ($revisionId, $parserId, $state, $found, $inserted, $error, $occurredUtc);
            """;
        receipt.Parameters.AddWithValue("$revisionId", revisionId);
        receipt.Parameters.AddWithValue("$parserId", ParserId);
        receipt.Parameters.AddWithValue("$state", state);
        receipt.Parameters.AddWithValue("$found", found);
        receipt.Parameters.AddWithValue("$inserted", inserted);
        receipt.Parameters.AddWithValue("$error", error);
        receipt.Parameters.AddWithValue("$occurredUtc", occurredUtc);
        receipt.ExecuteNonQuery();
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

    private static string? ResolveFormat(string contentType)
    {
        if (contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)) return "docx";
        if (contentType.Equals("application/vnd.oasis.opendocument.text", StringComparison.OrdinalIgnoreCase)) return "odt";
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) return "pdf";
        return null;
    }
    private static string Bound(string value) => value.Length <= 240 ? value : value[..240];
}

internal sealed record DocumentParseInput(long DocumentRevisionId);
internal sealed record DocumentParseResult(bool Ok, long DocumentRevisionId, string State, int BlocksFound,
    int Inserted, int Existing, string? Error);
internal sealed record DocumentTextDraft(string Id, int Ordinal, string SourceLocator, string Text, string Sha256);
internal sealed record DocumentTextBlock(string Id, long DocumentRevisionId, string DocumentId, int Ordinal,
    string SourceLocator, string TextContent, string TextSha256, string ParserId, string ExtractionState, string CreatedUtc);
