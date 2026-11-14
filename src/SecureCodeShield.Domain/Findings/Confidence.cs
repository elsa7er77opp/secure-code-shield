namespace SecureCodeShield.Domain.Findings;

/// <summary>
/// How certain the rule is that the match is real. Heuristic rules must not claim high confidence.
/// </summary>
public enum Confidence
{
    Low = 0,
    Medium = 1,
    High = 2
}
