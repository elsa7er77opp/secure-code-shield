namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// The single source of truth for every rule the local engine can raise.
/// Detectors reference a rule by identifier; nothing else in the codebase defines severity or mappings.
/// </summary>
public static class RuleCatalog
{
    // Credential material.
    public const string EnvironmentSecretAssignment = "SCS-SECRET-001";
    public const string AwsAccessKey = "SCS-SECRET-010";
    public const string GitHubToken = "SCS-SECRET-011";
    public const string SlackToken = "SCS-SECRET-012";
    public const string StripeSecretKey = "SCS-SECRET-013";
    public const string GoogleApiKey = "SCS-SECRET-014";
    public const string SendGridApiKey = "SCS-SECRET-015";
    public const string NpmAccessToken = "SCS-SECRET-016";
    public const string JsonWebToken = "SCS-SECRET-017";
    public const string PrivateKeyBlock = "SCS-SECRET-018";
    public const string ConnectionStringPassword = "SCS-SECRET-019";
    public const string HighEntropyAssignment = "SCS-SECRET-020";

    // Dangerous code patterns.
    public const string JwtAlgorithmNone = "SCS-CODE-001";
    public const string TlsVerificationDisabled = "SCS-CODE-002";
    public const string TimingUnsafeComparison = "SCS-CODE-003";
    public const string SqlBuiltByConcatenation = "SCS-CODE-004";
    public const string DynamicCodeEvaluation = "SCS-CODE-005";
    public const string ShellExecutionWithInterpolation = "SCS-CODE-006";
    public const string WeakHashAlgorithm = "SCS-CODE-007";
    public const string InsecureDeserialization = "SCS-CODE-008";
    public const string SecretWrittenToLog = "SCS-CODE-009";

    private static readonly IReadOnlyDictionary<string, RuleDefinition> Definitions =
        new[]
        {
            new RuleDefinition(EnvironmentSecretAssignment, "Environment-style secret assignment", Severity.High, Confidence.Medium, "CWE-798", "A07:2021",
                "A credential is assigned to an environment-style variable inside the repository, so anyone with repository access can read it."),
            new RuleDefinition(AwsAccessKey, "AWS access key identifier in source", Severity.Critical, Confidence.High, "CWE-798", "A07:2021",
                "An AWS access key identifier grants API access under the owning account and cannot be revoked by changing a password."),
            new RuleDefinition(GitHubToken, "GitHub access token in source", Severity.Critical, Confidence.High, "CWE-798", "A07:2021",
                "A GitHub token carries the permissions of the account that issued it, including write access to private repositories."),
            new RuleDefinition(SlackToken, "Slack token in source", Severity.High, Confidence.High, "CWE-798", "A07:2021",
                "A Slack token can read and post workspace messages until it is explicitly revoked."),
            new RuleDefinition(StripeSecretKey, "Stripe live secret key in source", Severity.Critical, Confidence.High, "CWE-798", "A07:2021",
                "A live Stripe secret key can move real money and read customer records."),
            new RuleDefinition(GoogleApiKey, "Google API key in source", Severity.High, Confidence.High, "CWE-798", "A07:2021",
                "A Google API key is billable and, unless restricted, usable by anyone who obtains it."),
            new RuleDefinition(SendGridApiKey, "SendGrid API key in source", Severity.High, Confidence.High, "CWE-798", "A07:2021",
                "A SendGrid key allows sending mail as the owning domain, which enables convincing phishing."),
            new RuleDefinition(NpmAccessToken, "npm access token in source", Severity.High, Confidence.High, "CWE-798", "A07:2021",
                "An npm token can publish package versions under the owning account, which is a supply-chain risk."),
            new RuleDefinition(JsonWebToken, "Encoded JSON Web Token in source", Severity.High, Confidence.Medium, "CWE-522", "A07:2021",
                "A committed JWT is a live session or identity assertion until it expires, and its payload is readable by anyone."),
            new RuleDefinition(PrivateKeyBlock, "Private key block in source", Severity.Critical, Confidence.High, "CWE-321", "A02:2021",
                "A private key in the repository compromises every signature, certificate or session that depends on it."),
            new RuleDefinition(ConnectionStringPassword, "Database connection string carries a password", Severity.Critical, Confidence.High, "CWE-798", "A07:2021",
                "A connection string with an inline password exposes the database to anyone who can read the file."),
            new RuleDefinition(HighEntropyAssignment, "High-entropy value assigned to a security identifier", Severity.High, Confidence.Medium, "CWE-798", "A07:2021",
                "A long random-looking literal assigned to a key, token or secret identifier is very likely a real credential in source."),

            new RuleDefinition(JwtAlgorithmNone, "Token signed with algorithm \"none\"", Severity.Critical, Confidence.High, "CWE-347", "A02:2021",
                "Signing or verifying with algorithm \"none\" disables the signature entirely, so any token can be forged."),
            new RuleDefinition(TlsVerificationDisabled, "TLS certificate verification disabled", Severity.High, Confidence.High, "CWE-295", "A02:2021",
                "Disabling certificate verification removes protection against interception, even over HTTPS."),
            new RuleDefinition(TimingUnsafeComparison, "Signature or token compared without constant time", Severity.High, Confidence.Low, "CWE-208", "A02:2021",
                "Comparing a signature with a normal equality operator leaks timing information that can be used to recover it byte by byte."),
            new RuleDefinition(SqlBuiltByConcatenation, "SQL statement built by string concatenation", Severity.High, Confidence.Low, "CWE-89", "A03:2021",
                "Building SQL from concatenated values allows an attacker-controlled value to change the statement."),
            new RuleDefinition(DynamicCodeEvaluation, "Dynamic code evaluation", Severity.High, Confidence.Medium, "CWE-95", "A03:2021",
                "Evaluating a string as code lets any value that reaches it execute inside the application."),
            new RuleDefinition(ShellExecutionWithInterpolation, "Shell command built from an interpolated value", Severity.High, Confidence.Medium, "CWE-78", "A03:2021",
                "A shell command assembled from a variable lets that value append further commands."),
            new RuleDefinition(WeakHashAlgorithm, "Weak hash algorithm", Severity.Medium, Confidence.Medium, "CWE-327", "A02:2021",
                "MD5 and SHA-1 are broken for security use; collisions can be produced deliberately."),
            new RuleDefinition(InsecureDeserialization, "Deserialization of untrusted data", Severity.High, Confidence.Medium, "CWE-502", "A08:2021",
                "These deserializers can construct arbitrary objects, which turns a hostile payload into code execution."),
            new RuleDefinition(SecretWrittenToLog, "Credential or session value written to a log", Severity.Medium, Confidence.Low, "CWE-532", "A09:2021",
                "Secrets written to logs spread to log aggregation, backups and support tickets long after the code changes.")
        }
        .ToDictionary(rule => rule.Id, StringComparer.Ordinal);

    /// <summary>All rules, ordered by severity then identifier, for the Rules screen and the report appendix.</summary>
    public static IReadOnlyList<RuleDefinition> All { get; } = Definitions.Values
        .OrderByDescending(rule => rule.Severity)
        .ThenBy(rule => rule.Id, StringComparer.Ordinal)
        .ToArray();

    public static bool TryGet(string ruleId, out RuleDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            definition = null;
            return false;
        }

        return Definitions.TryGetValue(ruleId, out definition);
    }

    public static RuleDefinition Require(string ruleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

        return Definitions.TryGetValue(ruleId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown rule identifier '{ruleId}'.");
    }
}
