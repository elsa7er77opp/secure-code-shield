using System.Text.RegularExpressions;

namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Decides what must never be reported. A scanner that floods a reviewer with fixture data
/// gets switched off, so suppression is part of the engine rather than a display filter.
/// Every decision is a pure function of the path and the line — nothing is read from disk here.
/// </summary>
public static class SuppressionPolicy
{
    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase;

    /// <summary>The inline marker a developer writes to accept a specific line.</summary>
    public const string InlineMarker = "scs-ignore";

    /// <summary>
    /// Paths whose whole purpose is to hold made-up values: tests, fixtures, samples, generated docs.
    /// Matched on directory segments and on file-name suffixes, not on a bare substring.
    /// </summary>
    private static readonly Regex NonProductionPath = new(
        @"(^|/)(?:tests?|__tests__|spec|specs|fixtures?|mocks?|__mocks__|testdata|test_data|samples?|examples?|demo|docs?|stories)(/|$)|\.(?:spec|test|stories|e2e)\.[a-z]+$|(^|/)[^/]*\.example(\.[a-z]+)?$|(^|/)[^/]*\.sample(\.[a-z]+)?$",
        Options);

    /// <summary>
    /// `scs-ignore` on its own suppresses the line; `scs-ignore SCS-CODE-004` suppresses only that rule,
    /// which keeps a targeted acceptance from silently hiding a different problem later.
    /// </summary>
    private static readonly Regex InlineSuppression = new(
        @"scs-ignore(?:\s*[:=]?\s*(?<rules>[A-Z]{3}-[A-Z]+-\d{3}(?:\s*,\s*[A-Z]{3}-[A-Z]+-\d{3})*))?",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsNonProductionPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        return NonProductionPath.IsMatch(relativePath.Replace('\\', '/'));
    }

    public static bool IsSuppressedInline(string sourceLine, string ruleId)
    {
        ArgumentNullException.ThrowIfNull(sourceLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

        var match = InlineSuppression.Match(sourceLine);
        if (!match.Success)
        {
            return false;
        }

        var rules = match.Groups["rules"];
        if (!rules.Success)
        {
            return true;
        }

        foreach (var candidate in rules.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(candidate, ruleId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsSuppressed(string relativePath, string sourceLine, string ruleId) =>
        IsNonProductionPath(relativePath) || IsSuppressedInline(sourceLine, ruleId);
}
