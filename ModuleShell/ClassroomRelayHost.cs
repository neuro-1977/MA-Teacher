using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MATeacher.ModuleShell;

internal sealed class ClassroomRelayHost : IDisposable
{
    internal const int Port = 5202;
    private const int MaximumRequestBytes = 15 * 1024 * 1024;
    private const int MaximumFailedJoinsPerWindow = 10;
    private static readonly TimeSpan FailedJoinWindow = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly char[] InviteAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray();

    private readonly object _gate = new();
    private readonly string _uiRoot;
    private readonly TeachingWorkspaceStore _teaching;
    private readonly LessonReviewStore _lessonReviews;
    private readonly LearningCheckStore _learningChecks;
    private readonly LearnerSafetyStore _learnerSafety;
    private readonly ClassroomPrintStore _printRequests;
    private readonly int _port;
    private readonly bool _loopbackOnly;
    private readonly Dictionary<string, ClassroomInvite> _invites = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClassroomSession> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FailedJoinBucket> _failedJoins = new(StringComparer.OrdinalIgnoreCase);
    private HttpListener? _listener;
    private CancellationTokenSource? _stopping;
    private Task? _serveTask;
    private string? _lastError;

    internal ClassroomRelayHost(
        string uiRoot,
        TeachingWorkspaceStore teaching,
        LessonReviewStore lessonReviews,
        LearningCheckStore learningChecks,
        LearnerSafetyStore learnerSafety,
        ClassroomPrintStore printRequests,
        int? listenerPort = null,
        bool loopbackOnly = false)
    {
        _uiRoot = Path.GetFullPath(uiRoot);
        _teaching = teaching;
        _lessonReviews = lessonReviews;
        _learningChecks = learningChecks;
        _learnerSafety = learnerSafety;
        _printRequests = printRequests;
        _port = listenerPort is > 0 and <= 65535 ? listenerPort.Value : Port;
        _loopbackOnly = loopbackOnly;
    }

    internal ClassroomRelayStatus GetStatus()
    {
        lock (_gate)
        {
            PruneExpiredLocked(DateTimeOffset.UtcNow);
            return new ClassroomRelayStatus(
                true,
                _listener?.IsListening == true,
                BuildClassroomUrl(),
                _invites.Count,
                _sessions.Count,
                _lastError,
                _learnerSafety.GetOverview().Incidents,
                _invites.Values
                    .OrderBy(value => value.ExpiresUtc)
                    .Select(value => new ClassroomInviteStatus(value.LearnerId, value.LessonId, value.ExpiresUtc, value.Consumed))
                    .ToArray(),
                new[]
                {
                    "The classroom link starts only when a teacher creates an invite.",
                    "Only private local-network callers can reach the relay.",
                    "The relay exposes assigned lessons, current checks, that learner's attempts and submission only.",
                    "Plain local HTTP needs a managed, isolated school network; it does not protect against an active network attacker."
                });
        }
    }

