using Microsoft.Data.Sqlite;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MATeacher.ModuleShell;

internal sealed class TeachingWorkspaceStore
{
    private const int SchemaVersion = 4;
    private static readonly Regex UnsafeBlockRegex = new("<(script|style|noscript)\\b[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));
    private static readonly Regex CandidateNodeRegex = new("<(h[1-6]|li|p)\\b[^>]*>(.*?)</(h[1-6]|li|p)>", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Singleline, TimeSpan.FromSeconds(2));
    private static readonly Regex WhiteSpaceRegex = new("\\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static readonly Regex CandidateBoundaryRegex = new("(?<=[.!?;:])\\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
    private static readonly string[] CandidatePhrases =
    {
        "pupils should", "students should", "should be taught", "should know", "should understand",
        "should be able", "aims to ensure", "attainment target", "programme of study"
    };
    private readonly string _connectionString;

    public TeachingWorkspaceStore(string dataRoot)
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

    public TeachingWorkspaceOverview GetOverview()
    {
        using var connection = OpenConnection();
        return new TeachingWorkspaceOverview(
            true,
            "install-root-sqlite",
            SchemaVersion,
            Count(connection, "learner_profiles"),
            Count(connection, "study_plans"),
            Count(connection, "curriculum_statements"),
            Count(connection, "lesson_records"),
            Count(connection, "assessment_attempts"),
            ReadPrinciples(connection),
            ReadGates(connection),
            new[]
            {
                "No curriculum statement is authoritative without a captured source revision and locator.",
                "Generated explanations are teaching material, not statutory curriculum evidence.",
                "Lessons and assessments remain locked until evidence, age or level, and safeguarding gates are satisfied.",
                "Schema availability does not imply learner, lesson, assessment, or tutor capability."
            });
    }

    public LearningWorkspace GetWorkspace()
    {
        using var connection = OpenConnection();
        return new LearningWorkspace(true, ReadLearners(connection), ReadStudyPlans(connection),
            ReadLessonDrafts(connection), ReadWorkspaceEvents(connection));
    }

    public TeachingMutationResult CreateLearner(LearnerProfileInput input)
    {
        try
        {
            var id = RequireIdentifier(input.Id, "learner id");
            var displayName = RequireText(input.DisplayName, "display name", 1, 80);
            var ageBand = RequireText(input.AgeBand, "age band", 1, 40);
            var learningStage = RequireText(input.LearningStage, "learning stage", 1, 80);
            var locale = NormalizeLocale(input.Locale);
            var accessibility = NormalizeObjectJson(input.Accessibility, "accessibility");
            var preferences = NormalizeObjectJson(input.Preferences, "preferences");
            var now = DateTimeOffset.UtcNow.ToString("O");

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT display_name, age_band, learning_stage, locale, accessibility_json, preferences_json
                    FROM learner_profiles WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id);
                using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    var same = reader.GetString(0) == displayName && reader.GetString(1) == ageBand
                        && reader.GetString(2) == learningStage && reader.GetString(3) == locale
                        && reader.GetString(4) == accessibility && reader.GetString(5) == preferences;
                    transaction.Rollback();
                    return same
                        ? new TeachingMutationResult(true, "already-present", id, null)
                        : new TeachingMutationResult(false, "conflict", id, "Learner id already exists with different content.");
                }
            }

            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO learner_profiles(id, display_name, age_band, learning_stage, locale,
                        accessibility_json, preferences_json, created_utc, updated_utc)
                    VALUES ($id, $displayName, $ageBand, $learningStage, $locale,
                        $accessibility, $preferences, $now, $now);
                    """;
                insert.Parameters.AddWithValue("$id", id);
                insert.Parameters.AddWithValue("$displayName", displayName);
                insert.Parameters.AddWithValue("$ageBand", ageBand);
                insert.Parameters.AddWithValue("$learningStage", learningStage);
                insert.Parameters.AddWithValue("$locale", locale);
                insert.Parameters.AddWithValue("$accessibility", accessibility);
                insert.Parameters.AddWithValue("$preferences", preferences);
                insert.Parameters.AddWithValue("$now", now);
                insert.ExecuteNonQuery();
            }
            RecordWorkspaceEvent(connection, transaction, "learner", id, "created", now);
            transaction.Commit();
            return new TeachingMutationResult(true, "created", id, null);
        }
        catch (ArgumentException exception)
        {
            return new TeachingMutationResult(false, "invalid", null, exception.Message);
        }
    }

    public TeachingMutationResult CreateStudyPlan(StudyPlanInput input)
    {
        try
        {
            var id = RequireIdentifier(input.Id, "study plan id");
            var learnerId = RequireIdentifier(input.LearnerId, "learner id");
            var subject = NormalizeSubject(input.Subject);
            var learningStage = RequireText(input.LearningStage, "learning stage", 1, 80);
            var goal = RequireText(input.Goal, "goal", 3, 500);
            var now = DateTimeOffset.UtcNow.ToString("O");

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var learner = connection.CreateCommand())
            {
                learner.Transaction = transaction;
                learner.CommandText = "SELECT COUNT(*) FROM learner_profiles WHERE id=$id AND archived_utc IS NULL;";
                learner.Parameters.AddWithValue("$id", learnerId);
                if (Convert.ToInt32(learner.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != 1)
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(false, "invalid", id, "Learner does not exist or is archived.");
                }
            }

            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = "SELECT learner_id, subject, learning_stage, goal, status FROM study_plans WHERE id=$id;";
                existing.Parameters.AddWithValue("$id", id);
                using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    var same = reader.GetString(0) == learnerId && reader.GetString(1) == subject
                        && reader.GetString(2) == learningStage && reader.GetString(3) == goal
                        && reader.GetString(4) == "active";
                    transaction.Rollback();
                    return same
                        ? new TeachingMutationResult(true, "already-present", id, null)
                        : new TeachingMutationResult(false, "conflict", id, "Study plan id already exists with different content.");
                }
            }

            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO study_plans(id, learner_id, subject, learning_stage, goal, status, created_utc, updated_utc)
                    VALUES ($id, $learnerId, $subject, $learningStage, $goal, 'active', $now, $now);
                    """;
                insert.Parameters.AddWithValue("$id", id);
                insert.Parameters.AddWithValue("$learnerId", learnerId);
                insert.Parameters.AddWithValue("$subject", subject);
                insert.Parameters.AddWithValue("$learningStage", learningStage);
                insert.Parameters.AddWithValue("$goal", goal);
                insert.Parameters.AddWithValue("$now", now);
                insert.ExecuteNonQuery();
            }
            RecordWorkspaceEvent(connection, transaction, "study-plan", id, "created", now);
            transaction.Commit();
            return new TeachingMutationResult(true, "created", id, null);
        }
        catch (ArgumentException exception)
        {
            return new TeachingMutationResult(false, "invalid", null, exception.Message);
        }
    }

    public LessonDetailResult GetLessonDetail(string lessonId)
    {
        try
        {
            var id = RequireIdentifier(lessonId, "lesson id");
            using var connection = OpenConnection();
            LessonDetailHeader header;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT l.id, l.study_plan_id, l.title, l.learning_objective, l.evidence_state, l.status,
                           l.created_utc, l.updated_utc, p.learner_id, p.subject, p.learning_stage, p.goal,
                           learner.display_name
                    FROM lesson_records l
                    JOIN study_plans p ON p.id=l.study_plan_id
                    JOIN learner_profiles learner ON learner.id=p.learner_id
                    WHERE l.id=$id;
                    """;
                command.Parameters.AddWithValue("$id", id);
                using var reader = command.ExecuteReader();
                if (!reader.Read()) return new LessonDetailResult(false, null, Array.Empty<LessonDetailSection>(),
                    Array.Empty<LessonDetailEvidence>(), "Lesson does not exist.");
                header = new LessonDetailHeader(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
                    reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetString(12));
            }

            var sections = new List<LessonDetailSection>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT sequence, section_kind, content FROM lesson_sections
                    WHERE lesson_id=$lessonId ORDER BY sequence;
                    """;
                command.Parameters.AddWithValue("$lessonId", id);
                using var reader = command.ExecuteReader();
                while (reader.Read()) sections.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }

            var evidence = new List<LessonDetailEvidence>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT c.id, c.source_revision_id, c.subject, c.learning_stage, c.statement_text,
                           c.source_locator, c.statement_sha256, c.review_state, le.evidence_role
                    FROM lesson_evidence le
                    JOIN curriculum_statements c ON c.id=le.curriculum_statement_id
                    WHERE le.lesson_id=$lessonId
                    ORDER BY c.subject, c.learning_stage, c.id;
                    """;
                command.Parameters.AddWithValue("$lessonId", id);
                using var reader = command.ExecuteReader();
                while (reader.Read()) evidence.Add(new(reader.GetString(0), reader.GetInt64(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)));
            }
            return new LessonDetailResult(true, header, sections, evidence, null);
        }
        catch (ArgumentException exception)
        {
            return new LessonDetailResult(false, null, Array.Empty<LessonDetailSection>(), Array.Empty<LessonDetailEvidence>(), exception.Message);
        }
    }

    public TeachingMutationResult CreateLessonDraft(LessonDraftInput input)
    {
        try
        {
            var id = RequireIdentifier(input.Id, "lesson id");
            var studyPlanId = RequireIdentifier(input.StudyPlanId, "study plan id");
            var title = RequireText(input.Title, "lesson title", 3, 160);
            var objective = RequireText(input.LearningObjective, "learning objective", 10, 500);
            var sections = NormalizeLessonSections(input.Sections);
            var candidateIds = (input.CurriculumCandidateIds ?? Array.Empty<string>())
                .Select(value => RequireIdentifier(value, "curriculum candidate id"))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (candidateIds.Length is < 1 or > 20)
                throw new ArgumentException("lesson draft requires 1-20 accepted curriculum candidates.");
            var contentJson = System.Text.Json.JsonSerializer.Serialize(sections);
            var now = DateTimeOffset.UtcNow.ToString("O");

            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            string planSubject;
            string planStage;
            using (var plan = connection.CreateCommand())
            {
                plan.Transaction = transaction;
                plan.CommandText = "SELECT subject, learning_stage FROM study_plans WHERE id=$id AND status='active';";
                plan.Parameters.AddWithValue("$id", studyPlanId);
                using var reader = plan.ExecuteReader();
                if (!reader.Read())
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(false, "invalid", id, "Active study plan does not exist.");
                }
                planSubject = reader.GetString(0);
                planStage = reader.GetString(1);
            }

            var candidateEvidence = new List<LessonCandidateEvidence>();
            foreach (var candidateId in candidateIds)
            {
                using var candidate = connection.CreateCommand();
                candidate.Transaction = transaction;
                candidate.CommandText = """
                    SELECT subject, learning_stage, review_state
                    FROM curriculum_statements WHERE id=$id;
                    """;
                candidate.Parameters.AddWithValue("$id", candidateId);
                using var reader = candidate.ExecuteReader();
                if (!reader.Read() || reader.GetString(2) != "accepted")
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(false, "invalid", id, $"Curriculum candidate {candidateId} is missing or not accepted.");
                }
                var candidateSubject = reader.GetString(0);
                var candidateStage = reader.GetString(1);
                if (!LessonSubjectCompatible(planSubject, candidateSubject))
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(false, "invalid", id, $"Curriculum candidate {candidateId} does not match the study-plan subject.");
                }
                if (!LessonStageCompatible(planStage, candidateStage))
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(false, "invalid", id, $"Curriculum candidate {candidateId} does not establish stage compatibility.");
                }
                candidateEvidence.Add(new LessonCandidateEvidence(candidateId, candidateSubject, candidateStage));
            }

            var existingLesson = false;
            var existingContentMatches = false;
            using (var existing = connection.CreateCommand())
            {
                existing.Transaction = transaction;
                existing.CommandText = """
                    SELECT study_plan_id, title, learning_objective, teaching_content, evidence_state, status
                    FROM lesson_records WHERE id=$id;
                    """;
                existing.Parameters.AddWithValue("$id", id);
                using var reader = existing.ExecuteReader();
                if (reader.Read())
                {
                    existingLesson = true;
                    existingContentMatches = reader.GetString(0) == studyPlanId && reader.GetString(1) == title
                        && reader.GetString(2) == objective && reader.GetString(3) == contentJson
                        && reader.GetString(4) == "curriculum-linked-subject-facts-unverified"
                        && reader.GetString(5) == "draft";
                }
            }
            if (existingLesson)
            {
                var existingCandidateIds = new List<string>();
                using (var evidence = connection.CreateCommand())
                {
                    evidence.Transaction = transaction;
                    evidence.CommandText = """
                        SELECT curriculum_statement_id FROM lesson_evidence
                        WHERE lesson_id=$lessonId ORDER BY curriculum_statement_id;
                        """;
                    evidence.Parameters.AddWithValue("$lessonId", id);
                    using var reader = evidence.ExecuteReader();
                    while (reader.Read()) existingCandidateIds.Add(reader.GetString(0));
                }
                var same = existingContentMatches && existingCandidateIds.SequenceEqual(candidateIds, StringComparer.Ordinal);
                transaction.Rollback();
                return same
                    ? new TeachingMutationResult(true, "already-present", id, null)
                    : new TeachingMutationResult(false, "conflict", id, "Lesson id already exists with different content or curriculum evidence.");
            }

            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO lesson_records(id, study_plan_id, title, learning_objective, teaching_content,
                        evidence_state, status, created_utc, updated_utc)
                    VALUES ($id, $studyPlanId, $title, $objective, $content,
                        'curriculum-linked-subject-facts-unverified', 'draft', $now, $now);
                    """;
                insert.Parameters.AddWithValue("$id", id);
                insert.Parameters.AddWithValue("$studyPlanId", studyPlanId);
                insert.Parameters.AddWithValue("$title", title);
                insert.Parameters.AddWithValue("$objective", objective);
                insert.Parameters.AddWithValue("$content", contentJson);
                insert.Parameters.AddWithValue("$now", now);
                insert.ExecuteNonQuery();
            }
            for (var index = 0; index < sections.Count; index++)
            {
                using var section = connection.CreateCommand();
                section.Transaction = transaction;
                section.CommandText = """
                    INSERT INTO lesson_sections(lesson_id, sequence, section_kind, content)
                    VALUES ($lessonId, $sequence, $kind, $content);
                    """;
                section.Parameters.AddWithValue("$lessonId", id);
                section.Parameters.AddWithValue("$sequence", index + 1);
                section.Parameters.AddWithValue("$kind", sections[index].Kind);
                section.Parameters.AddWithValue("$content", sections[index].Content);
                section.ExecuteNonQuery();
            }
            foreach (var evidence in candidateEvidence)
            {
                using var link = connection.CreateCommand();
                link.Transaction = transaction;
                link.CommandText = """
                    INSERT INTO lesson_evidence(lesson_id, curriculum_statement_id, evidence_role)
                    VALUES ($lessonId, $candidateId, 'curriculum-objective');
                    """;
                link.Parameters.AddWithValue("$lessonId", id);
                link.Parameters.AddWithValue("$candidateId", evidence.Id);
                link.ExecuteNonQuery();
            }
            RecordWorkspaceEvent(connection, transaction, "lesson-draft", id, "created", now);
            transaction.Commit();
            return new TeachingMutationResult(true, "draft-created-subject-facts-unverified", id, null);
        }
        catch (ArgumentException exception)
        {
            return new TeachingMutationResult(false, "invalid", null, exception.Message);
        }
    }

    public CurriculumExtractionResult ExtractCurriculumCandidates(long revisionId, CapturedSourceBody revision)
    {
        if (revisionId < 1)
            return new CurriculumExtractionResult(false, "invalid", revisionId, 0, 0, 0, "A positive source revision id is required.");
        if (!revision.ContentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            return new CurriculumExtractionResult(false, "unsupported", revisionId, 0, 0, 0, "Only captured HTML revisions are supported by the deterministic extractor.");

        using var connection = OpenConnection();
        string subject;
        string stageScope;
        using (var metadata = connection.CreateCommand())
        {
            metadata.CommandText = """
                SELECT s.subject, s.stage_scope
                FROM source_revisions r JOIN curriculum_sources s ON s.id=r.source_id
                WHERE r.id=$revisionId AND r.sha256=$sha256;
                """;
            metadata.Parameters.AddWithValue("$revisionId", revisionId);
            metadata.Parameters.AddWithValue("$sha256", revision.Sha256);
            using var reader = metadata.ExecuteReader();
            if (!reader.Read())
                return new CurriculumExtractionResult(false, "invalid", revisionId, 0, 0, 0, "Source revision metadata or hash does not match the captured body.");
            subject = reader.GetString(0).Trim().ToLowerInvariant();
            stageScope = reader.GetString(1).Trim();
        }

        List<CurriculumCandidateDraft> drafts;
        try
        {
            drafts = FindCandidateDrafts(revisionId, subject, stageScope, Encoding.UTF8.GetString(revision.Body));
        }
        catch (RegexMatchTimeoutException)
        {
            return new CurriculumExtractionResult(false, "bounded-timeout", revisionId, 0, 0, 0, "HTML scanning exceeded the bounded regular-expression time.");
        }

        using var transaction = connection.BeginTransaction();
        var inserted = 0;
        foreach (var draft in drafts)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT OR IGNORE INTO curriculum_statements(id, source_revision_id, subject, learning_stage,
                    statement_text, source_locator, statement_sha256, extraction_state, review_state, created_utc)
                VALUES ($id, $revisionId, $subject, $learningStage, $text, $locator, $sha256,
                    'deterministic-machine-candidate', 'unreviewed', $createdUtc);
                """;
            insert.Parameters.AddWithValue("$id", draft.Id);
            insert.Parameters.AddWithValue("$revisionId", revisionId);
            insert.Parameters.AddWithValue("$subject", draft.Subject);
            insert.Parameters.AddWithValue("$learningStage", draft.LearningStage);
            insert.Parameters.AddWithValue("$text", draft.Text);
            insert.Parameters.AddWithValue("$locator", draft.SourceLocator);
            insert.Parameters.AddWithValue("$sha256", draft.Sha256);
            insert.Parameters.AddWithValue("$createdUtc", DateTimeOffset.UtcNow.ToString("O"));
            inserted += insert.ExecuteNonQuery();
        }
        RecordWorkspaceEvent(connection, transaction, "source-revision", revisionId.ToString(System.Globalization.CultureInfo.InvariantCulture), "candidate-extraction", DateTimeOffset.UtcNow.ToString("O"));
        transaction.Commit();
        return new CurriculumExtractionResult(true, "candidates-recorded-unreviewed", revisionId, drafts.Count, inserted,
            drafts.Count - inserted, null);
    }

    public IReadOnlyList<CurriculumCandidate> GetCurriculumCandidates()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, source_revision_id, subject, learning_stage, statement_text, source_locator,
                statement_sha256, extraction_state, review_state, created_utc
            FROM curriculum_statements
            ORDER BY source_revision_id DESC, source_locator, id LIMIT 1000;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<CurriculumCandidate>();
        while (reader.Read())
            values.Add(new CurriculumCandidate(reader.GetString(0), reader.GetInt64(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetString(7), reader.GetString(8), reader.GetString(9)));
        return values;
    }

    public TeachingMutationResult ReviewCurriculumCandidate(CurriculumReviewInput input)
    {
        try
        {
            var id = RequireIdentifier(input.Id, "candidate id");
            var decision = (input.Decision ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "accept" or "accepted" => "accepted",
                "reject" or "rejected" => "rejected",
                _ => throw new ArgumentException("decision must be accept or reject.")
            };
            var note = string.IsNullOrWhiteSpace(input.Note) ? string.Empty : RequireText(input.Note, "review note", 1, 500);
            var now = DateTimeOffset.UtcNow.ToString("O");
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var current = connection.CreateCommand())
            {
                current.Transaction = transaction;
                current.CommandText = "SELECT review_state FROM curriculum_statements WHERE id=$id;";
                current.Parameters.AddWithValue("$id", id);
                var state = current.ExecuteScalar() as string;
                if (state is null)
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(false, "invalid", id, "Curriculum candidate does not exist.");
                }
                if (state == decision)
                {
                    transaction.Rollback();
                    return new TeachingMutationResult(true, "already-reviewed", id, null);
                }
            }
            using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = "UPDATE curriculum_statements SET review_state=$decision WHERE id=$id;";
                update.Parameters.AddWithValue("$decision", decision);
                update.Parameters.AddWithValue("$id", id);
                update.ExecuteNonQuery();
            }
            using (var review = connection.CreateCommand())
            {
                review.Transaction = transaction;
                review.CommandText = """
                    INSERT INTO curriculum_statement_reviews(statement_id, decision, review_note, reviewer, reviewed_utc)
                    VALUES ($id, $decision, $note, 'local-operator', $reviewedUtc);
                    """;
                review.Parameters.AddWithValue("$id", id);
                review.Parameters.AddWithValue("$decision", decision);
                review.Parameters.AddWithValue("$note", note);
                review.Parameters.AddWithValue("$reviewedUtc", now);
                review.ExecuteNonQuery();
            }
            RecordWorkspaceEvent(connection, transaction, "curriculum-candidate", id, decision, now);
            transaction.Commit();
            return new TeachingMutationResult(true, decision, id, null);
        }
        catch (ArgumentException exception)
        {
            return new TeachingMutationResult(false, "invalid", null, exception.Message);
        }
    }

    public DocumentCandidateExtractionResult ExtractDocumentCandidates(long documentRevisionId)
    {
        if (documentRevisionId < 1)
            return new DocumentCandidateExtractionResult(false, documentRevisionId, 0, 0, 0, 0, "A positive document revision id is required.");
        using var connection = OpenConnection();
        var blocks = new List<DocumentCandidateSource>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT b.id, b.source_locator, b.text_content, d.discovered_from_revision_id,
                    s.subject, s.stage_scope
                FROM curriculum_document_text_blocks b
                JOIN curriculum_document_revisions r ON r.id=b.document_revision_id
                JOIN curriculum_documents d ON d.id=r.document_id
                JOIN curriculum_sources s ON s.id=d.source_id
                WHERE b.document_revision_id=$revisionId
                ORDER BY b.ordinal, b.id LIMIT 10000;
                """;
            command.Parameters.AddWithValue("$revisionId", documentRevisionId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                blocks.Add(new DocumentCandidateSource(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetInt64(3), reader.GetString(4).Trim().ToLowerInvariant(), reader.GetString(5)));
        }
        if (blocks.Count == 0)
            return new DocumentCandidateExtractionResult(false, documentRevisionId, 0, 0, 0, 0, "No extracted text blocks exist for this document revision.");

        var drafts = new List<DocumentCandidateDraft>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in blocks)
        {
            foreach (var segment in CandidateBoundaryRegex.Split(block.Text))
            {
                var text = WhiteSpaceRegex.Replace(segment, " ").Trim();
                if (text.Length is < 20 or > 800) continue;
                if (!CandidatePhrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase))) continue;
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
                if (!seen.Add($"{block.SourceRevisionId}:{hash}:{block.BlockId}")) continue;
                var offset = Math.Max(0, block.Text.IndexOf(segment, StringComparison.Ordinal));
                drafts.Add(new DocumentCandidateDraft($"stmt-doc-{documentRevisionId}-{hash[..16]}", block.SourceRevisionId,
                    block.Subject, DetectLearningStage(text, block.StageScope), text,
                    $"document:{documentRevisionId}:{block.SourceLocator}:char:{offset}", hash, block.BlockId));
            }
        }

        using var transaction = connection.BeginTransaction();
        var inserted = 0;
        var evidenceLinks = 0;
        var now = DateTimeOffset.UtcNow.ToString("O");
        foreach (var draft in drafts)
        {
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT OR IGNORE INTO curriculum_statements(id, source_revision_id, subject, learning_stage,
                        statement_text, source_locator, statement_sha256, extraction_state, review_state, created_utc)
                    VALUES ($id, $sourceRevisionId, $subject, $learningStage, $text, $locator, $sha256,
                        'document-block-machine-candidate', 'unreviewed', $createdUtc);
                    """;
                insert.Parameters.AddWithValue("$id", draft.Id);
                insert.Parameters.AddWithValue("$sourceRevisionId", draft.SourceRevisionId);
                insert.Parameters.AddWithValue("$subject", draft.Subject);
                insert.Parameters.AddWithValue("$learningStage", draft.LearningStage);
                insert.Parameters.AddWithValue("$text", draft.Text);
                insert.Parameters.AddWithValue("$locator", draft.SourceLocator);
                insert.Parameters.AddWithValue("$sha256", draft.Sha256);
                insert.Parameters.AddWithValue("$createdUtc", now);
                inserted += insert.ExecuteNonQuery();
            }
            string statementId;
            using (var identity = connection.CreateCommand())
            {
                identity.Transaction = transaction;
                identity.CommandText = "SELECT id FROM curriculum_statements WHERE source_revision_id=$revisionId AND statement_sha256=$sha256;";
                identity.Parameters.AddWithValue("$revisionId", draft.SourceRevisionId);
                identity.Parameters.AddWithValue("$sha256", draft.Sha256);
                statementId = identity.ExecuteScalar() as string ?? throw new InvalidDataException("Candidate identity could not be resolved.");
            }
            using (var evidence = connection.CreateCommand())
            {
                evidence.Transaction = transaction;
                evidence.CommandText = """
                    INSERT OR IGNORE INTO curriculum_statement_document_evidence(statement_id, document_revision_id,
                        document_block_id, source_locator, text_sha256, linked_utc)
                    VALUES ($statementId, $documentRevisionId, $blockId, $locator, $sha256, $linkedUtc);
                    """;
                evidence.Parameters.AddWithValue("$statementId", statementId);
                evidence.Parameters.AddWithValue("$documentRevisionId", documentRevisionId);
                evidence.Parameters.AddWithValue("$blockId", draft.BlockId);
                evidence.Parameters.AddWithValue("$locator", draft.SourceLocator);
                evidence.Parameters.AddWithValue("$sha256", draft.Sha256);
                evidence.Parameters.AddWithValue("$linkedUtc", now);
                evidenceLinks += evidence.ExecuteNonQuery();
            }
        }
        RecordWorkspaceEvent(connection, transaction, "document-revision", documentRevisionId.ToString(System.Globalization.CultureInfo.InvariantCulture), "candidate-extraction", now);
        transaction.Commit();
        return new DocumentCandidateExtractionResult(true, documentRevisionId, blocks.Count, drafts.Count, inserted, evidenceLinks, null);
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS teaching_schema_versions (
                version INTEGER PRIMARY KEY,
                applied_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS teaching_principles (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                rule_text TEXT NOT NULL,
                sort_order INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS teaching_gates (
                id TEXT PRIMARY KEY,
                capability TEXT NOT NULL,
                state TEXT NOT NULL,
                required_evidence TEXT NOT NULL,
                sort_order INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS learner_profiles (
                id TEXT PRIMARY KEY,
                display_name TEXT NOT NULL,
                age_band TEXT NOT NULL,
                learning_stage TEXT NOT NULL,
                locale TEXT NOT NULL DEFAULT 'en-GB',
                accessibility_json TEXT NOT NULL DEFAULT '{}',
                preferences_json TEXT NOT NULL DEFAULT '{}',
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                archived_utc TEXT NULL
            );
            CREATE TABLE IF NOT EXISTS study_plans (
                id TEXT PRIMARY KEY,
                learner_id TEXT NOT NULL,
                subject TEXT NOT NULL,
                learning_stage TEXT NOT NULL,
                goal TEXT NOT NULL,
                status TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS curriculum_statements (
                id TEXT PRIMARY KEY,
                source_revision_id INTEGER NOT NULL,
                subject TEXT NOT NULL,
                learning_stage TEXT NOT NULL,
                statement_text TEXT NOT NULL,
                source_locator TEXT NOT NULL,
                statement_sha256 TEXT NOT NULL,
                extraction_state TEXT NOT NULL,
                review_state TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                UNIQUE(source_revision_id, statement_sha256),
                FOREIGN KEY (source_revision_id) REFERENCES source_revisions(id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS lesson_records (
                id TEXT PRIMARY KEY,
                study_plan_id TEXT NOT NULL,
                title TEXT NOT NULL,
                learning_objective TEXT NOT NULL,
                teaching_content TEXT NOT NULL,
                evidence_state TEXT NOT NULL,
                status TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                FOREIGN KEY (study_plan_id) REFERENCES study_plans(id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS curriculum_statement_reviews (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                statement_id TEXT NOT NULL,
                decision TEXT NOT NULL CHECK(decision IN ('accepted','rejected')),
                review_note TEXT NOT NULL,
                reviewer TEXT NOT NULL,
                reviewed_utc TEXT NOT NULL,
                FOREIGN KEY (statement_id) REFERENCES curriculum_statements(id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS curriculum_statement_document_evidence (
                statement_id TEXT NOT NULL REFERENCES curriculum_statements(id) ON DELETE CASCADE,
                document_revision_id INTEGER NOT NULL REFERENCES curriculum_document_revisions(id) ON DELETE RESTRICT,
                document_block_id TEXT NOT NULL REFERENCES curriculum_document_text_blocks(id) ON DELETE RESTRICT,
                source_locator TEXT NOT NULL,
                text_sha256 TEXT NOT NULL,
                linked_utc TEXT NOT NULL,
                PRIMARY KEY (statement_id, document_block_id)
            );
            CREATE TABLE IF NOT EXISTS lesson_evidence (
                lesson_id TEXT NOT NULL,
                curriculum_statement_id TEXT NOT NULL,
                evidence_role TEXT NOT NULL,
                PRIMARY KEY (lesson_id, curriculum_statement_id, evidence_role),
                FOREIGN KEY (lesson_id) REFERENCES lesson_records(id) ON DELETE CASCADE,
                FOREIGN KEY (curriculum_statement_id) REFERENCES curriculum_statements(id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS lesson_sections (
                lesson_id TEXT NOT NULL REFERENCES lesson_records(id) ON DELETE CASCADE,
                sequence INTEGER NOT NULL,
                section_kind TEXT NOT NULL,
                content TEXT NOT NULL,
                PRIMARY KEY (lesson_id, sequence)
            );
            CREATE TABLE IF NOT EXISTS assessment_attempts (
                id TEXT PRIMARY KEY,
                lesson_id TEXT NOT NULL,
                learner_id TEXT NOT NULL,
                prompt_text TEXT NOT NULL,
                response_text TEXT NOT NULL,
                expected_evidence TEXT NOT NULL,
                outcome TEXT NOT NULL,
                feedback_text TEXT NOT NULL,
                confidence_state TEXT NOT NULL,
                created_utc TEXT NOT NULL,
                FOREIGN KEY (lesson_id) REFERENCES lesson_records(id) ON DELETE RESTRICT,
                FOREIGN KEY (learner_id) REFERENCES learner_profiles(id) ON DELETE RESTRICT
            );
            CREATE TABLE IF NOT EXISTS learning_workspace_events (
                sequence INTEGER PRIMARY KEY AUTOINCREMENT,
                occurred_utc TEXT NOT NULL,
                entity_kind TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                action TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_study_plans_learner ON study_plans(learner_id, status);
            CREATE INDEX IF NOT EXISTS idx_curriculum_statements_subject_stage
                ON curriculum_statements(subject, learning_stage, review_state);
            CREATE INDEX IF NOT EXISTS idx_lesson_records_plan ON lesson_records(study_plan_id, status);
            CREATE INDEX IF NOT EXISTS idx_lesson_sections_lesson ON lesson_sections(lesson_id, sequence);
            CREATE INDEX IF NOT EXISTS idx_curriculum_statement_reviews_statement
                ON curriculum_statement_reviews(statement_id, sequence DESC);
            CREATE INDEX IF NOT EXISTS idx_curriculum_statement_document_evidence_revision
                ON curriculum_statement_document_evidence(document_revision_id, statement_id);
            CREATE INDEX IF NOT EXISTS idx_assessment_attempts_learner
                ON assessment_attempts(learner_id, created_utc DESC);
            CREATE INDEX IF NOT EXISTS idx_learning_workspace_events_time
                ON learning_workspace_events(occurred_utc DESC, sequence DESC);
            """;
        command.ExecuteNonQuery();

        SeedPrinciples(connection, transaction);
        SeedGates(connection, transaction);

        using var version = connection.CreateCommand();
        version.Transaction = transaction;
        version.CommandText = "INSERT OR IGNORE INTO teaching_schema_versions(version, applied_utc) VALUES ($version, $appliedUtc);";
        version.Parameters.AddWithValue("$version", SchemaVersion);
        version.Parameters.AddWithValue("$appliedUtc", DateTimeOffset.UtcNow.ToString("O"));
        version.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void SeedPrinciples(SqliteConnection connection, SqliteTransaction transaction)
    {
        var principles = new[]
        {
            new TeachingPrinciple("evidence-before-explanation", "Evidence before explanation", "Bind curriculum claims to a captured source revision and precise locator before teaching from them.", 10),
            new TeachingPrinciple("separate-claim-types", "Separate claim types", "Keep statutory curriculum claims, subject facts, pedagogical judgement, and generated examples visibly distinct.", 20),
            new TeachingPrinciple("level-is-explicit", "Level is explicit", "Every plan, lesson, and assessment names its learner stage; all-ages never means one explanation fits everyone.", 30),
            new TeachingPrinciple("diagnose-before-remediate", "Diagnose before remediation", "Use bounded checks to identify the misconception before selecting explanation, practice, or extension work.", 40),
            new TeachingPrinciple("answers-do-not-leak", "Answers do not leak", "Store assessment prompts, expected evidence, and feedback separately so the learner is not shown the answer accidentally.", 50),
            new TeachingPrinciple("uncertainty-is-data", "Uncertainty is data", "Unknown, inferred, stale, and unreviewed states remain explicit; fluent prose never upgrades evidence.", 60),
            new TeachingPrinciple("accessibility-is-structural", "Accessibility is structural", "Accessibility preferences shape presentation without reducing the learning objective.", 70),
            new TeachingPrinciple("privacy-by-minimum", "Privacy by minimum", "Store only learner information required for teaching continuity and keep it inside the selected installation root.", 80)
        };

        foreach (var principle in principles)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO teaching_principles(id, title, rule_text, sort_order)
                VALUES ($id, $title, $ruleText, $sortOrder)
                ON CONFLICT(id) DO UPDATE SET title=excluded.title, rule_text=excluded.rule_text, sort_order=excluded.sort_order;
                """;
            command.Parameters.AddWithValue("$id", principle.Id);
            command.Parameters.AddWithValue("$title", principle.Title);
            command.Parameters.AddWithValue("$ruleText", principle.RuleText);
            command.Parameters.AddWithValue("$sortOrder", principle.SortOrder);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedGates(SqliteConnection connection, SqliteTransaction transaction)
    {
        var gates = new[]
        {
            new TeachingGate("learner-storage", "Learner profiles and study plans", "schema-ready", "Guarded API, validation, privacy review, and persistence proof.", 10),
            new TeachingGate("objective-extraction", "Curriculum objective extraction", "not-implemented", "Versioned parser output, source locators, duplicate handling, and human review workflow.", 20),
            new TeachingGate("lesson-authoring", "Evidence-linked lesson authoring", "locked", "Reviewed curriculum statements, subject-fact evidence, and age or level presentation rules.", 30),
            new TeachingGate("assessment", "Assessment and feedback", "locked", "Question and answer separation, marking contract, uncertainty handling, and representative evaluation.", 40),
            new TeachingGate("tutor", "Interactive tutor", "locked", "All prior gates plus safeguarding, accessibility, refusal, citation, and conversational continuity evidence.", 50),
            new TeachingGate("curriculum-drift", "Curriculum drift reconciliation", "not-implemented", "Revision diff, changed-objective impact analysis, and operator acceptance receipt.", 60)
        };

        foreach (var gate in gates)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO teaching_gates(id, capability, state, required_evidence, sort_order)
                VALUES ($id, $capability, $state, $requiredEvidence, $sortOrder)
                ON CONFLICT(id) DO UPDATE SET capability=excluded.capability, state=excluded.state,
                    required_evidence=excluded.required_evidence, sort_order=excluded.sort_order;
                """;
            command.Parameters.AddWithValue("$id", gate.Id);
            command.Parameters.AddWithValue("$capability", gate.Capability);
            command.Parameters.AddWithValue("$state", gate.State);
            command.Parameters.AddWithValue("$requiredEvidence", gate.RequiredEvidence);
            command.Parameters.AddWithValue("$sortOrder", gate.SortOrder);
            command.ExecuteNonQuery();
        }
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

    private static int Count(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table};";
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<TeachingPrinciple> ReadPrinciples(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, title, rule_text, sort_order FROM teaching_principles ORDER BY sort_order, id;";
        using var reader = command.ExecuteReader();
        var values = new List<TeachingPrinciple>();
        while (reader.Read())
            values.Add(new TeachingPrinciple(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
        return values;
    }

    private static IReadOnlyList<TeachingGate> ReadGates(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, capability, state, required_evidence, sort_order FROM teaching_gates ORDER BY sort_order, id;";
        using var reader = command.ExecuteReader();
        var values = new List<TeachingGate>();
        while (reader.Read())
            values.Add(new TeachingGate(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4)));
        return values;
    }

    private static IReadOnlyList<LearnerProfile> ReadLearners(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, display_name, age_band, learning_stage, locale, accessibility_json,
                preferences_json, created_utc, updated_utc
            FROM learner_profiles WHERE archived_utc IS NULL ORDER BY display_name, id LIMIT 200;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<LearnerProfile>();
        while (reader.Read())
            values.Add(new LearnerProfile(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                reader.GetString(7), reader.GetString(8)));
        return values;
    }

    private static IReadOnlyList<StudyPlan> ReadStudyPlans(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, learner_id, subject, learning_stage, goal, status, created_utc, updated_utc
            FROM study_plans ORDER BY updated_utc DESC, id LIMIT 500;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<StudyPlan>();
        while (reader.Read())
            values.Add(new StudyPlan(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7)));
        return values;
    }

    private static IReadOnlyList<LearningWorkspaceEvent> ReadWorkspaceEvents(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT sequence, occurred_utc, entity_kind, entity_id, action
            FROM learning_workspace_events ORDER BY sequence DESC LIMIT 100;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<LearningWorkspaceEvent>();
        while (reader.Read())
            values.Add(new LearningWorkspaceEvent(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return values;
    }

    private static IReadOnlyList<LessonDraft> ReadLessonDrafts(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l.id, l.study_plan_id, l.title, l.learning_objective, l.evidence_state, l.status,
                l.created_utc, l.updated_utc,
                (SELECT COUNT(*) FROM lesson_sections s WHERE s.lesson_id=l.id),
                (SELECT COUNT(*) FROM lesson_evidence e WHERE e.lesson_id=l.id)
            FROM lesson_records l ORDER BY l.updated_utc DESC, l.id LIMIT 500;
            """;
        using var reader = command.ExecuteReader();
        var values = new List<LessonDraft>();
        while (reader.Read())
            values.Add(new LessonDraft(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetInt32(8), reader.GetInt32(9)));
        return values;
    }

    private static void RecordWorkspaceEvent(SqliteConnection connection, SqliteTransaction transaction,
        string entityKind, string entityId, string action, string occurredUtc)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO learning_workspace_events(occurred_utc, entity_kind, entity_id, action)
            VALUES ($occurredUtc, $entityKind, $entityId, $action);
            """;
        command.Parameters.AddWithValue("$occurredUtc", occurredUtc);
        command.Parameters.AddWithValue("$entityKind", entityKind);
        command.Parameters.AddWithValue("$entityId", entityId);
        command.Parameters.AddWithValue("$action", action);
        command.ExecuteNonQuery();
    }

    private static string RequireIdentifier(string? value, string field)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is < 3 or > 64)
            throw new ArgumentException($"{field} must contain 3-64 characters.");
        foreach (var character in normalized)
        {
            if (!(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
                throw new ArgumentException($"{field} may contain only lowercase letters, digits, hyphens, and underscores.");
        }
        return normalized;
    }

    private static string RequireText(string? value, string field, int minimum, int maximum)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length < minimum || normalized.Length > maximum)
            throw new ArgumentException($"{field} must contain {minimum}-{maximum} characters.");
        return normalized;
    }

    private static string NormalizeLocale(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "en-GB" : value.Trim();
        if (normalized.Length is < 2 or > 20 || normalized.Any(character => !(char.IsLetterOrDigit(character) || character == '-')))
            throw new ArgumentException("locale must be a bounded language or language-region tag.");
        return normalized;
    }

    private static string NormalizeObjectJson(System.Text.Json.JsonElement value, string field)
    {
        if (value.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null)
            return "{}";
        if (value.ValueKind != System.Text.Json.JsonValueKind.Object)
            throw new ArgumentException($"{field} must be a JSON object.");
        var raw = value.GetRawText();
        if (raw.Length > 4096)
            throw new ArgumentException($"{field} must not exceed 4096 characters.");
        return raw;
    }

    private static string NormalizeSubject(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        normalized = normalized switch
        {
            "math" or "maths" => "mathematics",
            "it" or "information technology" => "computing",
            "language" or "modern foreign languages" => "languages",
            _ => normalized
        };
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "english", "mathematics", "science", "history", "languages", "computing", "cross-curricular", "other"
        };
        if (!allowed.Contains(normalized))
            throw new ArgumentException("subject must be English, mathematics, science, history, languages, computing, cross-curricular, or other.");
        return normalized;
    }

    private static IReadOnlyList<LessonSectionInput> NormalizeLessonSections(IReadOnlyList<LessonSectionInput>? values)
    {
        if (values is null || values.Count is < 1 or > 12)
            throw new ArgumentException("lesson draft requires 1-12 structured sections.");
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "retrieval", "explanation", "worked-example", "guided-practice", "independent-practice",
            "check", "extension", "reflection"
        };
        var normalized = new List<LessonSectionInput>();
        var total = 0;
        foreach (var value in values)
        {
            var kind = (value.Kind ?? string.Empty).Trim().ToLowerInvariant();
            if (!allowed.Contains(kind))
                throw new ArgumentException("lesson section kind is not supported.");
            var content = RequireText(value.Content, "lesson section content", 3, 4000);
            total += content.Length;
            if (total > 16000) throw new ArgumentException("lesson section content must not exceed 16000 characters in total.");
            normalized.Add(new LessonSectionInput(kind, content));
        }
        return normalized;
    }

    private static bool LessonSubjectCompatible(string planSubject, string candidateSubject) =>
        planSubject == "cross-curricular" || candidateSubject == "framework"
        || planSubject.Equals(candidateSubject, StringComparison.OrdinalIgnoreCase);

    private static bool LessonStageCompatible(string planStage, string candidateStage)
    {
        if (candidateStage.Equals("KS1-KS4", StringComparison.OrdinalIgnoreCase)) return true;
        if (candidateStage.Equals("Unresolved stage", StringComparison.OrdinalIgnoreCase)) return false;
        return NormalizeLearningStage(planStage) == NormalizeLearningStage(candidateStage);
    }

    private static string NormalizeLearningStage(string value)
    {
        var compact = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "");
        return compact switch
        {
            "ks1" or "keystage1" => "ks1",
            "ks2" or "keystage2" => "ks2",
            "ks3" or "keystage3" => "ks3",
            "ks4" or "keystage4" => "ks4",
            "primary" or "primaryschool" => "primary",
            "secondary" or "secondaryschool" => "secondary",
            _ => compact
        };
    }

    private static List<CurriculumCandidateDraft> FindCandidateDrafts(long revisionId, string subject, string stageScope, string html)
    {
        var cleaned = UnsafeBlockRegex.Replace(html, " ");
        var values = new List<CurriculumCandidateDraft>();
        var seenHashes = new HashSet<string>(StringComparer.Ordinal);
        var ordinal = 0;
        foreach (Match match in CandidateNodeRegex.Matches(cleaned))
        {
            ordinal++;
            if (ordinal > 5000 || values.Count >= 1000) break;
            var tag = match.Groups[1].Value.ToLowerInvariant();
            var text = NormalizeHtmlText(match.Groups[2].Value);
            if (text.Length is < 20 or > 800) continue;
            if (!CandidatePhrases.Any(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase))) continue;
            var sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
            if (!seenHashes.Add(sha256)) continue;
            var learningStage = DetectLearningStage(text, stageScope);
            values.Add(new CurriculumCandidateDraft($"stmt-{revisionId}-{sha256[..16]}", subject, learningStage,
                text, $"html:{tag}:{ordinal}", sha256));
        }
        return values;
    }

    private static string NormalizeHtmlText(string value)
    {
        var withoutTags = TagRegex.Replace(value, " ");
        return WhiteSpaceRegex.Replace(WebUtility.HtmlDecode(withoutTags), " ").Trim();
    }

    private static string DetectLearningStage(string text, string fallback)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("key stage 1")) return "Key Stage 1";
        if (lower.Contains("key stage 2")) return "Key Stage 2";
        if (lower.Contains("key stage 3")) return "Key Stage 3";
        if (lower.Contains("key stage 4")) return "Key Stage 4";
        if (lower.Contains("primary")) return "Primary";
        if (lower.Contains("secondary")) return "Secondary";
        return string.IsNullOrWhiteSpace(fallback) ? "Unresolved stage" : fallback;
    }
}

