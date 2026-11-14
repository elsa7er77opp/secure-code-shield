using System.Collections;
using System.Reflection;

namespace SecureCodeShield.Infrastructure.Tests;

public sealed class LocalProjectScannerContractTests : IDisposable
{
    private readonly string projectRoot = Path.Combine(
        Path.GetTempPath(),
        "secure-code-shield-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Scan_returns_only_masked_findings_and_skips_default_dependency_directories()
    {
        const string rawToken = "ghp_1234567890ABCDEFGHIJKLMN";
        Directory.CreateDirectory(projectRoot);
        File.WriteAllText(Path.Combine(projectRoot, ".env"), "GITHUB_TOKEN=" + rawToken);
        Directory.CreateDirectory(Path.Combine(projectRoot, "node_modules", "sample-package"));
        File.WriteAllText(
            Path.Combine(projectRoot, "node_modules", "sample-package", ".env"),
            "GITHUB_TOKEN=" + rawToken);

        var scannerType = Type.GetType(
            "SecureCodeShield.Infrastructure.LocalFiles.LocalProjectScanner, SecureCodeShield.Infrastructure");
        var scanMethod = scannerType?.GetMethod("Scan", BindingFlags.Public | BindingFlags.Static);

        Assert.True(scanMethod is not null, "The local scan contract must expose LocalProjectScanner.Scan.");
        if (scanMethod is null)
        {
            return;
        }

        var findings = Assert.IsAssignableFrom<IEnumerable>(scanMethod.Invoke(null, [projectRoot, true]));
        var finding = Assert.Single(findings.Cast<object>());
        var maskedEvidence = Assert.IsType<string>(
            finding.GetType().GetProperty("MaskedEvidence")?.GetValue(finding));

        Assert.DoesNotContain(rawToken, maskedEvidence, StringComparison.Ordinal);
        Assert.Contains("•", maskedEvidence, StringComparison.Ordinal);
    }

    [Fact]
    public void Scan_rejects_a_local_folder_without_explicit_authorization()
    {
        Directory.CreateDirectory(projectRoot);

        var scannerType = Type.GetType(
            "SecureCodeShield.Infrastructure.LocalFiles.LocalProjectScanner, SecureCodeShield.Infrastructure");
        var scanMethod = scannerType?.GetMethod("Scan", BindingFlags.Public | BindingFlags.Static);

        Assert.True(scanMethod is not null, "The local scan contract must expose LocalProjectScanner.Scan.");
        if (scanMethod is null)
        {
            return;
        }

        var exception = Assert.Throws<TargetInvocationException>(
            () => scanMethod.Invoke(null, [projectRoot, false]));

        Assert.IsType<UnauthorizedAccessException>(exception.InnerException);
    }

    public void Dispose()
    {
        if (Directory.Exists(projectRoot))
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }
}