    internal async Task<ClassroomInviteResult> CreateInviteAsync(ClassroomInviteInput input)
    {
        var learnerId = Bound(input.LearnerId, 160);
        var lessonId = Bound(input.LessonId, 160);
        if (string.IsNullOrWhiteSpace(learnerId) || string.IsNullOrWhiteSpace(lessonId))
        {
            return new(false, "invalid", null, null, null, "Choose one learner and one approved lesson.");
        }

        var lesson = _teaching.GetLessonDetail(lessonId);
        if (!lesson.Ok || lesson.Lesson is null || !string.Equals(lesson.Lesson.LearnerId, learnerId, StringComparison.Ordinal))
        {
            return new(false, "invalid", null, null, null, "The selected lesson does not belong to the selected learner.");
        }

        var review = _lessonReviews.GetOverview().Lessons.FirstOrDefault(value => string.Equals(value.Id, lessonId, StringComparison.Ordinal));
        if (review is null || !review.LatestReviewCurrent || !string.Equals(review.LatestDecision, "approved-for-use", StringComparison.OrdinalIgnoreCase))
        {
            return new(false, "not-approved", null, null, null, "The lesson needs a current approved-for-use review before it can be shared.");
        }

        var started = await EnsureStartedAsync().ConfigureAwait(false);
        if (!started)
        {
            return new(false, "listener-unavailable", null, null, null, _lastError ?? "The classroom listener could not start.");
        }

        var duration = Math.Clamp(input.DurationMinutes <= 0 ? 60 : input.DurationMinutes, 5, 240);
        var code = CreateInviteCode();
        var expires = DateTimeOffset.UtcNow.AddMinutes(duration);
        lock (_gate)
        {
            PruneExpiredLocked(DateTimeOffset.UtcNow);
            _invites[HashSecret(NormalizeInviteCode(code))] = new ClassroomInvite(learnerId, lessonId, expires, false);
        }

        return new(true, "ready", BuildClassroomUrl(), code, expires, null);
    }

    internal async Task<ClassroomStopResult> StopAsync()
    {
        HttpListener? listener;
        CancellationTokenSource? stopping;
        Task? serveTask;
        lock (_gate)
        {
            listener = _listener;
            stopping = _stopping;
            serveTask = _serveTask;
            _listener = null;
            _stopping = null;
            _serveTask = null;
            _invites.Clear();
            _sessions.Clear();
            _failedJoins.Clear();
        }

        try { stopping?.Cancel(); } catch { }
        try { listener?.Stop(); } catch { }
        try { listener?.Close(); } catch { }
        if (serveTask is not null)
        {
            try { await serveTask.ConfigureAwait(false); } catch { }
        }
        stopping?.Dispose();
        return new(true, "stopped", "Classroom sharing stopped. All invites and learner sessions were revoked.");
    }

