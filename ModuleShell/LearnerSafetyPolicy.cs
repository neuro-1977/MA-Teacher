using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MATeacher.ModuleShell;

internal static partial class LearnerSafetyPolicy
{
    private static readonly HashSet<string> Profanity = new(StringComparer.Ordinal)
    {
        "arse", "asshole", "bastard", "bitch", "bollocks", "bullshit", "cunt", "dickhead", "fuck", "fucker", "fucking", "motherfucker", "piss", "prick", "shit", "shite", "wanker"
    };

    private static readonly HashSet<string> ExplicitSexual = new(StringComparer.Ordinal)
    {
        "porn", "porno", "pornography", "hentai", "onlyfans", "xvideos", "xhamster", "redtube", "pornhub"
    };

    private static readonly HashSet<string> HateOrSlur = new(StringComparer.Ordinal)
    {
        "chink", "coon", "faggot", "golliwog", "kike", "nigger", "paki", "retard", "spastic", "tranny"
    };

    internal static LearnerSafetyEvaluation EvaluateSubmission(string? value)
        => Evaluate(value, searchMode: false);

    internal static LearnerSafetyEvaluation EvaluateSearch(string? value)
        => Evaluate(value, searchMode: true);

    private static LearnerSafetyEvaluation Evaluate(string? value, bool searchMode)
    {
        var source = (value ?? string.Empty).Normalize(NormalizationForm.FormKC);
        var hadHiddenCharacters = HiddenCharacterRegex().IsMatch(source);
        source = HiddenCharacterRegex().Replace(source, string.Empty);
        var lowered = source.ToLowerInvariant();
        var simplified = SimplifyLeetspeak(lowered);
        var categories = new HashSet<string>(StringComparer.Ordinal);
        var tokens = WordRegex().Matches(simplified).Select(match => CollapseRuns(match.Value)).ToArray();

        if (tokens.Any(Profanity.Contains)) categories.Add("profanity");
        if (tokens.Any(ExplicitSexual.Contains) || ExplicitPhraseRegex().IsMatch(simplified)) categories.Add("explicit-sexual-content");
        if (tokens.Any(HateOrSlur.Contains)) categories.Add("hate-or-slur");
        if (SafetyBypassRegex().IsMatch(simplified)) categories.Add("safety-bypass");
        if (ExternalAddressRegex().IsMatch(lowered)) categories.Add(searchMode ? "unsafe-external-search" : "external-link");
        if (hadHiddenCharacters && categories.Count > 0) categories.Add("obfuscated-input");

        return new LearnerSafetyEvaluation(
            categories.Count == 0,
            categories.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            source.Length,
            categories.Count == 0
                ? null
                : "Use school-safe words and keep links out of learner text. If you need to tell your teacher about something unsafe, speak to them directly.");
    }

    private static string SimplifyLeetspeak(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '0' => 'o', '1' => 'i', '3' => 'e', '4' => 'a', '5' => 's', '7' => 't', '$' => 's', '@' => 'a',
                _ => character
            });
        }
        return builder.ToString();
    }

    private static string CollapseRuns(string value)
    {
        if (value.Length < 3) return value;
        var builder = new StringBuilder(value.Length);
        var previous = '\0';
        var run = 0;
        foreach (var character in value)
        {
            run = character == previous ? run + 1 : 1;
            if (run <= 2) builder.Append(character);
            previous = character;
        }
        return builder.ToString();
    }

    [GeneratedRegex("[\\u200B-\\u200F\\u202A-\\u202E\\u2060\\u2066-\\u2069\\uFEFF]", RegexOptions.CultureInvariant)]
    private static partial Regex HiddenCharacterRegex();

    [GeneratedRegex("[a-z]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    [GeneratedRegex("\\b(?:explicit\\s+(?:sex|sexual)|nude\\s+(?:photo|picture|video)|adult\\s+(?:video|site|content)|xxx)\\b", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitPhraseRegex();

    [GeneratedRegex("\\b(?:ignore|forget|override|remove|disable|break|bypass|evade)\\b.{0,48}\\b(?:rule|rules|filter|filters|safety|system|guard|guards|restriction|restrictions)\\b|\\b(?:jailbreak|developer\\s+mode|pretend\\s+(?:there\\s+are|you\\s+have)\\s+no\\s+rules)\\b", RegexOptions.CultureInvariant)]
    private static partial Regex SafetyBypassRegex();

    [GeneratedRegex("(?:https?://|www\\.|(?:[a-z0-9-]+\\.)+(?:com|net|org|io|xxx|adult|porn)(?:[/\\s]|$))", RegexOptions.CultureInvariant)]
    private static partial Regex ExternalAddressRegex();
}

internal sealed record LearnerSafetyEvaluation(bool Allowed, IReadOnlyList<string> Categories, int InputLength, string? LearnerMessage);