internal sealed record TeachingWorkspaceOverview(bool Ok, string DatabaseAuthority, int SchemaVersion,
    int LearnerProfiles, int StudyPlans, int CurriculumStatements, int LessonRecords, int AssessmentAttempts,
    IReadOnlyList<TeachingPrinciple> Principles, IReadOnlyList<TeachingGate> Gates, IReadOnlyList<string> Boundaries);
internal sealed record TeachingPrinciple(string Id, string Title, string RuleText, int SortOrder);
internal sealed record TeachingGate(string Id, string Capability, string State, string RequiredEvidence, int SortOrder);
internal sealed record LearnerProfileInput(string Id, string DisplayName, string AgeBand, string LearningStage,
    string Locale, System.Text.Json.JsonElement Accessibility, System.Text.Json.JsonElement Preferences);
internal sealed record StudyPlanInput(string Id, string LearnerId, string Subject, string LearningStage, string Goal);
internal sealed record TeachingMutationResult(bool Ok, string State, string? Id, string? Error);
internal sealed record LearningWorkspace(bool Ok, IReadOnlyList<LearnerProfile> Learners,
    IReadOnlyList<StudyPlan> StudyPlans, IReadOnlyList<LessonDraft> LessonDrafts, IReadOnlyList<LearningWorkspaceEvent> Events);
