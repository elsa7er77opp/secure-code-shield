using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class RuleCatalogContractTests
{
    [Fact]
    public void Every_rule_carries_the_mapping_an_audit_report_needs()
    {
        Assert.NotEmpty(RuleCatalog.All);

        foreach (var rule in RuleCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Title), $"{rule.Id} must have a title.");
            Assert.StartsWith("CWE-", rule.Cwe, StringComparison.Ordinal);
            Assert.Matches(@"^A\d{2}:2021$", rule.Owasp);
            Assert.False(string.IsNullOrWhiteSpace(rule.Rationale), $"{rule.Id} must explain why it matters.");
        }
    }

    [Fact]
    public void Rule_identifiers_are_unique()
    {
        var identifiers = RuleCatalog.All.Select(rule => rule.Id).ToArray();

        Assert.Equal(identifiers.Length, identifiers.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Require_rejects_an_unknown_identifier_instead_of_inventing_one()
    {
        Assert.Throws<KeyNotFoundException>(() => RuleCatalog.Require("SCS-DOES-NOT-EXIST"));
    }

    [Fact]
    public void Heuristic_rules_never_claim_high_confidence()
    {
        var heuristics = new[]
        {
            RuleCatalog.TimingUnsafeComparison,
            RuleCatalog.SqlBuiltByConcatenation,
            RuleCatalog.SecretWrittenToLog
        };

        foreach (var ruleId in heuristics)
        {
            Assert.NotEqual(Confidence.High, RuleCatalog.Require(ruleId).Confidence);
        }
    }
}
