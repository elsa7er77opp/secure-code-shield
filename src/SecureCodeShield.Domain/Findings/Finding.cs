namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// A display-safe finding. There is deliberately no field for a raw source line or a raw secret value:
/// evidence is masked before construction and the unmasked value is discarded by the caller.
/// </summary>
/// <param name="RuleId">Identifier resolvable through <see cref="RuleCatalog"/>.</param>
/// <param name="RelativePath">Path relative to the authorized project root.</param>
/// <param name="LineNumber">One-based line number.</param>
/// <param name="MaskedEvidence">Masked evidence, or an empty string when the rule has no value to show.</param>
/// <param name="Detail">Optional short, non-sensitive qualifier such as a matched identifier name.</param>
public sealed record Finding(
    string RuleId,
    string RelativePath,
    int LineNumber,
    string MaskedEvidence,
    string? Detail = null)
{
    public RuleDefinition Rule => RuleCatalog.Require(RuleId);
}