internal sealed record LearnerProfile(string Id, string DisplayName, string AgeBand, string LearningStage, string Locale,
    string AccessibilityJson, string PreferencesJson, string CreatedUtc, string UpdatedUtc);
internal sealed record StudyPlan(string Id, string LearnerId, string Subject, string LearningStage, string Goal,
    string Status, string CreatedUtc, string UpdatedUtc);
internal sealed record LearningWorkspaceEvent(long Sequence, string OccurredUtc, string EntityKind, string EntityId, string Action);
internal sealed record LessonDraftInput(string Id, string StudyPlanId, string Title, string LearningObjective,
    IReadOnlyList<LessonSectionInput> Sections, IReadOnlyList<string> CurriculumCandidateIds);
internal sealed record LessonSectionInput(string Kind, string Content);
internal sealed record LessonCandidateEvidence(string Id, string Subject, string LearningStage);
internal sealed record LessonDraft(string Id, string StudyPlanId, string Title, string LearningObjective,
    string EvidenceState, string Status, string CreatedUtc, string UpdatedUtc, int SectionCount, int EvidenceCount);
internal sealed record LessonDetailResult(bool Ok, LessonDetailHeader? Lesson, IReadOnlyList<LessonDetailSection> Sections,
    IReadOnlyList<LessonDetailEvidence> Evidence, string? Error);
