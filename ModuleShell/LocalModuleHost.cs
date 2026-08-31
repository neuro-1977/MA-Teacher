using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MATeacher.ModuleShell;

/// <summary>
/// Keeps a module self-contained: the desktop shell and a bookmarkable local URL
/// are two views of the same static bundle. This is not a Windows service.
/// </summary>
internal sealed class LocalModuleHost : IDisposable
{
    private static readonly ModuleIdentity Identity = new("io.github.neuro1977.ma-teacher", "MA-Teacher", 5201);

    private readonly HttpListener _listener = new();
    private readonly string _uiRoot;
    private readonly ModuleIdentity _identity;
    private readonly CurriculumEvidenceStore _evidence;
    private readonly TeachingWorkspaceStore _teaching;
    private readonly CurriculumDocumentStore _documents;
    private readonly CurriculumDocumentParserStore _documentParser;
    private readonly CurriculumDriftStore _drift;
    private readonly TeachingReferenceStore _teachingReferences;
    private readonly ProjectReadinessStore _readiness;
    private readonly CurriculumCoverageStore _coverage;
    private readonly LessonReviewStore _lessonReviews;
    private readonly TeachingSessionStore _teachingSessions;
    private readonly TeachingOperationsStore _teachingOperations;
    private readonly LearningCheckStore _learningChecks;
    private readonly LearnerSafetyStore _learnerSafety;
    private readonly ClassroomPrintStore _printRequests;
    private readonly LocalPrinterService _printing;
    private readonly ClassroomRelayHost _classroom;
    private readonly TeachingProposalStore _teachingProposals;
    private readonly LearningProgressStore _learningProgress;
    private readonly DatabaseBackupStore _databaseBackups;
    private readonly DataStewardshipStore _dataStewardship;
    private readonly AccessibilityReviewStore _accessibilityReviews;
    private readonly DevelopmentBreadcrumbService _developmentBreadcrumbs;
    private readonly GitHubFeedbackStore _githubFeedback;
    private readonly CancellationTokenSource _stopping = new();
    private Task? _serveTask;

    private readonly bool _includeDiagnosticErrors;

    public LocalModuleHost(string uiRoot, string dataRoot, bool includeDiagnosticErrors = false, int? listenerPort = null)
    {
        _includeDiagnosticErrors = includeDiagnosticErrors;
        _uiRoot = Path.GetFullPath(uiRoot);
        CanonicalDataRootMigration.Migrate(dataRoot);
        _identity = listenerPort is > 0 and <= 65535 ? Identity with { Port = listenerPort.Value } : Identity;
        _evidence = new CurriculumEvidenceStore(dataRoot);
        _documents = new CurriculumDocumentStore(dataRoot);
        _documentParser = new CurriculumDocumentParserStore(dataRoot);
        _teaching = new TeachingWorkspaceStore(dataRoot);
        _drift = new CurriculumDriftStore(dataRoot);
        _teachingReferences = new TeachingReferenceStore(dataRoot);
        _readiness = new ProjectReadinessStore(dataRoot);
        _coverage = new CurriculumCoverageStore(dataRoot);
        _lessonReviews = new LessonReviewStore(dataRoot);
        _teachingSessions = new TeachingSessionStore(dataRoot, _lessonReviews);
        _learningChecks = new LearningCheckStore(dataRoot, _lessonReviews);
        _learnerSafety = new LearnerSafetyStore(dataRoot);
        _printRequests = new ClassroomPrintStore(dataRoot);
        _printing = new LocalPrinterService(_printRequests, _teaching, _learningChecks, _learnerSafety);
        _classroom = new ClassroomRelayHost(_uiRoot, _teaching, _lessonReviews, _learningChecks, _learnerSafety, _printRequests);
        _teachingOperations = new TeachingOperationsStore(dataRoot, _lessonReviews, _teachingSessions);
        _teachingProposals = new TeachingProposalStore(dataRoot);
        _learningProgress = new LearningProgressStore(dataRoot);
        _databaseBackups = new DatabaseBackupStore(dataRoot);
        _dataStewardship = new DataStewardshipStore(dataRoot);
        _accessibilityReviews = new AccessibilityReviewStore(dataRoot);
        _developmentBreadcrumbs = new DevelopmentBreadcrumbService(dataRoot);
        _githubFeedback = new GitHubFeedbackStore(dataRoot);
        BaseAddress = $"http://127.0.0.1:{_identity.Port}/";
        _listener.Prefixes.Add(BaseAddress);
    }

    public string BaseAddress { get; }

    public Task<bool> StartAsync()
    {
        try
        {
            _listener.Start();
            _serveTask = Task.Run(ServeAsync);
            return Task.FromResult(true);
        }
        catch (HttpListenerException)
        {
            return Task.FromResult(false);
        }
    }

    private async Task ServeAsync()
    {
        while (!_stopping.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (_stopping.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => WriteResponseAsync(context));
        }
    }

