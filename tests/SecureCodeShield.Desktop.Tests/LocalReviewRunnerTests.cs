using SecureCodeShield.Desktop.Review;

namespace SecureCodeShield.Desktop.Tests;

public sealed class LocalReviewRunnerTests
{
    [Fact]
    public async Task Local_runner_returns_redacted_findings_for_an_explicitly_authorized_fixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "secure-code-shield-review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "fixture.env"), "API_KEY=synthetic-review-value\n");

        try
        {
            var runner = new LocalReviewRunner();

            var result = await runner.RunAsync(root, CancellationToken.None);

            var finding = Assert.Single(result);
            Assert.Equal("fixture.env", finding.RelativePath);
            Assert.Equal("SCS-SECRET-001", finding.RuleId);
            Assert.DoesNotContain("synthetic-review-value", finding.MaskedEvidence, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
