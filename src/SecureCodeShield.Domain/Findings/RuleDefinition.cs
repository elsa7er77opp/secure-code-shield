namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// The immutable description of a rule. Everything an exported report needs about a finding —
/// other than its location — comes from here, so a report can be produced without re-reading source.
/// </summary>
/// <param name="Id">Stable identifier used in exports and suppression lists.</param>
/// <param name="Title">Short human title shown in the workstation and the report.</param>
/// <param name="Severity">Rule severity.</param>
/// <param name="Confidence">How certain a match is; heuristics stay below High.</param>
/// <param name="Cwe">CWE identifier, e.g. CWE-798.</param>
/// <param name="Owasp">OWASP Top 10 (2021) category, e.g. A07:2021.</param>
/// <param name="Rationale">Why this matters, in one auditor-readable sentence.</param>
public sealed record RuleDefinition(
    string Id,
    string Title,
    Severity Severity,
    Confidence Confidence,
    string Cwe,
    string Owasp,
    string Rationale);
