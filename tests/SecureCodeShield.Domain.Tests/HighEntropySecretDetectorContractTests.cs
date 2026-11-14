using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class HighEntropySecretDetectorContractTests
{
    [Fact]
    public void Detects_the_hard_coded_signing_key_the_environment_rule_cannot_see()
    {
        const string line = "  const signingKey = \"hK3mQ9vZp2LxT7bR4nW8sY6cF1dJ0gA5\";";

        var finding = Assert.Single(HighEntropySecretDetector.ScanLine("src/services/auth.ts", 115, line));

        Assert.Equal(RuleCatalog.HighEntropyAssignment, finding.RuleId);
        Assert.Equal(115, finding.LineNumber);
        Assert.Equal("signingKey", finding.Detail);
        Assert.DoesNotContain("hK3mQ9vZp2LxT7bR4nW8sY6cF1dJ0gA5", finding.MaskedEvidence, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("apiKey: \"9fT2xQ7bLm4KpR8vN3zW6yH1cJ5dS0aG\"")]
    [InlineData("\"client_secret\": \"Zq7Xn2Lp9Rt4Vb8Mk3Wy6Hc1Jd5Sa0Gf\"")]
    [InlineData("PRIVATE_KEY = 'Mv8Kx3Qw7Zr2Np9Lt5Yb4Hc1Jd6Sa0Gf'")]
    public void Detects_a_high_entropy_value_on_a_security_identifier(string line)
    {
        Assert.NotEmpty(HighEntropySecretDetector.ScanLine("src/config.ts", 4, line));
    }

    [Theory]
    [InlineData("const apiKey = \"your-api-key-goes-here\";")]
    [InlineData("const apiKey = \"xxxxxxxxxxxxxxxxxxxxxxxx\";")]
    [InlineData("const apiKey = \"${process.env.API_KEY}\";")]
    [InlineData("const apiKey = \"<replace-with-your-key>\";")]
    [InlineData("const secretUrl = \"https://vault.internal/secrets/app\";")]
    [InlineData("const password = \"correct horse battery staple\";")]
    [InlineData("const token = \"short\";")]
    public void Rejects_a_placeholder_or_a_value_that_is_something_else(string line)
    {
        Assert.Empty(HighEntropySecretDetector.ScanLine("src/config.ts", 4, line));
    }

    [Fact]
    public void Ignores_an_assignment_whose_identifier_is_not_security_material()
    {
        const string line = "const buildHash = \"hK3mQ9vZp2LxT7bR4nW8sY6cF1dJ0gA5\";";

        Assert.Empty(HighEntropySecretDetector.ScanLine("src/build.ts", 9, line));
    }

    [Fact]
    public void Entropy_separates_random_material_from_english()
    {
        var random = EntropyEstimator.BitsPerCharacter("hK3mQ9vZp2LxT7bR4nW8sY6cF1dJ0gA5");
        var english = EntropyEstimator.BitsPerCharacter("aaaaaaaaaaaaaaaaaaaaaaaa");

        Assert.True(random > HighEntropySecretDetector.MinimumEntropy, $"expected high entropy, got {random}");
        Assert.True(english < HighEntropySecretDetector.MinimumEntropy, $"expected low entropy, got {english}");
    }
}
