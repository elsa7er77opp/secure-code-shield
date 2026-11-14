using SecureCodeShield.Desktop.Review;
using SecureCodeShield.Desktop.ViewModels;
using SecureCodeShield.Infrastructure.Reports;

namespace SecureCodeShield.Desktop.Tests;

public sealed class LocalReviewWorkflowAcceptanceTests
{
    [Fact]
    public async Task Authorized_local_review_produces_only_redacted_findings_and_a_sanitized_local_report()
    {
        var root = Path.Combine(Path.GetTempPath(), "secure-code-shield-acceptance-" + Guid.NewGuid().ToString("N"));
        var reportDirectory = Path.Combine(root, "reports");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "fixture.env"), "API_KEY=synthetic-final-check\n");

        try
        {
            var viewModel = new MainViewModel();
            viewModel.SetProjectCandidate("acceptance-fixture");
            viewModel.AuthorizeSelectedProject();
            viewModel.BeginLocalReview();

            var findings = await new LocalReviewRunner().RunAsync(root, CancellationToken.None);
            viewModel.CompleteLocalReview(findings.Count);
            var report = SanitizedReportExporter.Export(reportDirectory, "acceptance-fixture", findings);
            var reportText = File.ReadAllText(report.ReportPath);

            Assert.True(viewModel.CanExportSanitizedReport);
            Assert.Equal(1, viewModel.FindingCount);
            Assert.DoesNotContain("synthetic-final-check", reportText, StringComparison.Ordinal);
            Assert.Contains("SCS-SECRET-001", reportText, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
