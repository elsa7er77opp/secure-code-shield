using System.Text.RegularExpressions;

namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// Detects code shapes that are dangerous regardless of the values flowing through them.
/// These rules carry no secret, so they report an empty evidence string and rely on the
/// location plus the rule rationale. Heuristic rules are declared Low confidence in the
/// catalog rather than being hidden behind a stricter pattern that would miss real cases.
/// </summary>
public static class DangerousPatternDetector
{
    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase;

    private sealed record CodePattern(string RuleId, Regex Expression, Regex? Requires = null);

    private static readonly CodePattern[] Patterns =
    [
        // Signature disabled outright: { algorithm: "none" }, "alg": "none", setAlgorithm(NONE)
        new(RuleCatalog.JwtAlgorithmNone,
            new Regex(@"[""']?(?:alg|algorithm|algorithms)[""']?\s*[:=]\s*\[?\s*[""']none[""']", Options)),

        // Certificate validation switched off across the common runtimes.
        new(RuleCatalog.TlsVerificationDisabled,
            new Regex(@"rejectUnauthorized\s*:\s*false|NODE_TLS_REJECT_UNAUTHORIZED\s*=\s*[""']?0|InsecureSkipVerify\s*:\s*true|verify\s*=\s*False\b|CURLOPT_SSL_VERIFYPEER\s*,\s*(?:false|0)|ServerCertificateCustomValidationCallback\s*=\s*.*(?:true|AlwaysValid)", Options)),

        // A signature or digest compared with a normal equality operator.
        new(RuleCatalog.TimingUnsafeComparison,
            new Regex(@"(?<subject>signature|hmac|digest|mac|token|checksum)\w*\s*(?:==|!=|===|!==)\s*|\.Equals\s*\(\s*\w*(?:signature|hmac|digest|token)\w*\s*\)", Options)),

        // A SQL keyword in the same statement as concatenation or interpolation.
        new(RuleCatalog.SqlBuiltByConcatenation,
            new Regex(@"(?:SELECT\s+.+\s+FROM|INSERT\s+INTO|UPDATE\s+\w+\s+SET|DELETE\s+FROM)", Options),
            new Regex(@"""\s*\+\s*\w|\w\s*\+\s*""|\$\{|\{\d+\}|'\s*\.\s*\$|%s|f""", Options)),

        // A string evaluated as code.
        new(RuleCatalog.DynamicCodeEvaluation,
            new Regex(@"(?<![.\w])eval\s*\(|new\s+Function\s*\(|setTimeout\s*\(\s*[""'`]|exec\s*\(\s*compile\s*\(", Options)),

        // A shell command assembled from a variable.
        new(RuleCatalog.ShellExecutionWithInterpolation,
            new Regex(@"(?:child_process\.)?exec(?:Sync)?\s*\(|shell\s*=\s*True|Process\.Start\s*\(|os\.system\s*\(|popen\s*\(", Options),
            new Regex(@"\$\{|\+\s*\w|%s|f""|\$\(", Options)),

        // A broken hash used as though it were a security primitive.
        new(RuleCatalog.WeakHashAlgorithm,
            new Regex(@"\b(?:MD5|SHA-?1)\b\s*(?:\.Create\s*\(|\(\s*\)|\.new\s*\(|\)|""\s*\))|createHash\s*\(\s*[""'](?:md5|sha1)[""']|HashAlgorithm\.Create\s*\(\s*[""'](?:MD5|SHA1)[""']", Options)),

        // Deserializers that can construct arbitrary objects.
        new(RuleCatalog.InsecureDeserialization,
            new Regex(@"\bpickle\.loads?\s*\(|\bBinaryFormatter\s*\(|yaml\.load\s*\((?![^)]*Loader\s*=\s*yaml\.SafeLoader)|ObjectInputStream\s*\(|unserialize\s*\(", Options)),

        // A secret or session value handed to a logger.
        new(RuleCatalog.SecretWrittenToLog,
            new Regex(@"(?:console\.(?:log|info|warn|error)|logger?\.(?:log|info|debug|warn|error|trace)|print(?:ln)?|Console\.WriteLine|Debug\.WriteLine)\s*\(", Options),
            new Regex(@"\b\w*(?:password|secret|token|apikey|api_key|session|credential|authorization|cookie)\w*\b", Options))
    ];

    public static IReadOnlyList<Finding> ScanLine(string relativePath, int lineNumber, string sourceLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);
        ArgumentNullException.ThrowIfNull(sourceLine);

        if (IsCommentOnly(sourceLine))
        {
            return [];
        }

        List<Finding>? findings = null;

        foreach (var pattern in Patterns)
        {
            if (!pattern.Expression.IsMatch(sourceLine))
            {
                continue;
            }

            // A two-part rule only fires when both halves are present on the line.
            if (pattern.Requires is not null && !pattern.Requires.IsMatch(sourceLine))
            {
                continue;
            }

            findings ??= [];
            findings.Add(new Finding(pattern.RuleId, relativePath, lineNumber, string.Empty));
        }

        if (findings is null)
        {
            return Array.Empty<Finding>();
        }

        return findings;
    }

    /// <summary>
    /// A line that is entirely a comment describes code rather than being code.
    /// Reporting those is the fastest way to lose a reviewer's trust.
    /// </summary>
    private static bool IsCommentOnly(string sourceLine)
    {
        var trimmed = sourceLine.AsSpan().TrimStart();

        return trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("#", StringComparison.Ordinal)
            || trimmed.StartsWith("*", StringComparison.Ordinal)
            || trimmed.StartsWith("/*", StringComparison.Ordinal)
            || trimmed.StartsWith("--", StringComparison.Ordinal);
    }
}
