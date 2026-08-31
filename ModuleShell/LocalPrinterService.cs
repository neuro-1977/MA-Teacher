using System.Drawing;
using System.Drawing.Printing;
using System.Text;

namespace MATeacher.ModuleShell;

internal sealed class LocalPrinterService
{
    private readonly ClassroomPrintStore _requests;
    private readonly TeachingWorkspaceStore _teaching;
    private readonly LearningCheckStore _learningChecks;
    private readonly LearnerSafetyStore _learnerSafety;

    internal LocalPrinterService(ClassroomPrintStore requests, TeachingWorkspaceStore teaching, LearningCheckStore learningChecks, LearnerSafetyStore learnerSafety)
    {
        _requests = requests;
        _teaching = teaching;
        _learningChecks = learningChecks;
        _learnerSafety = learnerSafety;
    }

    internal LocalPrinterOverview GetOverview()
    {
        var printers = DetectPrinters(out var error);
        return new(true, printers, _requests.GetOverview().Requests, error, new[]
        {
            "Learners can request lesson or feedback output but cannot choose or contact a printer.",
            "A teacher must select a currently detected printer and approve each request.",
            "MA-Teacher prints generated plain text only; learner files and markup never enter the spooler.",
            "Printer queues may retain personal data. Use a managed school printer and collect pages promptly."
        });
    }

    internal async Task<LocalPrintMutation> ApproveAsync(LocalPrintApprovalInput input)
    {
        var request = _requests.Get(input.RequestId);
        if (request is null || !string.Equals(request.State, "pending", StringComparison.Ordinal)) return new(false, "conflict", input.RequestId, "The print request is not pending.");
        var printers = DetectPrinters(out _);
        if (!printers.Any(value => string.Equals(value.Name, input.PrinterName, StringComparison.Ordinal))) return new(false, "invalid-printer", request.Id, "Choose a printer currently detected by Windows.");
        var document = BuildRequestedDocument(request);
        if (!document.Ok) return new(false, "invalid-document", request.Id, document.Error);
        try
        {
            await Task.Run(() => PrintText(input.PrinterName, document.Title!, document.Body!)).ConfigureAwait(false);
            var completed = _requests.Complete(request.Id, "pending", "printed", input.PrinterName, null);
            return new(completed.Ok, completed.State, request.Id, completed.Error);
        }
        catch (Exception exception)
        {
            var error = Bound(exception.Message, 400);
            _requests.Complete(request.Id, "pending", "failed", input.PrinterName, error);
            return new(false, "failed", request.Id, error);
        }
    }

    internal LocalPrintMutation Decline(string requestId)
    {
        var mutation = _requests.Complete(requestId, "pending", "declined", null, null);
        return new(mutation.Ok, mutation.State, mutation.Id, mutation.Error);
    }

    internal async Task<LocalPrintMutation> PrintSafetyReportAsync(string printerName)
    {
        var printers = DetectPrinters(out _);
        if (!printers.Any(value => string.Equals(value.Name, printerName, StringComparison.Ordinal))) return new(false, "invalid-printer", null, "Choose a printer currently detected by Windows.");
        var incidents = _learnerSafety.GetOverview().Incidents;
        var body = new StringBuilder();
        body.AppendLine("MA-Teacher learner safety report").AppendLine($"Printed: {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}").AppendLine();
        body.AppendLine("Human follow-up only. This report is not an automatic punishment or diagnosis.").AppendLine();
        foreach (var incident in incidents)
        {
            body.AppendLine($"{incident.LastSeenUtc} | Learner {incident.LearnerId} | Lesson {incident.LessonId}");
            body.AppendLine($"Categories: {string.Join(", ", incident.Categories)} | Repeats: {incident.OccurrenceCount} | Action: {incident.Action}").AppendLine();
        }
        if (incidents.Count == 0) body.AppendLine("No learner safety incidents are recorded.");
        try
        {
            await Task.Run(() => PrintText(printerName, "MA-Teacher safety report", body.ToString())).ConfigureAwait(false);
            return new(true, "printed", null, null);
        }
        catch (Exception exception) { return new(false, "failed", null, Bound(exception.Message, 400)); }
    }

