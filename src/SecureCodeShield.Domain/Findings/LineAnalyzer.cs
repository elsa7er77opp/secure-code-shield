namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Runs every line-level detector in one place and applies suppression once, so no caller
/// can accidentally skip a rule or report a suppressed one. Raw source lines enter here and
/// only masked findings leave.
/// </summary>
public static class LineAnalyzer
{
    public static IReadOnlyList<Finding> Analyze(string relativePath, int lineNumber, string sourceLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        ArgumentNullException.ThrowIfNull(sourceLine);

        if (sourceLine.Length == 0 || SuppressionPolicy.IsNonProductionPath(relativePath))
        {
            return [];
        }

        var candidates = new List<Finding>();

        // The original environment-style rule, preserved and mapped onto the richer finding model.
        foreach (var legacy in SecretAssignmentDetector.ScanLine(relativePath, lineNumber, sourceLine))
        {
            candidates.Add(new Finding(legacy.RuleId, legacy.RelativePath, legacy.LineNumber, legacy.MaskedEvidence));
        }

        candidates.AddRange(SecretPatternDetector.ScanLine(relativePath, lineNumber, sourceLine));
        candidates.AddRange(HighEntropySecretDetector.ScanLine(relativePath, lineNumber, sourceLine));
        candidates.AddRange(DangerousPatternDetector.ScanLine(relativePath, lineNumber, sourceLine));

        if (candidates.Count == 0)
        {
            return [];
        }

        var accepted = new List<Finding>(candidates.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var finding in candidates)
        {
            if (SuppressionPolicy.IsSuppressedInline(sourceLine, finding.RuleId))
            {
                continue;
            }

            // One line raising the same rule twice adds noise without adding information.
            if (!seen.Add(finding.RuleId))
            {
                continue;
            }

            accepted.Add(finding);
        }

        return accepted;
    }
}
