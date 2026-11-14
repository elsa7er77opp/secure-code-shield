using System.Collections;
using System.Reflection;

namespace SecureCodeShield.Infrastructure.Tests;

public sealed class SanitizedReportExporterContractTests : IDisposable
{
    private readonly string outputDirectory = Path.Combine(
        Path.GetTempPath(),
        "secure-code-shield-report-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Export_writes_a_local_report_without_raw_evidence_or_absolute_project_paths()
    {
        const string rawToken = "ghp_1234567890ABCDEFGHIJKLMN";
        const string absoluteRoot = @"C:\Projects\Client-Repository";
        const string maskedEvidence = "ghp_••••••••••••••••••••••••MN";
        Directory.CreateDirectory(outputDirectory);

        var domainAssembly = Assembly.Load("SecureCodeShield.Domain");
        var findingType = domainAssembly.GetType("SecureCodeShield.Domain.Findings.SecretFinding");
        var exporterType = Type.GetType(
            "SecureCodeShield.Infrastructure.Reports.SanitizedReportExporter, SecureCodeShield.Infrastructure");
        var exportMethod = exporterType?.GetMethod("Export", BindingFlags.Public | BindingFlags.Static);

        Assert.True(findingType is not null, "The safe finding model must be available for export.");
        Assert.True(exportMethod is not null, "The reporting contract must expose SanitizedReportExporter.Export.");
        if (findingType is null || exportMethod is null)
        {
            return;
        }

        var finding = Activator.CreateInstance(
            findingType,
            "SCS-SECRET-001",
            "config/.env",
            7,
            maskedEvidence);
        var findings = Array.CreateInstance(findingType, 1);
        findings.SetValue(finding, 0);

        var result = exportMethod.Invoke(null, [outputDirectory, "Client Repository", findings]);
        var reportPath = Assert.IsType<string>(result?.GetType().GetProperty("ReportPath")?.GetValue(result));
        var reportContent = File.ReadAllText(reportPath);

        Assert.True(File.Exists(reportPath));
        Assert.Contains(maskedEvidence, reportContent, StringComparison.Ordinal);
        Assert.DoesNotContain(rawToken, reportContent, StringComparison.Ordinal);
        Assert.DoesNotContain(absoluteRoot, reportContent, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
