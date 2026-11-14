using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class LineAnalyzerContractTests
{
    [Fact]
    public void Runs_every_detector_over_one_line()
    {
        const string line = "const signingKey = \"hK3mQ9vZp2LxT7bR4nW8sY6cF1dJ0gA5\"; // used with algorithm: \"none\"";

        var findings = LineAnalyzer.Analyze("src/services/auth.ts", 115, line);

        Assert.Contains(findings, finding => finding.RuleId == RuleCatalog.HighEntropyAssignment);
    }

    [Fact]
    public void Raises_a_rule_at_most_once_per_line()
    {
        const string line = "a = \"AKIAIOSFODNN7EXAMPLE\"; b = \"AKIAIOSFODNN7EXAMPLE\";";

        var findings = LineAnalyzer.Analyze("src/config.ts", 2, line);

        Assert.Single(findings, finding => finding.RuleId == RuleCatalog.AwsAccessKey);
    }

    [Fact]
    public void Reports_nothing_from_a_non_production_path()
    {
        const string line = "const key = \"AKIAIOSFODNN7EXAMPLE\";";

        Assert.Empty(LineAnalyzer.Analyze("tests/fixtures/config.ts", 1, line));
    }

    [Fact]
    public void Honours_an_inline_acceptance()
    {
        const string line = "const key = \"AKIAIOSFODNN7EXAMPLE\"; // scs-ignore";

        Assert.Empty(LineAnalyzer.Analyze("src/config.ts", 1, line));
    }

    [Fact]
    public void Every_reported_finding_resolves_to_a_catalog_rule()
    {
        const string line = "const apiKey = \"9fT2xQ7bLm4KpR8vN3zW6yH1cJ5dS0aG\";";

        foreach (var finding in LineAnalyzer.Analyze("src/config.ts", 3, line))
        {
            Assert.NotNull(finding.Rule);
            Assert.Equal(finding.RuleId, finding.Rule.Id);
        }
    }
}
