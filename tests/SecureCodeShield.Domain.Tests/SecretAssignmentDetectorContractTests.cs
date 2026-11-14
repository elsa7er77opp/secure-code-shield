using System.Collections;
using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class SecretAssignmentDetectorContractTests
{
    [Fact]
    public void ScanLine_detects_a_synthetic_token_assignment_and_returns_masked_evidence_only()
    {
        const string rawToken = "ghp_1234567890ABCDEFGHIJKLMN";
        const string sourceLine = "GITHUB_TOKEN=" + rawToken;

        var domainAssembly = Assembly.Load("SecureCodeShield.Domain");
        var detectorType = domainAssembly.GetType("SecureCodeShield.Domain.Findings.SecretAssignmentDetector");
        var scanLineMethod = detectorType?.GetMethod("ScanLine", BindingFlags.Public | BindingFlags.Static);

        Assert.True(scanLineMethod is not null, "The finding contract must expose SecretAssignmentDetector.ScanLine.");
        if (scanLineMethod is null)
        {
            return;
        }

        var matches = Assert.IsAssignableFrom<IEnumerable>(
            scanLineMethod.Invoke(null, [".env", 7, sourceLine]));
        var finding = Assert.Single(matches.Cast<object>());

        var maskedEvidence = Assert.IsType<string>(
            finding.GetType().GetProperty("MaskedEvidence")?.GetValue(finding));
        var ruleId = Assert.IsType<string>(
            finding.GetType().GetProperty("RuleId")?.GetValue(finding));
        var lineNumber = Assert.IsType<int>(
            finding.GetType().GetProperty("LineNumber")?.GetValue(finding));

        Assert.Equal("SCS-SECRET-001", ruleId);
        Assert.Equal(7, lineNumber);
        Assert.DoesNotContain(rawToken, maskedEvidence, StringComparison.Ordinal);
        Assert.Contains("ghp_", maskedEvidence, StringComparison.Ordinal);
        Assert.Contains("•", maskedEvidence, StringComparison.Ordinal);
    }
}