    private GeneratedPrintDocument BuildRequestedDocument(ClassroomPrintRequest request)
    {
        var lesson = _teaching.GetLessonDetail(request.LessonId);
        if (!lesson.Ok || lesson.Lesson is null || !string.Equals(lesson.Lesson.LearnerId, request.LearnerId, StringComparison.Ordinal)) return new(false, null, null, "The assigned lesson is no longer available.");
        var body = new StringBuilder();
        body.AppendLine("MA-Teacher").AppendLine(lesson.Lesson.Title).AppendLine();
        body.AppendLine($"Learner: {lesson.Lesson.LearnerDisplayName}");
        body.AppendLine($"Subject: {lesson.Lesson.Subject} | Stage: {lesson.Lesson.LearningStage}");
        body.AppendLine($"Learning goal: {lesson.Lesson.LearningObjective}").AppendLine();
        if (request.DocumentKind == "lesson")
        {
            foreach (var section in lesson.Sections.OrderBy(value => value.Sequence)) body.AppendLine(section.Kind.ToUpperInvariant()).AppendLine(section.Content).AppendLine();
        }
        else
        {
            var overview = _learningChecks.GetOverview();
            var checks = overview.Checks.Where(value => value.FingerprintCurrent && value.LessonId == request.LessonId && value.LearnerId == request.LearnerId).ToArray();
            var ids = checks.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
            var attempts = overview.Attempts.Where(value => value.LearnerId == request.LearnerId && ids.Contains(value.CheckId)).ToArray();
            foreach (var check in checks)
            {
                body.AppendLine(check.Prompt).AppendLine($"Success looks like: {check.SuccessCriteria}");
                foreach (var attempt in attempts.Where(value => value.CheckId == check.Id))
                {
                    body.AppendLine($"Submitted {attempt.SubmittedUtc}: {attempt.ResponseText}");
                    body.AppendLine(attempt.ReviewState == "reviewed" ? $"Teacher review: {attempt.Outcome} | {attempt.Feedback}" : "Waiting for teacher review.");
                }
                body.AppendLine();
            }
            if (checks.Length == 0) body.AppendLine("No current practice checks are available for this lesson.");
        }
        body.AppendLine().AppendLine("Printed with teacher approval. Keep learner pages private and dispose of them securely.");
        return new(true, $"MA-Teacher - {lesson.Lesson.Title} - {request.DocumentKind}", body.ToString(), null);
    }

    private static IReadOnlyList<LocalPrinterRecord> DetectPrinters(out string? error)
    {
        try
        {
            var defaults = new PrinterSettings();
            var defaultName = defaults.PrinterName;
            error = null;
            return PrinterSettings.InstalledPrinters.Cast<string>()
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .Select(value => new LocalPrinterRecord(value, string.Equals(value, defaultName, StringComparison.Ordinal)))
                .ToArray();
        }
        catch (Exception exception)
        {
            error = $"Windows printer detection failed: {Bound(exception.Message, 300)}";
            return Array.Empty<LocalPrinterRecord>();
        }
    }

    private static void PrintText(string printerName, string title, string body)
    {
        var lines = Wrap(body, 96).ToArray();
        var index = 0;
        using var document = new PrintDocument
        {
            DocumentName = Bound(title, 120),
            PrintController = new StandardPrintController(),
            PrinterSettings = new PrinterSettings { PrinterName = printerName }
        };
        if (!document.PrinterSettings.IsValid) throw new InvalidOperationException("Windows reports that the selected printer is unavailable.");
        document.PrintPage += (_, eventArgs) =>
        {
            var graphics = eventArgs.Graphics ?? throw new InvalidOperationException("Windows did not provide a printer graphics surface.");
            using var font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            using var brush = new SolidBrush(Color.Black);
            var lineHeight = font.GetHeight(graphics) + 3f;
            var y = (float)eventArgs.MarginBounds.Top;
            while (index < lines.Length && y + lineHeight <= eventArgs.MarginBounds.Bottom)
            {
                graphics.DrawString(lines[index++], font, brush, eventArgs.MarginBounds.Left, y);
                y += lineHeight;
            }
            eventArgs.HasMorePages = index < lines.Length;
        };
        document.Print();
    }

    private static IEnumerable<string> Wrap(string body, int width)
    {
        foreach (var sourceLine in body.Replace("\r", string.Empty).Split('\n'))
        {
            var line = sourceLine.TrimEnd();
            if (line.Length == 0) { yield return string.Empty; continue; }
            while (line.Length > width)
            {
                var split = line.LastIndexOf(' ', width);
                if (split < width / 2) split = width;
                yield return line[..split].TrimEnd();
                line = line[split..].TrimStart();
            }
            yield return line;
        }
    }

    private static string Bound(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
    private sealed record GeneratedPrintDocument(bool Ok, string? Title, string? Body, string? Error);
}

internal sealed record LocalPrinterOverview(bool Ok, IReadOnlyList<LocalPrinterRecord> Printers, IReadOnlyList<ClassroomPrintRequest> Requests, string? Error, IReadOnlyList<string> Boundaries);
internal sealed record LocalPrinterRecord(string Name, bool IsDefault);
internal sealed record LocalPrintApprovalInput(string RequestId, string PrinterName);
internal sealed record LocalPrintDeclineInput(string RequestId);
internal sealed record LocalPrinterNameInput(string PrinterName);
internal sealed record LocalPrintMutation(bool Ok, string State, string? RequestId, string? Error);
