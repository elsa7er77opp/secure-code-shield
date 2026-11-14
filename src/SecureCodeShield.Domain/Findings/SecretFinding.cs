namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// A display-safe security finding. It intentionally has no field for raw source evidence.
/// </summary>
public sealed record SecretFinding(
    string RuleId,
    string RelativePath,
    int LineNumber,
    string MaskedEvidence);