internal sealed record LessonDetailHeader(string Id, string StudyPlanId, string Title, string LearningObjective,
    string EvidenceState, string Status, string CreatedUtc, string UpdatedUtc, string LearnerId, string Subject,
    string LearningStage, string StudyGoal, string LearnerDisplayName);
internal sealed record LessonDetailSection(int Sequence, string Kind, string Content);
internal sealed record LessonDetailEvidence(string Id, long SourceRevisionId, string Subject, string LearningStage,
    string StatementText, string SourceLocator, string StatementSha256, string ReviewState, string EvidenceRole);
internal sealed record CurriculumExtractionInput(long RevisionId);
internal sealed record CurriculumExtractionResult(bool Ok, string State, long RevisionId, int CandidatesFound,
    int Inserted, int Existing, string? Error);
internal sealed record CurriculumReviewInput(string Id, string Decision, string Note);
internal sealed record CurriculumCandidate(string Id, long SourceRevisionId, string Subject, string LearningStage,
    string StatementText, string SourceLocator, string StatementSha256, string ExtractionState, string ReviewState, string CreatedUtc);
internal sealed record CurriculumCandidateDraft(string Id, string Subject, string LearningStage, string Text, string SourceLocator, string Sha256);
internal sealed record DocumentCandidateExtractionInput(long DocumentRevisionId);
internal sealed record DocumentCandidateExtractionResult(bool Ok, long DocumentRevisionId, int BlocksScanned,
    int CandidatesFound, int Inserted, int EvidenceLinks, string? Error);
internal sealed record DocumentCandidateSource(string BlockId, string SourceLocator, string Text, long SourceRevisionId,
    string Subject, string StageScope);
internal sealed record DocumentCandidateDraft(string Id, long SourceRevisionId, string Subject, string LearningStage,
    string Text, string SourceLocator, string Sha256, string BlockId);
