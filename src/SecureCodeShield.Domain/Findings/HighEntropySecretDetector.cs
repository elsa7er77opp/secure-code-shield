using System.Text.RegularExpressions;
using SecureCodeShield.Domain.Security;

namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Detects credentials that have no publicly documented shape, by combining two weak signals
/// that are strong together: the identifier being assigned reads as security material, and the
/// literal assigned to it is long and high-entropy.
///
/// This is the rule that catches `const signingKey = "…"`, which the environment-style rule cannot see.
/// It stays at medium confidence by design and is deliberately conservative: placeholders,
/// obvious examples and low-entropy values are rejected rather than reported.
/// </summary>
public static class HighEntropySecretDetector
{
    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase;

    /// <summary>Minimum literal length considered. Shorter values produce too many false positives.</summary>
    public const int MinimumLength = 20;

    /// <summary>Minimum Shannon entropy in bits per character. Base64-like credentials sit above 4.</summary>
    public const double MinimumEntropy = 3.6d;

    /// <summary>
    /// An assignment of a quoted literal to an identifier whose name reads as security material.
    /// Covers `const x = "…"`, `x: "…"`, `x = '…'`, `"x": "…"` across C-family, JSON and YAML.
    /// </summary>
    private static readonly Regex SecurityIdentifierAssignment = new(
        @"[""']?(?<name>[A-Za-z0-9_.\-]*(?:secret|token|passwd|password|apikey|api_key|accesskey|access_key|privatekey|private_key|signingkey|signing_key|credential|auth|salt|cipherkey)[A-Za-z0-9_.\-]*)[""']?\s*[:=]\s*[""'](?<value>[^""'\r\n]{20,512})[""']",
        Options);

    /// <summary>Values that are plainly not credentials, however random they look.</summary>
    private static readonly Regex ObviousPlaceholder = new(
        @"^(?:x{4,}|\*{4,}|\.{4,}|<[^>]+>|\$\{[^}]*\}|\{\{[^}]*\}\}|%[A-Za-z0-9_]+%|process\.env\.|your[-_ ]|change[-_ ]?me|replace[-_ ]?me|example|sample|dummy|placeholder|redacted|todo|null|undefined|none)",
        Options);

    /// <summary>Values that are structurally something else — a URL, a path, a MIME type, a date.</summary>
    private static readonly Regex NotACredential = new(
        @"^(?:https?://|ftp://|/|\./|\.\./|[A-Za-z]:\\|[a-z]+/[a-z0-9.+-]+$|\d{4}-\d{2}-\d{2})",
        Options);

    public static IReadOnlyList<Finding> ScanLine(string relativePath, int lineNumber, string sourceLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        ArgumentNullException.ThrowIfNull(sourceLine);

        var match = SecurityIdentifierAssignment.Match(sourceLine);
        if (!match.Success)
        {
            return [];
        }

        var value = match.Groups["value"].Value;
        if (!IsLikelyCredential(value))
        {
            return [];
        }

        return
        [
            new Finding(
                RuleCatalog.HighEntropyAssignment,
                relativePath,
                lineNumber,
                SecretEvidenceMasker.Mask(value),
                match.Groups["name"].Value)
        ];
    }

    /// <summary>
    /// Exposed so the rejection reasons can be tested directly and stay honest:
    /// this rule must reject far more than it reports.
    /// </summary>
    public static bool IsLikelyCredential(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length < MinimumLength)
        {
            return false;
        }

        if (ObviousPlaceholder.IsMatch(value) || NotACredential.IsMatch(value))
        {
            return false;
        }

        // A credential is a single opaque token; a sentence or a list is not.
        if (value.Contains(' ', StringComparison.Ordinal) || value.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        return EntropyEstimator.BitsPerCharacter(value) >= MinimumEntropy;
    }
}
