using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class DangerousPatternDetectorContractTests
{
    [Theory]
    [InlineData("return jwt.sign(payload, key, { algorithm: \"none\" });", RuleCatalog.JwtAlgorithmNone)]
    [InlineData("const agent = new https.Agent({ rejectUnauthorized: false });", RuleCatalog.TlsVerificationDisabled)]
    [InlineData("tls := &tls.Config{InsecureSkipVerify: true}", RuleCatalog.TlsVerificationDisabled)]
    [InlineData("if (signature == expected) { return true; }", RuleCatalog.TimingUnsafeComparison)]
    [InlineData("const sql = \"SELECT id FROM users WHERE name = \" + name;", RuleCatalog.SqlBuiltByConcatenation)]
    [InlineData("const result = eval(userInput);", RuleCatalog.DynamicCodeEvaluation)]
    [InlineData("child_process.exec(`rm -rf ${target}`);", RuleCatalog.ShellExecutionWithInterpolation)]
    [InlineData("using var hash = MD5.Create();", RuleCatalog.WeakHashAlgorithm)]
    [InlineData("const data = pickle.loads(payload)", RuleCatalog.InsecureDeserialization)]
    [InlineData("logger.info(`session=${sessionToken}`);", RuleCatalog.SecretWrittenToLog)]
    public void Detects_a_dangerous_code_shape(string line, string expectedRuleId)
    {
        var findings = DangerousPatternDetector.ScanLine("src/services/auth.ts", 42, line);

        Assert.Contains(findings, finding => finding.RuleId == expectedRuleId);
    }

    [Fact]
    public void Carries_no_evidence_because_these_rules_read_no_value()
    {
        var finding = Assert.Single(
            DangerousPatternDetector.ScanLine("src/auth.ts", 1, "jwt.verify(token, key, { algorithm: \"none\" });"));

        Assert.Equal(string.Empty, finding.MaskedEvidence);
    }

    [Theory]
    [InlineData("// never sign with algorithm: \"none\" in production")]
    [InlineData("# rejectUnauthorized: false is forbidden here")]
    [InlineData(" * eval(input) would be unsafe")]
    public void Ignores_a_line_that_is_only_a_comment(string line)
    {
        Assert.Empty(DangerousPatternDetector.ScanLine("src/auth.ts", 5, line));
    }

    [Theory]
    [InlineData("const sql = \"SELECT id FROM users WHERE name = @name\";")]
    [InlineData("const agent = new https.Agent({ rejectUnauthorized: true });")]
    [InlineData("const total = subtotal + tax;")]
    public void Ignores_a_safe_line(string line)
    {
        Assert.Empty(DangerousPatternDetector.ScanLine("src/services/order.ts", 8, line));
    }
}