    private Task<bool> EnsureStartedAsync()
    {
        lock (_gate)
        {
            if (_listener?.IsListening == true) return Task.FromResult(true);
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add(_loopbackOnly ? $"http://127.0.0.1:{_port}/" : $"http://+:{_port}/");
                listener.Start();
                var stopping = new CancellationTokenSource();
                _listener = listener;
                _stopping = stopping;
                _lastError = null;
                _serveTask = ServeAsync(listener, stopping.Token);
                return Task.FromResult(true);
            }
            catch (Exception exception) when (exception is HttpListenerException or InvalidOperationException)
            {
                _lastError = $"Classroom sharing could not open port {_port}. Ask school IT to allow MA-Teacher on Private/Domain networks and reserve the local URL. {exception.Message}";
                return Task.FromResult(false);
            }
        }
    }

    private async Task ServeAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || !listener.IsListening) { break; }
            catch (ObjectDisposedException) { break; }

            try { await HandleAsync(context, cancellationToken).ConfigureAwait(false); }
            catch (Exception)
            {
                if (!context.Response.OutputStream.CanWrite) continue;
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteJsonAsync(context.Response, new { ok = false, error = "The classroom request could not be completed." }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                try { context.Response.Close(); } catch { }
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        AddSecurityHeaders(context.Response);
        if (!IsPrivateNetworkAddress(context.Request.RemoteEndPoint?.Address))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "Classroom sharing is available only on the private local network." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var path = context.Request.Url?.AbsolutePath ?? "/";
        if (string.Equals(path, "/api/classroom/join", StringComparison.OrdinalIgnoreCase))
        {
            await HandleJoinAsync(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (path.StartsWith("/api/classroom/", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetSession(context.Request, out var session))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await WriteJsonAsync(context.Response, new { ok = false, error = "Join this classroom again to continue." }, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (string.Equals(path, "/api/classroom/me", StringComparison.OrdinalIgnoreCase) && string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context.Response, BuildLearnerView(session), cancellationToken).ConfigureAwait(false);
                return;
            }
            if (string.Equals(path, "/api/classroom/attempts", StringComparison.OrdinalIgnoreCase))
            {
                await HandleAttemptAsync(context, session, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (string.Equals(path, "/api/classroom/logout", StringComparison.OrdinalIgnoreCase))
            {
                await HandleLogoutAsync(context, session, cancellationToken).ConfigureAwait(false);
                return;
            }
            if (string.Equals(path, "/api/classroom/print-requests", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePrintRequestAsync(context, session, cancellationToken).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteJsonAsync(context.Response, new { ok = false, error = "That classroom action does not exist." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }

        if (string.Equals(path, "/classroom", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/classroom/", StringComparison.OrdinalIgnoreCase))
        {
            await WriteStaticAsync(context.Response, Path.Combine(_uiRoot, "index.html"), cancellationToken).ConfigureAwait(false);
            return;
        }
        if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
        {
            var relative = Uri.UnescapeDataString(path.TrimStart('/')).Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(_uiRoot, relative));
            if (!candidate.StartsWith(_uiRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            await WriteStaticAsync(context.Response, candidate, cancellationToken).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
    }

    private async Task HandleJoinAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!ValidMutationRequest(context.Request, 4096))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "Use the classroom join page to enter a code." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var remoteKey = context.Request.RemoteEndPoint?.Address.ToString() ?? "unknown";
        if (IsJoinRateLimited(remoteKey, DateTimeOffset.UtcNow))
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await WriteJsonAsync(context.Response, new { ok = false, error = "Too many incorrect codes. Wait five minutes, then ask your teacher." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        ClassroomJoinInput? input;
        try { input = await ReadJsonAsync<ClassroomJoinInput>(context.Request, cancellationToken).ConfigureAwait(false); }
        catch (JsonException) { input = null; }
        var codeHash = input is null ? string.Empty : HashSecret(NormalizeInviteCode(input.Code));
        ClassroomInvite? invite = null;
        lock (_gate)
        {
            PruneExpiredLocked(DateTimeOffset.UtcNow);
            if (_invites.TryGetValue(codeHash, out var found) && !found.Consumed && found.ExpiresUtc > DateTimeOffset.UtcNow)
            {
                invite = found with { Consumed = true };
                _invites[codeHash] = invite;
            }
        }

        if (invite is null)
        {
            RecordFailedJoin(remoteKey, DateTimeOffset.UtcNow);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await WriteJsonAsync(context.Response, new { ok = false, error = "That code is not ready. Check it with your teacher." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = new ClassroomSession(HashSecret(token), invite.LearnerId, invite.LessonId, invite.ExpiresUtc);
        lock (_gate) _sessions[session.TokenHash] = session;
        context.Response.Headers.Add("Set-Cookie", $"ma_teacher_classroom={token}; Path=/; HttpOnly; SameSite=Strict; Max-Age={Math.Max(1, (int)(invite.ExpiresUtc - DateTimeOffset.UtcNow).TotalSeconds)}");
        await WriteJsonAsync(context.Response, new { ok = true, state = "joined", expiresUtc = invite.ExpiresUtc }, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleAttemptAsync(HttpListenerContext context, ClassroomSession session, CancellationToken cancellationToken)
    {
        if (!ValidMutationRequest(context.Request, MaximumRequestBytes))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "Use the classroom work form to send work." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        ClassroomAttemptInput? input;
        try { input = await ReadJsonAsync<ClassroomAttemptInput>(context.Request, cancellationToken).ConfigureAwait(false); }
        catch (JsonException) { input = null; }
        if (input is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteJsonAsync(context.Response, new { ok = false, error = "The work form was not readable." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var safetyInput = string.Join('\n', new[] { input.ResponseText ?? string.Empty, input.AttachmentName ?? string.Empty });
        var safety = LearnerSafetyPolicy.EvaluateSubmission(safetyInput);
        if (!safety.Allowed)
        {
            var incident = _learnerSafety.Record(session.LearnerId, session.LessonId, "classroom-submission", safetyInput, safety);
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            await WriteJsonAsync(context.Response, new { ok = false, state = "blocked-and-reported", incidentId = incident.Id, error = safety.LearnerMessage }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var checks = _learningChecks.GetOverview().Checks;
        var check = checks.FirstOrDefault(value => value.FingerprintCurrent
            && string.Equals(value.Id, input.CheckId, StringComparison.Ordinal)
            && string.Equals(value.LessonId, session.LessonId, StringComparison.Ordinal)
            && string.Equals(value.LearnerId, session.LearnerId, StringComparison.Ordinal));
        if (check is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "That practice check is not part of this classroom link." }, cancellationToken).ConfigureAwait(false);
            return;
        }

        var mutation = _learningChecks.SubmitAttempt(new LearningCheckAttemptInput(
            $"classroom-attempt-{Guid.NewGuid():N}",
            check.Id,
            session.LearnerId,
            input.ResponseText ?? string.Empty,
            input.AttachmentName,
            input.AttachmentMediaType,
            input.AttachmentBase64));
        context.Response.StatusCode = mutation.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
        await WriteJsonAsync(context.Response, mutation, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleLogoutAsync(HttpListenerContext context, ClassroomSession session, CancellationToken cancellationToken)
    {
        if (!ValidMutationRequest(context.Request, 1024))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "Use the classroom sign-out button." }, cancellationToken).ConfigureAwait(false);
            return;
        }
        lock (_gate) _sessions.Remove(session.TokenHash);
        context.Response.Headers.Add("Set-Cookie", "ma_teacher_classroom=; Path=/; HttpOnly; SameSite=Strict; Max-Age=0");
        await WriteJsonAsync(context.Response, new { ok = true, state = "signed-out" }, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandlePrintRequestAsync(HttpListenerContext context, ClassroomSession session, CancellationToken cancellationToken)
    {
        if (!ValidMutationRequest(context.Request, 1024))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "Use the classroom print-request button." }, cancellationToken).ConfigureAwait(false);
            return;
        }
        ClassroomPrintRequestInput? input;
        try { input = await ReadJsonAsync<ClassroomPrintRequestInput>(context.Request, cancellationToken).ConfigureAwait(false); }
        catch (JsonException) { input = null; }
        if (input is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteJsonAsync(context.Response, new { ok = false, error = "The print request was not readable." }, cancellationToken).ConfigureAwait(false);
            return;
        }
        var lesson = _teaching.GetLessonDetail(session.LessonId);
        if (!lesson.Ok || lesson.Lesson is null || lesson.Lesson.LearnerId != session.LearnerId)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await WriteJsonAsync(context.Response, new { ok = false, error = "The assigned lesson is no longer available." }, cancellationToken).ConfigureAwait(false);
            return;
        }
        var mutation = _printRequests.Request(session.LearnerId, session.LessonId, input.Kind);
        context.Response.StatusCode = mutation.Ok ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest;
        await WriteJsonAsync(context.Response, mutation, cancellationToken).ConfigureAwait(false);
    }

    private object BuildLearnerView(ClassroomSession session)
    {
        var lesson = _teaching.GetLessonDetail(session.LessonId);
        if (!lesson.Ok || lesson.Lesson is null || !string.Equals(lesson.Lesson.LearnerId, session.LearnerId, StringComparison.Ordinal))
        {
            return new { ok = false, error = "The assigned lesson is no longer available." };
        }
        var overview = _learningChecks.GetOverview();
        var checks = overview.Checks.Where(value => value.FingerprintCurrent
            && string.Equals(value.LessonId, session.LessonId, StringComparison.Ordinal)
            && string.Equals(value.LearnerId, session.LearnerId, StringComparison.Ordinal)).ToArray();
        var checkIds = checks.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
        var attempts = overview.Attempts.Where(value => string.Equals(value.LearnerId, session.LearnerId, StringComparison.Ordinal)
            && checkIds.Contains(value.CheckId)).ToArray();
        return new
        {
            ok = true,
            expiresUtc = session.ExpiresUtc,
            learner = new { id = lesson.Lesson.LearnerId, name = lesson.Lesson.LearnerDisplayName },
            lesson = new
            {
                id = lesson.Lesson.Id,
                title = lesson.Lesson.Title,
                goal = lesson.Lesson.LearningObjective,
                subject = lesson.Lesson.Subject,
                stage = lesson.Lesson.LearningStage,
                sections = lesson.Sections
            },
            checks,
            attempts,
            printRequests = _printRequests.GetOverview().Requests.Where(value => value.LearnerId == session.LearnerId && value.LessonId == session.LessonId).ToArray(),
            boundaries = new[] { "Your teacher reviews every answer.", "MA-Teacher does not invent a mark.", "Only work for this lesson is visible here." }
        };
    }

    private bool TryGetSession(HttpListenerRequest request, out ClassroomSession session)
    {
        session = default!;
        var token = request.Cookies["ma_teacher_classroom"]?.Value;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128) return false;
        var tokenHash = HashSecret(token);
        lock (_gate)
        {
            PruneExpiredLocked(DateTimeOffset.UtcNow);
            return _sessions.TryGetValue(tokenHash, out session!);
        }
    }

    private bool ValidMutationRequest(HttpListenerRequest request, long maximumBodyBytes)
    {
        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            || request.ContentLength64 < 0
            || request.ContentLength64 > maximumBodyBytes
            || !string.Equals(request.ContentType?.Split(';', 2)[0].Trim(), "application/json", StringComparison.OrdinalIgnoreCase)) return false;
        var expected = request.Url?.GetLeftPart(UriPartial.Authority);
        return !string.IsNullOrWhiteSpace(expected)
            && string.Equals(request.Headers["Origin"], expected, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpListenerRequest request, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync<T>(request.InputStream, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, object value, CancellationToken cancellationToken)
    {
        response.ContentType = "application/json; charset=utf-8";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteStaticAsync(HttpListenerResponse response, string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }
        response.ContentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream"
        };
        response.Headers["Cache-Control"] = Path.GetExtension(path).Equals(".html", StringComparison.OrdinalIgnoreCase) ? "no-store" : "public, max-age=31536000, immutable";
        var info = new FileInfo(path);
        response.ContentLength64 = info.Length;
        await using var stream = File.OpenRead(path);
        await stream.CopyToAsync(response.OutputStream, cancellationToken).ConfigureAwait(false);
    }

    private static void AddSecurityHeaders(HttpListenerResponse response)
    {
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' blob:; worker-src blob:; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    }

    private bool IsJoinRateLimited(string remoteKey, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_failedJoins.TryGetValue(remoteKey, out var bucket) || now - bucket.WindowStartedUtc >= FailedJoinWindow) return false;
            return bucket.Failures >= MaximumFailedJoinsPerWindow;
        }
    }

    private void RecordFailedJoin(string remoteKey, DateTimeOffset now)
    {
        lock (_gate)
        {
            if (!_failedJoins.TryGetValue(remoteKey, out var bucket) || now - bucket.WindowStartedUtc >= FailedJoinWindow)
                _failedJoins[remoteKey] = new FailedJoinBucket(now, 1);
            else
                _failedJoins[remoteKey] = bucket with { Failures = bucket.Failures + 1 };
        }
    }

    private void PruneExpiredLocked(DateTimeOffset now)
    {
        foreach (var key in _invites.Where(pair => pair.Value.ExpiresUtc <= now).Select(pair => pair.Key).ToArray()) _invites.Remove(key);
        foreach (var key in _sessions.Where(pair => pair.Value.ExpiresUtc <= now).Select(pair => pair.Key).ToArray()) _sessions.Remove(key);
        foreach (var key in _failedJoins.Where(pair => now - pair.Value.WindowStartedUtc >= FailedJoinWindow).Select(pair => pair.Key).ToArray()) _failedJoins.Remove(key);
    }

    private static string CreateInviteCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        var chars = new char[14];
        for (var index = 0; index < 12; index++) chars[index + index / 4] = InviteAlphabet[bytes[index] % InviteAlphabet.Length];
        chars[4] = '-';
        chars[9] = '-';
        return new string(chars);
    }

    private static string NormalizeInviteCode(string? value) => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private static string HashSecret(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Bound(string? value, int maximum) => (value ?? string.Empty).Trim() is var clean && clean.Length <= maximum ? clean : clean[..maximum];

    private static bool IsPrivateNetworkAddress(IPAddress? address)
    {
        if (address is null) return false;
        if (IPAddress.IsLoopback(address)) return true;
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv4MappedToIPv6) return IsPrivateNetworkAddress(address.MapToIPv4());
            var bytes = address.GetAddressBytes();
            return address.IsIPv6LinkLocal || (bytes[0] & 0xfe) == 0xfc;
        }
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        var octets = address.GetAddressBytes();
        return octets[0] == 10
            || octets[0] == 127
            || (octets[0] == 169 && octets[1] == 254)
            || (octets[0] == 172 && octets[1] is >= 16 and <= 31)
            || (octets[0] == 192 && octets[1] == 168);
    }

    private static string? PreferredLanAddress()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(value => value.OperationalStatus == OperationalStatus.Up && value.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
            .SelectMany(value => value.GetIPProperties().UnicastAddresses.Select(address => new { value.NetworkInterfaceType, Address = address.Address, HasGateway = value.GetIPProperties().GatewayAddresses.Count > 0 }))
            .Where(value => value.Address.AddressFamily == AddressFamily.InterNetwork && IsPrivateNetworkAddress(value.Address) && !IPAddress.IsLoopback(value.Address))
            .OrderByDescending(value => value.HasGateway)
            .ThenBy(value => value.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 0 : 1)
            .ToArray();
        return candidates.FirstOrDefault()?.Address.ToString();
    }

    private string? BuildClassroomUrl()
    {
        if (_loopbackOnly) return $"http://127.0.0.1:{_port}/classroom";
        return PreferredLanAddress() is { } address ? $"http://{address}:{_port}/classroom" : null;
    }

    public void Dispose() => StopAsync().GetAwaiter().GetResult();

    private sealed record ClassroomInvite(string LearnerId, string LessonId, DateTimeOffset ExpiresUtc, bool Consumed);
    private sealed record ClassroomSession(string TokenHash, string LearnerId, string LessonId, DateTimeOffset ExpiresUtc);
    private sealed record FailedJoinBucket(DateTimeOffset WindowStartedUtc, int Failures);
}

internal sealed record ClassroomInviteInput(string LearnerId, string LessonId, int DurationMinutes);
internal sealed record ClassroomJoinInput(string Code);
internal sealed record ClassroomAttemptInput(string CheckId, string? ResponseText, string? AttachmentName, string? AttachmentMediaType, string? AttachmentBase64);
internal sealed record ClassroomInviteStatus(string LearnerId, string LessonId, DateTimeOffset ExpiresUtc, bool Consumed);
internal sealed record ClassroomRelayStatus(bool Ok, bool Running, string? ClassroomUrl, int ActiveInvites, int ConnectedLearners, string? Error, IReadOnlyList<LearnerSafetyIncident> SafetyIncidents, IReadOnlyList<ClassroomInviteStatus> Invites, IReadOnlyList<string> Boundaries);
internal sealed record ClassroomInviteResult(bool Ok, string State, string? ClassroomUrl, string? Code, DateTimeOffset? ExpiresUtc, string? Error);
internal sealed record ClassroomStopResult(bool Ok, string State, string Message);
