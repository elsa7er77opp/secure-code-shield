using System.Text.RegularExpressions;
using SecureCodeShield.Domain.Security;

namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Detects credentials whose format is publicly documented by the issuing provider.
/// These are high-confidence matches: the shape itself identifies the credential, so no
/// network call, validation or guesswork is involved. The matched value never leaves this
/// class unmasked.
/// </summary>
public static class SecretPatternDetector
{
    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private sealed record ProviderPattern(string RuleId, Regex Expression);

    private static readonly ProviderPattern[] Patterns =
    [
        new(RuleCatalog.AwsAccessKey,
            new Regex(@"\b((?:A3T[A-Z0-9]|AKIA|ASIA|ABIA|ACCA)[A-Z0-9]{16})\b", Options)),

        new(RuleCatalog.GitHubToken,
            new Regex(@"\b(gh[pousr]_[A-Za-z0-9]{36,251}|github_pat_[A-Za-z0-9_]{22,255})\b", Options)),

        new(RuleCatalog.SlackToken,
            new Regex(@"\b(xox[baprs]-[A-Za-z0-9-]{10,})", Options)),

        new(RuleCatalog.StripeSecretKey,
            new Regex(@"\b((?:sk|rk)_live_[A-Za-z0-9]{16,})\b", Options)),

        new(RuleCatalog.GoogleApiKey,
            new Regex(@"\b(AIza[0-9A-Za-z\-_]{35})\b", Options)),

        new(RuleCatalog.SendGridApiKey,
            new Regex(@"\b(SG\.[A-Za-z0-9_-]{16,}\.[A-Za-z0-9_-]{16,})\b", Options)),

        new(RuleCatalog.NpmAccessToken,
            new Regex(@"\b(npm_[A-Za-z0-9]{36})\b", Options)),

        new(RuleCatalog.JsonWebToken,
            new Regex(@"\b(eyJ[A-Za-z0-9_-]{8,}\.eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,})\b", Options)),

        new(RuleCatalog.PrivateKeyBlock,
            new Regex(@"-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP |ENCRYPTED )?PRIVATE KEY-----", Options)),

        new(RuleCatalog.ConnectionStringPassword,
            new Regex(@"\b(?:postgres(?:ql)?|mysql|mariadb|mongodb(?:\+srv)?|redis|amqp)://[^:/\s]+:([^@\s""']{3,})@", Options))
    ];

    /// <summary>
    /// Returns every provider credential found on a single line. A line may carry more than one.
    /// </summary>
    public static IReadOnlyList<Finding> ScanLine(string relativePath, int lineNumber, string sourceLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        ArgumentNullException.ThrowIfNull(sourceLine);

        List<Finding>? findings = null;

        foreach (var pattern in Patterns)
        {
            var match = pattern.Expression.Match(sourceLine);
            if (!match.Success)
            {
                continue;
            }

            // Group 1 holds the credential itself where the pattern needs surrounding context to match.
            var captured = match.Groups.Count > 1 && match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Value;

            findings ??= [];
            findings.Add(new Finding(
                pattern.RuleId,
                relativePath,
                lineNumber,
                SecretEvidenceMasker.Mask(captured)));
        }

        if (findings is null)
        {
            return Array.Empty<Finding>();
        }

        return findings;
    }
}
