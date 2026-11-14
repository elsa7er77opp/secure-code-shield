namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// How much attention a finding demands. Severity is a property of the rule, never of presentation.
/// </summary>
public enum Severity
{
    Note = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