    private async Task WriteResponseAsync(HttpListenerContext context)
    {
        try
        {
            var path = Uri.UnescapeDataString(context.Request.Url?.AbsolutePath ?? "/");
            if (string.Equals(path, "/ma-id", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await WriteTextAsync(context.Response, JsonSerializer.Serialize(new
                {
                    id = _identity.Id,
                    name = _identity.Name,
                    port = _identity.Port,
                }));
                return;
            }

            if (string.Equals(path, "/api/health", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, _evidence.GetHealth());
                return;
            }

            if (string.Equals(path, "/api/curriculum/overview", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, _evidence.GetOverview());
                return;
            }

            if (string.Equals(path, "/api/curriculum/refresh", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "refresh-curriculum-sources", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin operator intent is required." });
                    return;
                }

                var result = await _evidence.RefreshSourcesAsync(_stopping.Token);
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadGateway;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/curriculum/revisions", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, new { ok = true, revisions = _evidence.GetRevisionIndex() });
                return;
            }

            if (string.Equals(path, "/api/teaching/overview", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, _teaching.GetOverview());
                return;
            }

            if (string.Equals(path, "/api/curriculum/drift", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, new { ok = true, comparisons = _drift.GetComparisons() });
                return;
            }

            if (path.StartsWith("/api/curriculum/drift/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith("/items", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                const string driftPrefix = "/api/curriculum/drift/";
                const string itemsSuffix = "/items";
                var comparisonId = path.Substring(driftPrefix.Length, path.Length - driftPrefix.Length - itemsSuffix.Length);
                await WriteJsonAsync(context.Response, new { ok = true, items = _drift.GetDeltaItems(comparisonId) });
                return;
            }

            if (string.Equals(path, "/api/curriculum/drift/compare", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/api/curriculum/drift/disposition", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "reconcile-curriculum-revisions", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin curriculum-reconciliation intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 8192)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Curriculum-reconciliation body must be 1-8192 bytes." });
                    return;
                }
                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                object result;
                try
                {
                    if (path.EndsWith("/compare", StringComparison.OrdinalIgnoreCase))
                    {
                        var input = JsonSerializer.Deserialize<DriftComparisonInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new DriftComparisonResult(false, null, "invalid", 0, 0, 0, "A comparison request is required.")
                            : _drift.Compare(input);
                    }
                    else
                    {
                        var input = JsonSerializer.Deserialize<DriftDispositionInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new DriftDispositionResult(false, string.Empty, "invalid", "A disposition request is required.")
                            : _drift.RecordDisposition(input);
                    }
                }
                catch (JsonException)
                {
                    result = new DriftDispositionResult(false, string.Empty, "invalid", "Body must be valid JSON.");
                }
                var succeeded = result switch
                {
                    DriftComparisonResult comparison => comparison.Ok,
                    DriftDispositionResult disposition => disposition.Ok,
                    _ => false
                };
                context.Response.StatusCode = succeeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/curriculum/documents", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, new { ok = true, documents = _documents.GetDocuments() });
                return;
            }

            if (string.Equals(path, "/api/curriculum/document-revisions", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, new { ok = true, revisions = _documents.GetRevisionIndex() });
                return;
            }

            if (string.Equals(path, "/api/curriculum/document-blocks", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, new { ok = true, blocks = _documentParser.GetBlocks() });
                return;
            }

            if (string.Equals(path, "/api/curriculum/documents/parse", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "parse-curriculum-document", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin document-parser intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 4096)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Document-parser body must be 1-4096 bytes." });
                    return;
                }
                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                DocumentParseResult result;
                try
                {
                    var input = JsonSerializer.Deserialize<DocumentParseInput>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var revision = input is null ? null : _documents.GetRevisionBody(input.DocumentRevisionId);
                    result = input is null || revision is null
                        ? new DocumentParseResult(false, input?.DocumentRevisionId ?? 0, "invalid", 0, 0, 0, "A captured document revision is required.")
                        : _documentParser.Parse(input.DocumentRevisionId, revision);
                }
                catch (JsonException)
                {
                    result = new DocumentParseResult(false, 0, "invalid", 0, 0, 0, "Body must be valid JSON.");
                }
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/curriculum/documents/discover", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/api/curriculum/documents/capture", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "manage-curriculum-documents", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin curriculum-document intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 4096)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Curriculum-document body must be 1-4096 bytes." });
                    return;
                }
                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                object result;
                try
                {
                    if (path.EndsWith("/discover", StringComparison.OrdinalIgnoreCase))
                    {
                        var input = JsonSerializer.Deserialize<CurriculumDocumentDiscoveryInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new CurriculumDocumentDiscoveryResult(false, 0, 0, 0, 0, "A captured source revision is required.")
                            : _documents.Discover(input.RevisionId);
                    }
                    else
                    {
                        var input = JsonSerializer.Deserialize<CurriculumDocumentCaptureInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new CurriculumDocumentCaptureResult(false, string.Empty, "invalid", 0, null, "A discovered document is required.")
                            : await _documents.CaptureAsync(input.DocumentId, _stopping.Token);
                    }
                }
                catch (JsonException)
                {
                    result = new CurriculumDocumentCaptureResult(false, string.Empty, "invalid", 0, null, "Body must be valid JSON.");
                }
                var succeeded = result switch
                {
                    CurriculumDocumentDiscoveryResult discovery => discovery.Ok,
                    CurriculumDocumentCaptureResult capture => capture.Ok,
                    _ => false
                };
                context.Response.StatusCode = succeeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/curriculum/candidates", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, new { ok = true, candidates = _teaching.GetCurriculumCandidates() });
                return;
            }

            if (string.Equals(path, "/api/curriculum/candidates/extract", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/api/curriculum/candidates/review", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "review-curriculum-candidates", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin curriculum-review intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 16384)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Curriculum-review body must be 1-16384 bytes." });
                    return;
                }

                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                object result;
                try
                {
                    if (path.EndsWith("/extract", StringComparison.OrdinalIgnoreCase))
                    {
                        var input = JsonSerializer.Deserialize<CurriculumExtractionInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        var revision = input is null ? null : _evidence.GetRevisionBody(input.RevisionId);
                        result = input is null || revision is null
                            ? new CurriculumExtractionResult(false, "invalid", input?.RevisionId ?? 0, 0, 0, 0, "A captured source revision is required.")
                            : _teaching.ExtractCurriculumCandidates(input.RevisionId, revision);
                    }
                    else
                    {
                        var input = JsonSerializer.Deserialize<CurriculumReviewInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new TeachingMutationResult(false, "invalid", null, "A valid curriculum review is required.")
                            : _teaching.ReviewCurriculumCandidate(input);
                    }
                }
                catch (JsonException)
                {
                    result = new TeachingMutationResult(false, "invalid", null, "Body must be valid JSON.");
                }
                var succeeded = result switch
                {
                    CurriculumExtractionResult extraction => extraction.Ok,
                    TeachingMutationResult mutation => mutation.Ok,
                    _ => false
                };
                context.Response.StatusCode = succeeded ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/curriculum/document-blocks/extract-candidates", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "review-curriculum-candidates", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin curriculum-review intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 4096)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Document-candidate body must be 1-4096 bytes." });
                    return;
                }
                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                DocumentCandidateExtractionResult result;
                try
                {
                    var input = JsonSerializer.Deserialize<DocumentCandidateExtractionInput>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result = input is null
                        ? new DocumentCandidateExtractionResult(false, 0, 0, 0, 0, 0, "A document revision is required.")
                        : _teaching.ExtractDocumentCandidates(input.DocumentRevisionId);
                }
                catch (JsonException)
                {
                    result = new DocumentCandidateExtractionResult(false, 0, 0, 0, 0, 0, "Body must be valid JSON.");
                }
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/teaching/workspace", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, _teaching.GetWorkspace());
                return;
            }

            if (string.Equals(path, "/api/teaching/learners", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/api/teaching/study-plans", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "manage-learning-workspace", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin learning-workspace intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 16384)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Learning-workspace body must be 1-16384 bytes." });
                    return;
                }

                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                TeachingMutationResult result;
                try
                {
                    if (path.EndsWith("/learners", StringComparison.OrdinalIgnoreCase))
                    {
                        var input = JsonSerializer.Deserialize<LearnerProfileInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new TeachingMutationResult(false, "invalid", null, "A valid learner profile is required.")
                            : _teaching.CreateLearner(input);
                    }
                    else
                    {
                        var input = JsonSerializer.Deserialize<StudyPlanInput>(body,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result = input is null
                            ? new TeachingMutationResult(false, "invalid", null, "A valid study plan is required.")
                            : _teaching.CreateStudyPlan(input);
                    }
                }
                catch (JsonException)
                {
                    result = new TeachingMutationResult(false, "invalid", null, "Body must be valid JSON.");
                }

                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK
                    : result.State == "conflict" ? (int)HttpStatusCode.Conflict
                    : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/storage/backups", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                { await WriteJsonAsync(context.Response, _databaseBackups.GetOverview()); return; }
                if (!ValidDatabaseBackupMutationRequest(context.Request, "create-local-database-backup", 1024))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin backup intent is required." }); return; }
                var result = await ReadDatabaseBackupMutationAsync<DatabaseBackupInput>(context.Request, _databaseBackups.CreateBackup);
                context.Response.StatusCode = DatabaseBackupMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }
            if (string.Equals(path, "/api/storage/backups/verify", StringComparison.OrdinalIgnoreCase))
            {
                if (!ValidDatabaseBackupMutationRequest(context.Request, "verify-local-database-backup", 1024))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin backup-verification intent is required." }); return; }
                var result = await ReadDatabaseBackupMutationAsync<DatabaseBackupVerifyInput>(context.Request, _databaseBackups.VerifyBackup);
                context.Response.StatusCode = DatabaseBackupMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/storage/stewardship", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                { await WriteJsonAsync(context.Response, _dataStewardship.GetOverview()); return; }
                if (!ValidDatabaseBackupMutationRequest(context.Request, "record-local-retention-policy", 16384))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin data-stewardship intent is required." }); return; }
                var result = await ReadDataStewardshipMutationAsync(context.Request);
                context.Response.StatusCode = DataStewardshipMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/accessibility/reviews", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                { await WriteJsonAsync(context.Response, _accessibilityReviews.GetOverview()); return; }
                if (!ValidTeachingProposalMutationRequest(context.Request, "record-accessibility-review", 65536))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin accessibility-review intent is required." }); return; }
                var result = await ReadAccessibilityReviewMutationAsync(context.Request);
                context.Response.StatusCode = AccessibilityReviewMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/learning/progress", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Learning progress evidence is read-only." }); return;
                }
                await WriteJsonAsync(context.Response, _learningProgress.GetOverview()); return;
            }

            if (string.Equals(path, "/api/teaching/lesson-reviews", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                { await WriteJsonAsync(context.Response, _lessonReviews.GetOverview()); return; }
                if (!ValidLessonReviewMutationRequest(context.Request))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin lesson-review intent is required." }); return; }
                var result = await ReadLessonReviewMutationAsync(context.Request);
                context.Response.StatusCode = LessonReviewMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/teaching/sessions", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                { await WriteJsonAsync(context.Response, _teachingSessions.GetOverview()); return; }
                if (!ValidTeachingProposalMutationRequest(context.Request, "record-teaching-session-receipt", 32768))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin teaching-session intent is required." }); return; }
                var result = await ReadTeachingSessionMutationAsync(context.Request);
                context.Response.StatusCode = TeachingSessionMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/teaching/operations", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                { context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed; await WriteJsonAsync(context.Response, new { ok = false, error = "Teaching operations evidence is read-only." }); return; }
                await WriteJsonAsync(context.Response, _teachingOperations.GetOverview()); return;
            }

            if (string.Equals(path, "/api/teaching/proposals", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(context.Response, _teachingProposals.GetOverview()); return;
                }
                if (!ValidTeachingProposalMutationRequest(context.Request, "record-teaching-proposal", 65536))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin teaching-proposal intent is required." }); return;
                }
                var result = await ReadTeachingProposalMutationAsync<TeachingProposalInput>(context.Request, _teachingProposals.CreateProposal);
                context.Response.StatusCode = TeachingProposalMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }
            if (string.Equals(path, "/api/teaching/proposal-reviews", StringComparison.OrdinalIgnoreCase))
            {
                if (!ValidTeachingProposalMutationRequest(context.Request, "review-teaching-proposal", 32768))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin teaching-proposal review intent is required." }); return;
                }
                var result = await ReadTeachingProposalMutationAsync<TeachingProposalReviewInput>(context.Request, _teachingProposals.ReviewProposal);
                context.Response.StatusCode = TeachingProposalMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/teaching/checks", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(context.Response, _learningChecks.GetOverview()); return;
                }
                if (!ValidLearningCheckMutationRequest(context.Request, "create-evidence-linked-check", 65536))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin learning-check intent is required." }); return;
                }
                var result = await ReadLearningCheckMutationAsync<LearningCheckInput>(context.Request, _learningChecks.CreateCheck);
                context.Response.StatusCode = LearningCheckMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }
            if (path.StartsWith("/api/teaching/check-attempts/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/attachment", StringComparison.OrdinalIgnoreCase) && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var encodedId = path["/api/teaching/check-attempts/".Length..^"/attachment".Length];
                if (string.IsNullOrWhiteSpace(encodedId) || encodedId.Contains('/')) { context.Response.StatusCode = (int)HttpStatusCode.BadRequest; await WriteJsonAsync(context.Response, new { ok = false, error = "A single attempt id is required." }); return; }
                var attachment = _learningChecks.GetAttemptAttachment(Uri.UnescapeDataString(encodedId));
                if (attachment is null) { context.Response.StatusCode = (int)HttpStatusCode.NotFound; await WriteJsonAsync(context.Response, new { ok = false, error = "Attempt attachment was not found or failed integrity verification." }); return; }
                context.Response.ContentType = attachment.MediaType; context.Response.ContentLength64 = attachment.ByteLength; context.Response.Headers["Cache-Control"] = "no-store"; context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Content-Disposition"] = $"attachment; filename*=UTF-8''{Uri.EscapeDataString(attachment.FileName)}"; await context.Response.OutputStream.WriteAsync(attachment.Body, _stopping.Token); return;
            }
            if (string.Equals(path, "/api/teaching/check-attempts", StringComparison.OrdinalIgnoreCase))
            {
                if (!ValidLearningCheckMutationRequest(context.Request, "submit-learning-check-attempt", 15 * 1024 * 1024))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin learning-attempt intent is required." }); return;
                }
                var result = await ReadLearningCheckMutationAsync<LearningCheckAttemptInput>(context.Request, _learningChecks.SubmitAttempt);
                context.Response.StatusCode = LearningCheckMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }
            if (string.Equals(path, "/api/teaching/check-reviews", StringComparison.OrdinalIgnoreCase))
            {
                if (!ValidLearningCheckMutationRequest(context.Request, "review-learning-check-attempt", 32768))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin human-review intent is required." }); return;
                }
                var result = await ReadLearningCheckMutationAsync<LearningCheckReviewInput>(context.Request, _learningChecks.ReviewAttempt);
                context.Response.StatusCode = LearningCheckMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (path.StartsWith("/api/teaching/lessons/", StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var encodedId = path["/api/teaching/lessons/".Length..];
                if (string.IsNullOrWhiteSpace(encodedId) || encodedId.Contains('/'))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "A single lesson id is required." });
                    return;
                }
                var result = _teaching.GetLessonDetail(Uri.UnescapeDataString(encodedId));
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/teaching/lesson-drafts", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "draft-evidence-linked-lesson", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin lesson-draft intent is required." });
                    return;
                }
                if (context.Request.ContentLength64 is < 1 or > 32768)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Lesson-draft body must be 1-32768 bytes." });
                    return;
                }
                using var bodyReader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await bodyReader.ReadToEndAsync(_stopping.Token);
                TeachingMutationResult result;
                try
                {
                    var input = JsonSerializer.Deserialize<LessonDraftInput>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result = input is null
                        ? new TeachingMutationResult(false, "invalid", null, "A valid lesson draft is required.")
                        : _teaching.CreateLessonDraft(input);
                }
                catch (JsonException)
                {
                    result = new TeachingMutationResult(false, "invalid", null, "Body must be valid JSON.");
                }
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK
                    : result.State == "conflict" ? (int)HttpStatusCode.Conflict
                    : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/curriculum/coverage", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Curriculum coverage is read-only." });
                    return;
                }
                await WriteJsonAsync(context.Response, _coverage.GetOverview());
                return;
            }

            if (string.Equals(path, "/api/project/readiness", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Project readiness is read-only." });
                    return;
                }
                await WriteJsonAsync(context.Response, _readiness.GetBoard());
                return;
            }

            if (string.Equals(path, "/api/teaching/references", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Teaching references are read-only." });
                    return;
                }
                await WriteJsonAsync(context.Response, _teachingReferences.GetOverview());
                return;
            }

            if (string.Equals(path, "/api/teaching/reference-reviews", StringComparison.OrdinalIgnoreCase))
            {
                if (!ValidTeachingReferenceReviewRequest(context.Request))
                { context.Response.StatusCode = (int)HttpStatusCode.Forbidden; await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin teaching-reference review intent is required." }); return; }
                var result = await ReadTeachingReferenceReviewAsync(context.Request);
                context.Response.StatusCode = TeachingReferenceMutationStatus(result); await WriteJsonAsync(context.Response, result); return;
            }

            if (string.Equals(path, "/api/development/feedback", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var state = context.Request.QueryString["state"];
                        var limit = BoundedQueryInteger(context.Request, "limit", 200, 1, 500);
                        await WriteJsonAsync(context.Response, new { ok = true, feedback = _githubFeedback.GetQueue(state, limit) });
                    }
                    catch (ArgumentException exception)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await WriteJsonAsync(context.Response, new { ok = false, error = exception.Message });
                    }
                    return;
                }
                if (!ValidGitHubFeedbackImportRequest(context.Request))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin GitHub-feedback import intent is required." });
                    return;
                }
                var mutation = await ReadGitHubFeedbackImportAsync(context.Request);
                context.Response.StatusCode = mutation.Ok ? (int)HttpStatusCode.OK : mutation.State == "invalid"
                    ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.InternalServerError;
                await WriteJsonAsync(context.Response, mutation);
                return;
            }

            if (string.Equals(path, "/api/development/breadcrumbs", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    var recordLimit = BoundedQueryInteger(context.Request, "limit", 200, 1, 1000);
                    var issueLimit = BoundedQueryInteger(context.Request, "issueLimit", 20, 1, 100);
                    await WriteJsonAsync(context.Response, new { ok = true, context = _developmentBreadcrumbs.GetContext(recordLimit, issueLimit) });
                    return;
                }
                if (!ValidDevelopmentBreadcrumbMutationRequest(context.Request))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin canonical-breadcrumb intent is required." });
                    return;
                }
                var mutation = await ReadDevelopmentBreadcrumbMutationAsync(context.Request);
                context.Response.StatusCode = DevelopmentBreadcrumbMutationStatus(mutation);
                await WriteJsonAsync(context.Response, mutation);
                return;
            }

            if (string.Equals(path, "/api/development/breadcrumbs/page", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Canonical breadcrumb pages are read-only." });
                    return;
                }
                try
                {
                    var recordLimit = BoundedQueryInteger(context.Request, "limit", 200, 1, 1000);
                    var page = _developmentBreadcrumbs.GetPage(
                        context.Request.QueryString["beforeUtc"],
                        context.Request.QueryString["beforeId"],
                        recordLimit);
                    await WriteJsonAsync(context.Response, new { ok = true, page });
                }
                catch (ArgumentException exception)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context.Response, new { ok = false, error = exception.Message });
                }
                return;
            }

            if (string.Equals(path, "/api/development/events", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(context.Response, new
                    {
                        ok = true,
                        legacy = true,
                        readOnly = true,
                        canonicalPath = "/api/development/breadcrumbs",
                        message = "Legacy source-era development events are preserved for migration evidence. New receipts belong in the canonical breadcrumb ledger.",
                        events = _evidence.GetDevelopmentEvents()
                    });
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.Gone;
                await WriteJsonAsync(context.Response, new
                {
                    ok = false,
                    error = "The legacy development-event write lane is retired. Append immutable receipts through /api/development/breadcrumbs."
                });
                return;
            }

            const string revisionContentPrefix = "/api/curriculum/revisions/";
            const string revisionContentSuffix = "/content";
            if (path.StartsWith(revisionContentPrefix, StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(revisionContentSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var idText = path[revisionContentPrefix.Length..^revisionContentSuffix.Length].Trim('/');
                if (!long.TryParse(idText, out var revisionId) || revisionId <= 0)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "A positive revision id is required." });
                    return;
                }

                var revision = _evidence.GetRevisionBody(revisionId);
                if (revision is null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Revision not found." });
                    return;
                }

                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-MA-Teacher-Source-SHA256"] = revision.Sha256;
                context.Response.ContentLength64 = revision.Body.LongLength;
                await context.Response.OutputStream.WriteAsync(revision.Body, _stopping.Token);
                return;
            }

            if (string.Equals(path, "/api/classroom/status", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Classroom status is read-only." });
                    return;
                }
                await WriteJsonAsync(context.Response, _classroom.GetStatus());
                return;
            }

            if (string.Equals(path, "/api/classroom/invites", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "create-classroom-invite", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent || context.Request.ContentLength64 is < 1 or > 4096)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin classroom-sharing intent is required." });
                    return;
                }
                ClassroomInviteInput? input;
                try
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8, leaveOpen: true);
                    input = JsonSerializer.Deserialize<ClassroomInviteInput>(await reader.ReadToEndAsync(_stopping.Token), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException) { input = null; }
                var result = input is null
                    ? new ClassroomInviteResult(false, "invalid", null, null, null, "The classroom invite form was not readable.")
                    : await _classroom.CreateInviteAsync(input);
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (string.Equals(path, "/api/classroom/stop", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], "stop-classroom-sharing", StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin classroom-stop intent is required." });
                    return;
                }
                await WriteJsonAsync(context.Response, await _classroom.StopAsync());
                return;
            }

            if (string.Equals(path, "/api/printing/status", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Printer status is read-only." });
                    return;
                }
                await WriteJsonAsync(context.Response, _printing.GetOverview());
                return;
            }

            if (string.Equals(path, "/api/printing/approve", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/api/printing/decline", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/api/printing/safety-report", StringComparison.OrdinalIgnoreCase))
            {
                var expectedOrigin = BaseAddress.TrimEnd('/');
                var validOrigin = string.Equals(context.Request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase);
                var expectedIntent = path.EndsWith("/approve", StringComparison.OrdinalIgnoreCase) ? "approve-local-print"
                    : path.EndsWith("/decline", StringComparison.OrdinalIgnoreCase) ? "decline-local-print" : "print-safety-report";
                var validIntent = string.Equals(context.Request.Headers["X-MA-Teacher-Intent"], expectedIntent, StringComparison.Ordinal);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) || !validOrigin || !validIntent || context.Request.ContentLength64 is < 1 or > 4096)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    await WriteJsonAsync(context.Response, new { ok = false, error = "Same-origin teacher print approval is required." });
                    return;
                }
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8, leaveOpen: true)) body = await reader.ReadToEndAsync(_stopping.Token);
                LocalPrintMutation result;
                try
                {
                    if (path.EndsWith("/approve", StringComparison.OrdinalIgnoreCase))
                    {
                        var input = JsonSerializer.Deserialize<LocalPrintApprovalInput>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        result = input is null ? new(false, "invalid", null, "The approval form was not readable.") : await _printing.ApproveAsync(input);
                    }
                    else if (path.EndsWith("/decline", StringComparison.OrdinalIgnoreCase))
                    {
                        var input = JsonSerializer.Deserialize<LocalPrintDeclineInput>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        result = input is null ? new(false, "invalid", null, "The decline form was not readable.") : _printing.Decline(input.RequestId);
                    }
                    else
                    {
                        var input = JsonSerializer.Deserialize<LocalPrinterNameInput>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                        result = input is null ? new(false, "invalid", null, "The printer form was not readable.") : await _printing.PrintSafetyReportAsync(input.PrinterName);
                    }
                }
                catch (JsonException) { result = new(false, "invalid", null, "The print form must be valid JSON."); }
                context.Response.StatusCode = result.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
                await WriteJsonAsync(context.Response, result);
                return;
            }

            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await WriteJsonAsync(context.Response, new { ok = false, error = "API route not found." });
                return;
            }

            var relative = path == "/" ? "index.html" : path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(_uiRoot, relative));
            var insideUiRoot = candidate.StartsWith(_uiRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            if (!insideUiRoot || !File.Exists(candidate))
            {
                if (!insideUiRoot || Path.HasExtension(relative))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                candidate = Path.Combine(_uiRoot, "index.html");
            }

            context.Response.ContentType = GetMimeType(candidate);
            context.Response.ContentLength64 = new FileInfo(candidate).Length;
            await using var staticFileStream = File.OpenRead(candidate);
            await staticFileStream.CopyToAsync(context.Response.OutputStream, _stopping.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            // Never close an API request with an empty body. The Guided Setup
            // page reads several independent local evidence stores together;
            // one route exception previously became an empty HTTP 500 and the
            // browser exposed JavaScript's "Unexpected end of JSON input" text.
            // Keep implementation details local, but always return a valid,
            // child-safe JSON refusal when the response can still be written.
            try
            {
                if (context.Response.OutputStream.CanWrite)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    var failure = new Dictionary<string, object?>
                    {
                        ["ok"] = false,
                        ["error"] = "MA-Teacher could not complete this local request. Press Check again."
                    };
                    if (_includeDiagnosticErrors)
                        failure["diagnostic"] = $"{exception.GetType().Name}: {exception.Message}";
                    await WriteJsonAsync(context.Response, failure);
                }
            }
            catch { }
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static async Task WriteTextAsync(HttpListenerResponse response, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object value)
    {
        response.ContentType = "application/json; charset=utf-8";
        response.Headers["Cache-Control"] = "no-store";
        await WriteTextAsync(response, JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }));
    }

    private static string GetMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream",
    };

    private static int BoundedQueryInteger(HttpListenerRequest request, string name, int fallback, int minimum, int maximum)
        => int.TryParse(request.QueryString[name], out var parsed) ? Math.Clamp(parsed, minimum, maximum) : fallback;

    private bool ValidDevelopmentBreadcrumbMutationRequest(HttpListenerRequest request)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], "append-development-breadcrumb", StringComparison.Ordinal)
            && request.ContentLength64 is > 0 and <= 32768;
    }

    private bool ValidGitHubFeedbackImportRequest(HttpListenerRequest request)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], "import-github-feedback", StringComparison.Ordinal)
            && request.ContentLength64 is > 0 and <= 4194304;
    }

    private async Task<GitHubFeedbackMutation> ReadGitHubFeedbackImportAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<GitHubFeedbackBatchImport>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new GitHubFeedbackMutation(false, "invalid", 0, 0, 0, 0, "A valid feedback batch is required.")
                : _githubFeedback.Import(input);
        }
        catch (JsonException)
        {
            return new GitHubFeedbackMutation(false, "invalid", 0, 0, 0, 0, "Body must be valid JSON.");
        }
    }

    private async Task<DevelopmentBreadcrumbMutation> ReadDevelopmentBreadcrumbMutationAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<DevelopmentBreadcrumbWrite>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null
                ? new DevelopmentBreadcrumbMutation(false, "invalid", false, null, "A valid canonical breadcrumb body is required.")
                : _developmentBreadcrumbs.Append(input);
        }
        catch (JsonException)
        {
            return new DevelopmentBreadcrumbMutation(false, "invalid", false, null, "Body must be valid JSON.");
        }
    }

    private static int DevelopmentBreadcrumbMutationStatus(DevelopmentBreadcrumbMutation result) => result.State switch
    {
        "inserted" => (int)HttpStatusCode.Created,
        "already-present" => (int)HttpStatusCode.OK,
        "conflict" => (int)HttpStatusCode.Conflict,
        "invalid" => (int)HttpStatusCode.BadRequest,
        _ => (int)HttpStatusCode.InternalServerError,
    };

    private bool ValidDatabaseBackupMutationRequest(HttpListenerRequest request, string intent, long maximumBodyBytes)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], intent, StringComparison.Ordinal)
            && request.ContentLength64 is > 0 && request.ContentLength64 <= maximumBodyBytes;
    }

    private async Task<DatabaseBackupMutation> ReadDatabaseBackupMutationAsync<T>(HttpListenerRequest request,
        Func<T, DatabaseBackupMutation> mutation) where T : class
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new DatabaseBackupMutation(false, "invalid", null, null, null, "A valid request body is required.") : mutation(input);
        }
        catch (JsonException) { return new DatabaseBackupMutation(false, "invalid", null, null, null, "Body must be valid JSON."); }
    }

    private static int DatabaseBackupMutationStatus(DatabaseBackupMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict
        : result.State == "capacity-refused" ? (int)HttpStatusCode.InsufficientStorage : (int)HttpStatusCode.BadRequest;

    private async Task<DataStewardshipMutation> ReadDataStewardshipMutationAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<DataRetentionPolicyInput>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new DataStewardshipMutation(false, "invalid", null, "A valid local retention policy is required.") : _dataStewardship.RecordPolicy(input);
        }
        catch (JsonException) { return new DataStewardshipMutation(false, "invalid", null, "Body must be valid JSON."); }
    }

    private static int DataStewardshipMutationStatus(DataStewardshipMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    private async Task<AccessibilityReviewMutation> ReadAccessibilityReviewMutationAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<AccessibilityReviewInput>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new AccessibilityReviewMutation(false, "invalid", null, "A valid accessibility review is required.") : _accessibilityReviews.Record(input);
        }
        catch (JsonException) { return new AccessibilityReviewMutation(false, "invalid", null, "Body must be valid JSON."); }
    }

    private static int AccessibilityReviewMutationStatus(AccessibilityReviewMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    private bool ValidTeachingReferenceReviewRequest(HttpListenerRequest request)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], "review-teaching-reference", StringComparison.Ordinal)
            && request.ContentLength64 is > 0 and <= 16384;
    }

    private async Task<TeachingReferenceMutation> ReadTeachingReferenceReviewAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<TeachingReferenceReviewInput>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new TeachingReferenceMutation(false, "invalid", null, null, "A valid teaching-reference review is required.") : _teachingReferences.ReviewSource(input);
        }
        catch (JsonException) { return new TeachingReferenceMutation(false, "invalid", null, null, "Body must be valid JSON."); }
    }

    private static int TeachingReferenceMutationStatus(TeachingReferenceMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    private bool ValidLessonReviewMutationRequest(HttpListenerRequest request)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], "record-exact-lesson-review", StringComparison.Ordinal)
            && request.ContentLength64 is > 0 and <= 131072;
    }

    private async Task<LessonReviewMutation> ReadLessonReviewMutationAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<LessonReviewInput>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new LessonReviewMutation(false, "invalid", null, null, "A valid lesson review is required.") : _lessonReviews.ReviewLesson(input);
        }
        catch (JsonException) { return new LessonReviewMutation(false, "invalid", null, null, "Body must be valid JSON."); }
    }

    private static int LessonReviewMutationStatus(LessonReviewMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    private async Task<TeachingSessionMutation> ReadTeachingSessionMutationAsync(HttpListenerRequest request)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<TeachingSessionInput>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new TeachingSessionMutation(false, "invalid", null, null, "A valid teaching-session receipt is required.") : _teachingSessions.Record(input);
        }
        catch (JsonException) { return new TeachingSessionMutation(false, "invalid", null, null, "Body must be valid JSON."); }
    }

    private static int TeachingSessionMutationStatus(TeachingSessionMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    private bool ValidTeachingProposalMutationRequest(HttpListenerRequest request, string intent, long maximumBodyBytes)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], intent, StringComparison.Ordinal)
            && request.ContentLength64 is > 0 && request.ContentLength64 <= maximumBodyBytes;
    }

    private async Task<TeachingProposalMutation> ReadTeachingProposalMutationAsync<T>(HttpListenerRequest request,
        Func<T, TeachingProposalMutation> mutation) where T : class
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new TeachingProposalMutation(false, "invalid", null, "A valid request body is required.") : mutation(input);
        }
        catch (JsonException) { return new TeachingProposalMutation(false, "invalid", null, "Body must be valid JSON."); }
    }

    private static int TeachingProposalMutationStatus(TeachingProposalMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    private bool ValidLearningCheckMutationRequest(HttpListenerRequest request, string intent, long maximumBodyBytes)
    {
        var expectedOrigin = BaseAddress.TrimEnd('/');
        return string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["Origin"], expectedOrigin, StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.Headers["X-MA-Teacher-Intent"], intent, StringComparison.Ordinal)
            && request.ContentLength64 is > 0 && request.ContentLength64 <= maximumBodyBytes;
    }

    private async Task<LearningCheckMutation> ReadLearningCheckMutationAsync<T>(HttpListenerRequest request,
        Func<T, LearningCheckMutation> mutation) where T : class
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            var body = await reader.ReadToEndAsync(_stopping.Token);
            var input = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return input is null ? new LearningCheckMutation(false, "invalid", null, "A valid request body is required.") : mutation(input);
        }
        catch (JsonException) { return new LearningCheckMutation(false, "invalid", null, "Body must be valid JSON."); }
    }

    private static int LearningCheckMutationStatus(LearningCheckMutation result) => result.Ok
        ? (int)HttpStatusCode.OK : result.State == "conflict" ? (int)HttpStatusCode.Conflict : (int)HttpStatusCode.BadRequest;

    public void Dispose()
    {
        _classroom.Dispose();
        _stopping.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _stopping.Dispose();
    }

    private sealed record ModuleIdentity(string Id, string Name, int Port);
}
