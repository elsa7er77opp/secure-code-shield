using System.Text.RegularExpressions;
using SecureCodeShield.Domain.Security;

namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Detects a conservative subset of environment-style assignments. This rule is advisory and never validates credentials.
/// </summary>
public static partial class SecretAssignmentDetector
{
    private const string RuleId = "SCS-SECRET-001";

    public static IReadOnlyList<SecretFinding> ScanLine(string relativePath, int lineNumber, string sourceLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        ArgumentNullException.ThrowIfNull(sourceLine);

        var match = EnvironmentStyleSecretAssignment().Match(sourceLine);
        if (!match.Success)
        {
            return [];
        }

        var maskedEvidence = SecretEvidenceMasker.Mask(match.Groups["value"].Value);

        return
        [
            new SecretFinding(
                RuleId,
                relativePath,
                lineNumber,
                maskedEvidence)
        ];
    }

    [GeneratedRegex(
        "^\\s*(?<name>[A-Z0-9_]*(?:TOKEN|SECRET|API_KEY|PASSWORD|PRIVATE_KEY)[A-Z0-9_]*)\\s*=\\s*(?<value>[^\\s#]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentStyleSecretAssignment();
}
