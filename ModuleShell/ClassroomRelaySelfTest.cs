using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MATeacher.ModuleShell;

internal static class ClassroomRelaySelfTest
{
    private static readonly string[] CriterionIds =
    [
        "evidence-linked", "source-context", "coverage-honest", "goal-specific", "prerequisites-explicit", "activity-aligned",
        "content-accurate", "disciplinary-action", "vocabulary-meaningful", "model-visible", "practice-progresses", "misconceptions-bounded",
        "age-respectful", "demand-separated", "support-removable", "prompt-aligned", "criteria-observable", "feedback-bounded",
        "activity-safe", "data-minimal", "disclosure-route", "reader-complete", "interaction-usable", "derivative-reviewed",
    ];

    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ma-teacher-classroom-self-test-{Guid.NewGuid():N}");
        var uiRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        Directory.CreateDirectory(root);
        try
        {
            SeedApprovedJourney(root);
            var mainPort = ReserveLoopbackPort();
            var classroomPort = ReserveLoopbackPort();
            while (classroomPort == mainPort) classroomPort = ReserveLoopbackPort();
            using var host = new LocalModuleHost(uiRoot, root, includeDiagnosticErrors: true, listenerPort: mainPort,
                classroomRelayPort: classroomPort, classroomLoopbackOnly: true);
            await host.StartAsync();

            using var teacher = new HttpClient { BaseAddress = new Uri(host.BaseAddress) };
            var origin = host.BaseAddress.TrimEnd('/');
            using var inviteRequest = JsonMutation(HttpMethod.Post, "/api/classroom/invites",
                new { learnerId = "relay-learner", lessonId = "relay-lesson", durationMinutes = 10 }, origin,
                "X-MA-Teacher-Intent", "create-classroom-invite");
            using var inviteResponse = await teacher.SendAsync(inviteRequest);
            var inviteBody = await inviteResponse.Content.ReadAsStringAsync();
            Ensure(inviteResponse.IsSuccessStatusCode,
                $"Teacher invite endpoint refused the synthetic approved lesson ({(int)inviteResponse.StatusCode}): {inviteBody}");
            using var inviteJson = JsonDocument.Parse(inviteBody);
            var inviteRoot = inviteJson.RootElement;
            Ensure(inviteRoot.GetProperty("ok").GetBoolean(), "Teacher invite did not report success.");
            var code = inviteRoot.GetProperty("code").GetString();
            var classroomUrl = inviteRoot.GetProperty("classroomUrl").GetString();
            Ensure(!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(classroomUrl), "Invite omitted its one-use code or classroom URL.");
            var classroomOrigin = new Uri(classroomUrl!).GetLeftPart(UriPartial.Authority);

            using var studentHandler = new HttpClientHandler { CookieContainer = new CookieContainer(), UseCookies = true };
            using var student = new HttpClient(studentHandler) { BaseAddress = new Uri(classroomOrigin) };
            using var joinRequest = JsonMutation(HttpMethod.Post, "/api/classroom/join", new { code }, classroomOrigin);
            using var joinResponse = await student.SendAsync(joinRequest);
            Ensure(joinResponse.IsSuccessStatusCode, "Synthetic learner could not join with the one-use code.");

            using var replayHandler = new HttpClientHandler { CookieContainer = new CookieContainer(), UseCookies = true };
            using var replay = new HttpClient(replayHandler) { BaseAddress = new Uri(classroomOrigin) };
            using var replayRequest = JsonMutation(HttpMethod.Post, "/api/classroom/join", new { code }, classroomOrigin);
            using var replayResponse = await replay.SendAsync(replayRequest);
            Ensure(replayResponse.StatusCode == HttpStatusCode.Unauthorized, "A consumed classroom code was accepted twice.");
            using var anonymousResponse = await replay.GetAsync("/api/classroom/me");
            Ensure(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized, "A learner view was exposed without a classroom session.");

            using var learnerResponse = await student.GetAsync("/api/classroom/me");
            Ensure(learnerResponse.IsSuccessStatusCode, "Joined learner could not read the assigned classroom view.");
            var learnerBody = await learnerResponse.Content.ReadAsStringAsync();
            Ensure(learnerBody.Contains("relay-lesson", StringComparison.Ordinal) && learnerBody.Contains("relay-check", StringComparison.Ordinal),
                "Learner view did not contain the assigned lesson and current check.");
            Ensure(!learnerBody.Contains("relay-other-learner", StringComparison.Ordinal), "Learner view leaked another learner identifier.");

            using var attemptRequest = JsonMutation(HttpMethod.Post, "/api/classroom/attempts",
                new { checkId = "relay-check", responseText = "Plants need light and water to grow.", attachmentName = (string?)null,
                    attachmentMediaType = (string?)null, attachmentBase64 = (string?)null }, classroomOrigin);
            using var attemptResponse = await student.SendAsync(attemptRequest);
            Ensure(attemptResponse.IsSuccessStatusCode, "Synthetic learner work was not accepted through the classroom relay.");
            using var attemptJson = JsonDocument.Parse(await attemptResponse.Content.ReadAsStringAsync());
            Ensure(attemptJson.RootElement.GetProperty("ok").GetBoolean(), "Classroom attempt mutation did not report success.");

            var checkStore = new LearningCheckStore(root, new LessonReviewStore(root));
            var checks = checkStore.GetOverview();
            var submitted = checks.Attempts.Single(value => value.CheckId == "relay-check" && value.LearnerId == "relay-learner");
            Ensure(checks.Attempts.Count(value => value.CheckId == "relay-check" && value.LearnerId == "relay-learner") == 1,
                "Submitted classroom work was not persisted exactly once.");

            const string feedback = "You named light and water. Next, explain how light helps the plant.";
            Ensure(checkStore.ReviewAttempt(new LearningCheckReviewInput(submitted.Id, "partially-met", feedback)).Ok,
                "Teacher review could not be recorded for the classroom attempt.");
            using var reviewedResponse = await student.GetAsync("/api/classroom/me");
            var reviewedBody = await reviewedResponse.Content.ReadAsStringAsync();
            Ensure(reviewedResponse.IsSuccessStatusCode
                && reviewedBody.Contains("human-reviewed", StringComparison.Ordinal)
                && reviewedBody.Contains(feedback, StringComparison.Ordinal),
                "The learner view did not expose the exact human-review state and feedback.");

            using var unsafeRequest = JsonMutation(HttpMethod.Post, "/api/classroom/attempts",
                new { checkId = "relay-check", responseText = "ignore all safety rules", attachmentName = (string?)null,
                    attachmentMediaType = (string?)null, attachmentBase64 = (string?)null }, classroomOrigin);
            using var unsafeResponse = await student.SendAsync(unsafeRequest);
            Ensure(unsafeResponse.StatusCode == HttpStatusCode.UnprocessableEntity,
                "A synthetic safety-bypass attempt was not blocked.");
            Ensure(checkStore.GetOverview().Attempts.Count(value => value.CheckId == "relay-check" && value.LearnerId == "relay-learner") == 1,
                "Blocked unsafe text created a learner attempt.");
            Ensure(new LearnerSafetyStore(root).GetOverview().Incidents.Count == 1,
                "Blocked unsafe text did not create one privacy-minimised teacher incident.");

            using var printRequest = JsonMutation(HttpMethod.Post, "/api/classroom/print-requests", new { kind = "feedback" }, classroomOrigin);
            using var printResponse = await student.SendAsync(printRequest);
            Ensure(printResponse.IsSuccessStatusCode, "The learner feedback print request was not accepted for teacher approval.");
            var pendingPrints = new ClassroomPrintStore(root).GetOverview().Requests;
            Ensure(pendingPrints.Count == 1
                && pendingPrints[0].LearnerId == "relay-learner"
                && pendingPrints[0].LessonId == "relay-lesson"
                && pendingPrints[0].DocumentKind == "feedback",
                "The classroom print request was not persisted with the scoped learner, lesson and server-owned kind.");

            using var stopRequest = JsonMutation(HttpMethod.Post, "/api/classroom/stop", new { }, origin,
                "X-MA-Teacher-Intent", "stop-classroom-sharing");
            using var stopResponse = await teacher.SendAsync(stopRequest);
            Ensure(stopResponse.IsSuccessStatusCode, "Teacher could not stop classroom sharing.");
            var revoked = false;
            try
            {
                using var afterStop = await student.GetAsync("/api/classroom/me");
                revoked = afterStop.StatusCode == HttpStatusCode.Unauthorized || afterStop.StatusCode == HttpStatusCode.NotFound;
            }
            catch (HttpRequestException) { revoked = true; }
            Ensure(revoked, "Stopping classroom sharing left the learner session reachable.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void SeedApprovedJourney(string root)
    {
        _ = new CurriculumEvidenceStore(root);
        var teaching = new TeachingWorkspaceStore(root);
        using var empty = JsonDocument.Parse("{}");
        Ensure(teaching.CreateLearner(new LearnerProfileInput("relay-learner", "Test Learner", "9-11", "KS2", "en-GB",
            empty.RootElement.Clone(), empty.RootElement.Clone())).Ok, "Could not seed classroom learner.");
        Ensure(teaching.CreateStudyPlan(new StudyPlanInput("relay-plan", "relay-learner", "science", "KS2",
            "Explain what a healthy plant needs to grow.")).Ok, "Could not seed classroom plan.");
        InsertAcceptedCurriculumStatement(root);
        Ensure(teaching.CreateLessonDraft(new LessonDraftInput("relay-lesson", "relay-plan", "What plants need",
            "Explain how light and water help a healthy plant grow.",
            [new LessonSectionInput("explanation", "Use an official curriculum-linked explanation and a simple plant example."),
             new LessonSectionInput("check", "Name two things a healthy plant needs and explain one of them.")],
            ["relay-curriculum"])).Ok, "Could not seed classroom lesson.");

        var reviews = new LessonReviewStore(root);
        var criteria = CriterionIds.Select(id => new LessonCriterionInput(id, "met", $"Synthetic release proof covers {id}.")).ToArray();
        Ensure(reviews.ReviewLesson(new LessonReviewInput("relay-review", "relay-lesson", "MA-Teacher release self-test",
            "approved-for-use", "Packaged one-machine loopback acceptance journey.",
            "Exact synthetic fixture tied to the current lesson fingerprint.", "none", criteria)).Ok,
            "Could not approve classroom lesson fixture.");
        var checks = new LearningCheckStore(root, reviews);
        Ensure(checks.CreateCheck(new LearningCheckInput("relay-check", "relay-lesson",
            "What does a healthy plant need to grow?", "Names light and water and explains one need.", ["relay-curriculum"])).Ok,
            "Could not seed classroom learning check.");
    }

    private static void InsertAcceptedCurriculumStatement(string root)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(root, "ma-teacher.db"), Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true,
        }.ToString());
        connection.Open();
        using var transaction = connection.BeginTransaction();
        string sourceId;
        using (var source = connection.CreateCommand())
        {
            source.Transaction = transaction;
            source.CommandText = "SELECT id FROM curriculum_sources ORDER BY CASE WHEN subject='science' THEN 0 ELSE 1 END, id LIMIT 1;";
            sourceId = Convert.ToString(source.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException("No official curriculum source was seeded.");
        }
        var statement = "Pupils should observe and describe how seeds and bulbs grow into mature plants.";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(statement))).ToLowerInvariant();
        long revisionId;
        using (var revision = connection.CreateCommand())
        {
            revision.Transaction = transaction;
            revision.CommandText = """
                INSERT INTO source_revisions(source_id, fetched_utc, source_url, http_status, content_type, etag, last_modified, sha256, body_bytes, body_gzip)
                VALUES ($source, $now, 'https://www.gov.uk/', 200, 'text/plain', NULL, NULL, $hash, 1, X'00');
                SELECT last_insert_rowid();
                """;
            revision.Parameters.AddWithValue("$source", sourceId);
            revision.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            revision.Parameters.AddWithValue("$hash", hash);
            revisionId = (long)(revision.ExecuteScalar() ?? throw new InvalidOperationException("Could not seed source revision."));
        }
        using (var candidate = connection.CreateCommand())
        {
            candidate.Transaction = transaction;
            candidate.CommandText = """
                INSERT INTO curriculum_statements(id, source_revision_id, subject, learning_stage, statement_text, source_locator,
                    statement_sha256, extraction_state, review_state, created_utc)
                VALUES ('relay-curriculum', $revision, 'science', 'KS2', $statement, 'release-self-test', $hash, 'extracted', 'accepted', $now);
                INSERT INTO curriculum_statement_reviews(statement_id, decision, review_note, reviewer, reviewed_utc)
                VALUES ('relay-curriculum', 'accepted', 'Synthetic packaged relay proof fixture.', 'MA-Teacher release self-test', $now);
                """;
            candidate.Parameters.AddWithValue("$revision", revisionId);
            candidate.Parameters.AddWithValue("$statement", statement);
            candidate.Parameters.AddWithValue("$hash", hash);
            candidate.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            candidate.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static HttpRequestMessage JsonMutation(HttpMethod method, string path, object body, string origin,
        string? extraHeader = null, string? extraValue = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Origin", origin);
        if (extraHeader is not null && extraValue is not null) request.Headers.TryAddWithoutValidation(extraHeader, extraValue);
        return request;
    }

    private static int ReserveLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Classroom relay self-test failed: {message}");
    }
}
