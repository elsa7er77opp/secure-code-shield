using System.Reflection;

namespace SecureCodeShield.Infrastructure.Tests;

public sealed class LocalProjectScannerCancellationContractTests : IDisposable
{
    private readonly string projectRoot = Path.Combine(
        Path.GetTempPath(),
        "secure-code-shield-cancellation-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ScanCancellable_stops_before_reading_when_cancellation_is_already_requested()
    {
        Directory.CreateDirectory(projectRoot);
        File.WriteAllText(Path.Combine(projectRoot, ".env"), "GITHUB_TOKEN=ghp_1234567890ABCDEFGHIJKLMN");

        var scannerType = Type.GetType(
            "SecureCodeShield.Infrastructure.LocalFiles.LocalProjectScanner, SecureCodeShield.Infrastructure");
        var scanMethod = scannerType?.GetMethod("ScanCancellable", BindingFlags.Public | BindingFlags.Static);

        Assert.True(scanMethod is not null, "The local scan contract must expose LocalProjectScanner.ScanCancellable.");
        if (scanMethod is null)
        {
            return;
        }

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = Assert.Throws<TargetInvocationException>(
            () => scanMethod.Invoke(null, [projectRoot, true, cancellation.Token]));

        Assert.IsType<OperationCanceledException>(exception.InnerException);
    }

    public void Dispose()
    {
        if (Directory.Exists(projectRoot))
        {
            Directory.Delete(projectRoot, recursive: true);
        }
    }
}
