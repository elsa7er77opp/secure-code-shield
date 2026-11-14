using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class SecretPatternDetectorContractTests
{
    [Theory]
    [InlineData("const key = \"AKIAIOSFODNN7EXAMPLE\";", RuleCatalog.AwsAccessKey)]
    [InlineData("token: ghp_0123456789abcdefghijklmnopqrstuvwxyz", RuleCatalog.GitHubToken)]
    [InlineData("SLACK=xoxb-1234567890-abcdefghij", RuleCatalog.SlackToken)]
    [InlineData("stripe = 'sk_live_0123456789abcdefghij'", RuleCatalog.StripeSecretKey)]
    [InlineData("maps: \"AIzaSyA0123456789abcdefghijklmnopqrstuv\"", RuleCatalog.GoogleApiKey)]
    [InlineData("npmToken=npm_012345678901234567890123456789012345", RuleCatalog.NpmAccessToken)]
    [InlineData("-----BEGIN RSA PRIVATE KEY-----", RuleCatalog.PrivateKeyBlock)]
    [InlineData("DB=postgres://app:s3cr3tvalue@db.internal:5432/app", RuleCatalog.ConnectionStringPassword)]
    public void Detects_a_documented_provider_credential(string line, string expectedRuleId)
    {
        var findings = SecretPatternDetector.ScanLine("src/config.ts", 12, line);

        Assert.Contains(findings, finding => finding.RuleId == expectedRuleId);
    }

    [Fact]
    public void Masks_the_credential_so_the_raw_value_never_reaches_a_finding()
    {
        const string secret = "AKIAIOSFODNN7EXAMPLE";

        var finding = Assert.Single(SecretPatternDetector.ScanLine("src/config.ts", 1, $"key = \"{secret}\";"));

        Assert.DoesNotContain(secret, finding.MaskedEvidence, StringComparison.Ordinal);
        Assert.Contains("•", finding.MaskedEvidence, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("const region = \"eu-west-1\";")]
    [InlineData("// AKIA is the prefix of an AWS access key identifier")]
    [InlineData("import { getToken } from \"./auth\";")]
    public void Ignores_a_line_that_carries_no_credential(string line)
    {
        Assert.Empty(SecretPatternDetector.ScanLine("src/config.ts", 3, line));
    }

    [Fact]
    public void Reports_every_credential_when_one_line_carries_two()
    {
        const string line = "cfg = { aws: \"AKIAIOSFODNN7EXAMPLE\", gh: \"ghp_0123456789abcdefghijklmnopqrstuvwxyz\" };";

        var findings = SecretPatternDetector.ScanLine("src/config.ts", 7, line);

        Assert.Contains(findings, finding => finding.RuleId == RuleCatalog.AwsAccessKey);
        Assert.Contains(findings, finding => finding.RuleId == RuleCatalog.GitHubToken);
    }
}
